using System;
using System.Collections.Generic;
using System.Text;
using Comparer.Utils.Objects;
using Comparer.Utils.Extensions;
using System.Text.RegularExpressions;

namespace Comparer.Utils
{
    public class Comparer
    {
        public float Timeout { get; set; } = 1.0f;
        public short EditCost { get; set; } = 4;

        /**
         * Find the differences between two texts.
         * Run a faster, slightly less optimal diff.
         * This method allows the 'checklines' of diff_main() to be optional.
         * Most of the time checklines is wanted, so default to true.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @return List of Diff objects.
         */
        public DiffCollection Compare(string reference, string textToCompare)
        {
            return this.Compare(reference, textToCompare, true);
        }

        /**
         * Find the differences between two texts.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param checklines Speedup flag.  If false, then don't run a
         *     line-level diff first to identify the changed areas.
         *     If true, then run a faster slightly less optimal diff.
         * @return List of Diff objects.
         */
        public DiffCollection Compare(string reference, string textToCompare, bool checklines)
        {
            // Set a deadline by which time the diff must be complete.
            DateTime deadline;

            if (this.Timeout <= 0)
            {
                deadline = DateTime.MaxValue;
            }
            else
            {
                deadline = DateTime.Now +
                    new TimeSpan(((long)(this.Timeout * 1000)) * 10000);
            }

            return this.Compare(reference, textToCompare, checklines, deadline);
        }

        /**
         * Find the differences between two texts.  Simplifies the problem by
         * stripping any common prefix or suffix off the texts before diffing.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param checklines Speedup flag.  If false, then don't run a
         *     line-level diff first to identify the changed areas.
         *     If true, then run a faster slightly less optimal diff.
         * @param deadline Time when the diff should be complete by.  Used
         *     internally for recursive calls.  Users should set DiffTimeout
         *     instead.
         * @return List of Diff objects.
         */
        private DiffCollection Compare(string reference, string textToCompare, bool checklines, DateTime deadline)
        {
            // Check for null inputs not needed since null can't be passed in C#.

            // Check for equality (speedup).
            DiffCollection diffCollection;

            if (reference == textToCompare)
            {
                diffCollection = new DiffCollection();

                if (reference.Length != 0)
                {
                    diffCollection.Add(new Diff(Operation.EQUAL, reference));
                }

                return diffCollection;
            }

            // Trim off common prefix (speedup).
            int commonlength = this.CommonPrefix(reference, textToCompare);
            string commonprefix = reference.Substring(0, commonlength);
            reference = reference.Substring(commonlength);
            textToCompare = textToCompare.Substring(commonlength);

            // Trim off common suffix (speedup).
            commonlength = this.CommonSuffix(reference, textToCompare);
            string commonsuffix = reference.Substring(reference.Length - commonlength);
            reference = reference.Substring(0, reference.Length - commonlength);
            textToCompare = textToCompare.Substring(0, textToCompare.Length - commonlength);

            // Compute the diff on the middle block.
            diffCollection = this.Compute(reference, textToCompare, checklines, deadline);

            // Restore the prefix and suffix.
            if (commonprefix.Length != 0)
            {
                diffCollection.Insert(0, (new Diff(Operation.EQUAL, commonprefix)));
            }
            if (commonsuffix.Length != 0)
            {
                diffCollection.Add(new Diff(Operation.EQUAL, commonsuffix));
            }

            this.CleanupMerge(diffCollection);
            return diffCollection;
        }

        /**
         * Find the differences between two texts.  Assumes that the texts do not
         * have any common prefix or suffix.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param checklines Speedup flag.  If false, then don't run a
         *     line-level diff first to identify the changed areas.
         *     If true, then run a faster slightly less optimal diff.
         * @param deadline Time when the diff should be complete by.
         * @return List of Diff objects.
         */
        private DiffCollection Compute(string reference, string textToCompare, bool checklines, DateTime deadline)
        {
            DiffCollection diffCollection = new DiffCollection();

            if (reference.Length == 0)
            {
                // Just add some text (speedup).
                diffCollection.Add(new Diff(Operation.INSERT, textToCompare));

                return diffCollection;
            }

            if (textToCompare.Length == 0)
            {
                // Just delete some text (speedup).
                diffCollection.Add(new Diff(Operation.DELETE, reference));

                return diffCollection;
            }

            string longtext = reference.Length > textToCompare.Length ? reference : textToCompare;
            string shorttext = reference.Length > textToCompare.Length ? textToCompare : reference;

            int index = longtext.IndexOf(shorttext, StringComparison.Ordinal);

            if (index != -1)
            {
                // Shorter text is inside the longer text (speedup).
                Operation operation = (reference.Length > textToCompare.Length) ? Operation.DELETE : Operation.INSERT;

                diffCollection.Add(new Diff(operation, longtext.Substring(0, index)));
                diffCollection.Add(new Diff(Operation.EQUAL, shorttext));
                diffCollection.Add(new Diff(operation, longtext.Substring(index + shorttext.Length)));

                return diffCollection;
            }

            if (shorttext.Length == 1)
            {
                // Single character string.
                // After the previous speedup, the character can't be an equality.
                diffCollection.Add(new Diff(Operation.DELETE, reference));
                diffCollection.Add(new Diff(Operation.INSERT, textToCompare));

                return diffCollection;
            }

            // Check to see if the problem can be split in two.
            string[] halfmatch = this.Halfmatch(reference, textToCompare);

            if (halfmatch != null)
            {
                // A half-match was found, sort out the return data.
                string reference_a = halfmatch[0];
                string reference_b = halfmatch[1];
                string textToCompare_a = halfmatch[2];
                string textToCompare_b = halfmatch[3];
                string common = halfmatch[4];

                // Send both pairs off for separate processing.
                DiffCollection diffs_a = this.Compare(reference_a, textToCompare_a, checklines, deadline);
                DiffCollection diffs_b = this.Compare(reference_b, textToCompare_b, checklines, deadline);

                // Merge the results.
                diffCollection = diffs_a;
                diffCollection.Add(new Diff(Operation.EQUAL, common));
                diffCollection.AddRange(diffs_b);
                return diffCollection;
            }

            if (checklines && reference.Length > 100 && textToCompare.Length > 100)
            {
                return this.Linemode(reference, textToCompare, deadline);
            }

            return this.Bisect(reference, textToCompare, deadline);
        }

        /**
         * Do a quick line-level diff on both strings, then rediff the parts for
         * greater accuracy.
         * This speedup can produce non-minimal diffs.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param deadline Time when the diff should be complete by.
         * @return List of Diff objects.
         */
        private DiffCollection Linemode(string reference, string textToCompare, DateTime deadline)
        {
            // Scan the text on a line-by-line basis first.
            Object[] lines = this.LinesToChars(reference, textToCompare);

            reference = (string)lines[0];
            textToCompare = (string)lines[1];

            List<string> linearray = (List<string>)lines[2];

            DiffCollection diffCollection = this.Compare(reference, textToCompare, false, deadline);

            // Convert the diff back to original text.
            this.CharsToLines(diffCollection, linearray);

            // Eliminate freak matches (e.g. blank lines)
            this.CleanupSemantic(diffCollection);

            // Rediff any replacement blocks, this time character-by-character.
            // Add a dummy entry at the end.
            diffCollection.Add(new Diff(Operation.EQUAL, String.Empty));

            int pointer = 0;
            int count_delete = 0;
            int count_insert = 0;

            string text_delete = String.Empty;
            string text_insert = String.Empty;

            while (pointer < diffCollection.Count)
            {
                switch (diffCollection[pointer].Operation)
                {
                    case Operation.INSERT:
                        count_insert++;
                        text_insert += diffCollection[pointer].Text;
                        break;

                    case Operation.DELETE:
                        count_delete++;
                        text_delete += diffCollection[pointer].Text;
                        break;

                    case Operation.EQUAL:
                        // Upon reaching an equality, check for prior redundancies.
                        if (count_delete >= 1 && count_insert >= 1)
                        {
                            // Delete the offending records and add the merged ones.
                            diffCollection.RemoveRange(pointer - count_delete - count_insert, count_delete + count_insert);

                            pointer = pointer - count_delete - count_insert;

                            DiffCollection a = this.Compare(text_delete, text_insert, false, deadline);

                            diffCollection.InsertRange(pointer, a);
                            pointer = pointer + a.Count;
                        }

                        count_insert = 0;
                        count_delete = 0;

                        text_delete = String.Empty;
                        text_insert = String.Empty;

                        break;
                }

                pointer++;
            }

            diffCollection.RemoveAt(diffCollection.Count - 1);  // Remove the dummy entry at the end.

            return diffCollection;
        }

        /**
         * Find the 'middle snake' of a diff, split the problem in two
         * and return the recursively constructed diff.
         * See Myers 1986 paper: An O(ND) Difference Algorithm and Its Variations.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param deadline Time at which to bail if not yet complete.
         * @return List of Diff objects.
         */
        protected DiffCollection Bisect(string reference, string textToCompare, DateTime deadline)
        {
            // Cache the text lengths to prevent multiple calls.
            int reference_length = reference.Length;
            int textToCompare_length = textToCompare.Length;

            int max = (reference_length + textToCompare_length + 1) / 2;

            int offset = max;
            int length = 2 * max;
            int[] v1 = new int[length];
            int[] v2 = new int[length];

            for (int x = 0; x < length; x++)
            {
                v1[x] = -1;
                v2[x] = -1;
            }

            v1[offset + 1] = 0;
            v2[offset + 1] = 0;

            int delta = reference_length - textToCompare_length;

            // If the total number of characters is odd, then the front path will
            // collide with the reverse path.
            bool front = (delta % 2 != 0);

            // Offsets for start and end of k loop.
            // Prevents mapping of space beyond the grid.
            int firstLoopStart = 0;
            int firstLoopEnd = 0;
            int secondLoopStart = 0;
            int secondLoopEnd = 0;

            for (int i = 0; i < max; i++)
            {
                // Bail out if deadline is reached.
                if (DateTime.Now > deadline)
                {
                    break;
                }

                // Walk the front path one step.
                for (int j = -i + firstLoopStart; j <= i - firstLoopEnd; j += 2)
                {
                    int loopOffset = offset + j;
                    int xIndex;

                    if (j == -i || j != i && v1[loopOffset - 1] < v1[loopOffset + 1])
                    {
                        xIndex = v1[loopOffset + 1];
                    }
                    else
                    {
                        xIndex = v1[loopOffset - 1] + 1;
                    }
                    int yIndex = xIndex - j;
                    while (xIndex < reference_length && yIndex < textToCompare_length
                          && reference[xIndex] == textToCompare[yIndex])
                    {
                        xIndex++;
                        yIndex++;
                    }

                    v1[loopOffset] = xIndex;

                    if (xIndex > reference_length)
                    {
                        // Ran off the right of the graph.
                        firstLoopEnd += 2;
                    }
                    else if (yIndex > textToCompare_length)
                    {
                        // Ran off the bottom of the graph.
                        firstLoopStart += 2;
                    }
                    else if (front)
                    {
                        int deltaOffset = offset + delta - j;

                        if (deltaOffset >= 0 && deltaOffset < length && v2[deltaOffset] != -1)
                        {
                            // Mirror x2 onto top-left coordinate system.
                            int x = reference_length - v2[deltaOffset];
                            if (xIndex >= x)
                            {
                                // Overlap detected.
                                return this.BisectSplit(reference, textToCompare, xIndex, yIndex, deadline);
                            }
                        }
                    }
                }

                // Walk the reverse path one step.
                for (int k2 = -i + secondLoopStart; k2 <= i - secondLoopEnd; k2 += 2)
                {
                    int k2_offset = offset + k2;
                    int x2;
                    if (k2 == -i || k2 != i && v2[k2_offset - 1] < v2[k2_offset + 1])
                    {
                        x2 = v2[k2_offset + 1];
                    }
                    else
                    {
                        x2 = v2[k2_offset - 1] + 1;
                    }
                    int y2 = x2 - k2;
                    while (x2 < reference_length && y2 < textToCompare_length
                        && reference[reference_length - x2 - 1]
                        == textToCompare[textToCompare_length - y2 - 1])
                    {
                        x2++;
                        y2++;
                    }
                    v2[k2_offset] = x2;
                    if (x2 > reference_length)
                    {
                        // Ran off the left of the graph.
                        secondLoopEnd += 2;
                    }
                    else if (y2 > textToCompare_length)
                    {
                        // Ran off the top of the graph.
                        secondLoopStart += 2;
                    }
                    else if (!front)
                    {
                        int k1_offset = offset + delta - k2;
                        if (k1_offset >= 0 && k1_offset < length && v1[k1_offset] != -1)
                        {
                            int x1 = v1[k1_offset];
                            int y1 = offset + x1 - k1_offset;
                            // Mirror x2 onto top-left coordinate system.
                            x2 = reference_length - v2[k2_offset];
                            if (x1 >= x2)
                            {
                                // Overlap detected.
                                return BisectSplit(reference, textToCompare, x1, y1, deadline);
                            }
                        }
                    }
                }
            }
            // Diff took too long and hit the deadline or
            // number of diffs equals number of characters, no commonality at all.
            DiffCollection diffCollection = new DiffCollection
            {
                new Diff(Operation.DELETE, reference),
                new Diff(Operation.INSERT, textToCompare)
            };

            return diffCollection;
        }

        /**
         * Given the location of the 'middle snake', split the diff in two parts
         * and recurse.
         * @param reference Old string to be diffed.
         * @param textToCompare New string to be diffed.
         * @param x Index of split point in reference.
         * @param y Index of split point in textToCompare.
         * @param deadline Time at which to bail if not yet complete.
         * @return LinkedList of Diff objects.
         */
        private DiffCollection BisectSplit(string reference, string textToCompare, int x, int y, DateTime deadline)
        {
            string referenceA = reference.Substring(0, x);
            string textToCompareA = textToCompare.Substring(0, y);
            string referenceB = reference.Substring(x);
            string textToCompareB = textToCompare.Substring(y);

            // Compute both diffs serially.
            DiffCollection diffCollection = this.Compare(referenceA, textToCompareA, false, deadline);
            DiffCollection diffCollectionB = this.Compare(referenceB, textToCompareB, false, deadline);

            diffCollection.AddRange(diffCollectionB);
            return diffCollection;
        }

        /**
         * Split two texts into a list of strings.  Reduce the texts to a string of
         * hashes where each Unicode character represents one line.
         * @param reference First string.
         * @param textToCompare Second string.
         * @return Three element Object array, containing the encoded reference, the
         *     encoded textToCompare and the List of unique strings.  The zeroth element
         *     of the List of unique strings is intentionally blank.
         */
        protected Object[] LinesToChars(string reference, string textToCompare)
        {
            List<string> lineArray = new List<string>();

            Dictionary<string, int> lineHash = new Dictionary<string, int>();

            // e.g. linearray[4] == "Hello\n"
            // e.g. linehash.get("Hello\n") == 4

            // "\x00" is a valid character, but various debuggers don't like it.
            // So we'll insert a junk entry to avoid generating a null character.
            lineArray.Add(String.Empty);

            string chars1 = this.LinesToCharsMunge(reference, lineArray, lineHash);
            string chars2 = this.LinesToCharsMunge(textToCompare, lineArray, lineHash);

            return new Object[] { chars1, chars2, lineArray };
        }

        /**
         * Split a text into a list of strings.  Reduce the texts to a string of
         * hashes where each Unicode character represents one line.
         * @param text String to encode.
         * @param lineArray List of unique strings.
         * @param lineHash Map of strings to indices.
         * @return Encoded string.
         */
        private string LinesToCharsMunge(string text, List<string> lineArray, Dictionary<string, int> lineHash)
        {
            string line;
            int lineStart = 0;
            int lineEnd = -1;

            StringBuilder chars = new StringBuilder();

            // Walk the text, pulling out a Substring for each line.
            // text.split('\n') would temporarily double our memory footprint.
            // Modifying text would create many large strings to garbage collect.
            while (lineEnd < text.Length - 1)
            {
                lineEnd = text.IndexOf('\n', lineStart);

                if (lineEnd == -1)
                {
                    lineEnd = text.Length - 1;
                }

                line = text.JavaSubstring(lineStart, lineEnd + 1);

                lineStart = lineEnd + 1;

                if (lineHash.ContainsKey(line))
                {
                    chars.Append(((char)(int)lineHash[line]));
                }
                else
                {
                    lineArray.Add(line);
                    lineHash.Add(line, lineArray.Count - 1);
                    chars.Append(((char)(lineArray.Count - 1)));
                }
            }

            return chars.ToString();
        }

        /**
         * Rehydrate the text in a diff from a string of line hashes to real lines
         * of text.
         * @param diffs List of Diff objects.
         * @param lineArray List of unique strings.
         */
        protected void CharsToLines(DiffCollection diffCollection, List<string> lineArray)
        {
            StringBuilder text;

            foreach (Diff diff in diffCollection)
            {
                text = new StringBuilder();

                for (int i = 0; i < diff.Text.Length; i++)
                {
                    text.Append(lineArray[diff.Text[i]]);
                }

                diff.Text = text.ToString();
            }
        }

        /**
         * Determine the common prefix of two strings.
         * @param reference First string.
         * @param textToCompare Second string.
         * @return The number of characters common to the start of each string.
         */
        public int CommonPrefix(string reference, string textToCompare)
        {
            int min = Math.Min(reference.Length, textToCompare.Length);

            for (int i = 0; i < min; i++)
            {
                if (reference[i] != textToCompare[i])
                {
                    return i;
                }
            }
            return min;
        }

        /**
         * Determine the common suffix of two strings.
         * @param reference First string.
         * @param textToCompare Second string.
         * @return The number of characters common to the end of each string.
         */
        public int CommonSuffix(string reference, string textToCompare)
        {
            int reference_length = reference.Length;
            int textToCompare_length = textToCompare.Length;

            int min = Math.Min(reference.Length, textToCompare.Length);

            for (int i = 1; i <= min; i++)
            {
                if (reference[reference_length - i] != textToCompare[textToCompare_length - i])
                {
                    return i - 1;
                }
            }
            return min;
        }

        /**
         * Determine if the suffix of one string is the prefix of another.
         * @param reference First string.
         * @param textToCompare Second string.
         * @return The number of characters common to the end of the first
         *     string and the start of the second string.
         */
        protected int CommonOverlap(string reference, string textToCompare)
        {
            // Cache the text lengths to prevent multiple calls.
            int reference_length = reference.Length;
            int textToCompare_length = textToCompare.Length;

            // Eliminate the null case.
            if (reference_length == 0 || textToCompare_length == 0)
            {
                return 0;
            }

            // Truncate the longer string.
            if (reference_length > textToCompare_length)
            {
                reference = reference.Substring(reference_length - textToCompare_length);
            }
            else if (reference_length < textToCompare_length)
            {
                textToCompare = textToCompare.Substring(0, reference_length);
            }
            int text_length = Math.Min(reference_length, textToCompare_length);

            // Quick check for the worst case.
            if (reference == textToCompare)
            {
                return text_length;
            }

            // Start by looking for a single character match
            // and increase length until no match is found.
            int best = 0;
            int length = 1;

            while (true)
            {
                string pattern = reference.Substring(text_length - length);

                int found = textToCompare.IndexOf(pattern, StringComparison.Ordinal);

                if (found == -1)
                {
                    return best;
                }

                length += found;

                if (found == 0 || reference.Substring(text_length - length) ==
                    textToCompare.Substring(0, length))
                {
                    best = length;
                    length++;
                }
            }
        }

        /**
         * Do the two texts share a Substring which is at least half the length of
         * the longer text?
         * This speedup can produce non-minimal diffs.
         * @param reference First string.
         * @param textToCompare Second string.
         * @return Five element String array, containing the prefix of reference, the
         *     suffix of reference, the prefix of textToCompare, the suffix of textToCompare and the
         *     common middle.  Or null if there was no match.
         */

        protected string[] Halfmatch(string reference, string textToCompare)
        {
            if (this.Timeout <= 0)
            {
                // Don't risk returning a non-optimal diff if we have unlimited time.
                return null;
            }

            string longtext = reference.Length > textToCompare.Length ? reference : textToCompare;
            string shorttext = reference.Length > textToCompare.Length ? textToCompare : reference;

            if (longtext.Length < 4 || shorttext.Length * 2 < longtext.Length)
            {
                return null;  // Pointless.
            }

            // First check if the second quarter is the seed for a half-match.
            string[] firstHalfmatch = HalfmatchI(longtext, shorttext, (longtext.Length + 3) / 4);

            // Check again based on the third quarter.
            string[] secondHalfmatch = HalfmatchI(longtext, shorttext, (longtext.Length + 1) / 2);

            string[] halfmatch;

            if (firstHalfmatch == null && secondHalfmatch == null)
            {
                return null;
            }
            else if (secondHalfmatch == null)
            {
                halfmatch = firstHalfmatch;
            }
            else if (firstHalfmatch == null)
            {
                halfmatch = secondHalfmatch;
            }
            else
            {
                // Both matched.  Select the longest.
                halfmatch = firstHalfmatch[4].Length > secondHalfmatch[4].Length ? firstHalfmatch : secondHalfmatch;
            }

            // A half-match was found, sort out the return data.
            if (reference.Length > textToCompare.Length)
            {
                return halfmatch;
            }
            else
            {
                return new string[] { halfmatch[2], halfmatch[3], halfmatch[0], halfmatch[1], halfmatch[4] };
            }
        }

        /**
         * Does a Substring of shorttext exist within longtext such that the
         * Substring is at least half the length of longtext?
         * @param longtext Longer string.
         * @param shorttext Shorter string.
         * @param i Start index of quarter length Substring within longtext.
         * @return Five element string array, containing the prefix of longtext, the
         *     suffix of longtext, the prefix of shorttext, the suffix of shorttext
         *     and the common middle.  Or null if there was no match.
         */
        private string[] HalfmatchI(string longtext, string shorttext, int i)
        {
            // Start with a 1/4 length Substring at position i as a seed.
            string seed = longtext.Substring(i, longtext.Length / 4);

            int index = -1;

            string best_common = String.Empty;
            string best_longtext_a = String.Empty, best_longtext_b = String.Empty;
            string best_shorttext_a = String.Empty, best_shorttext_b = String.Empty;

            while (index < shorttext.Length && (index = shorttext.IndexOf(seed, index + 1, StringComparison.Ordinal)) != -1)
            {
                int prefixLength = CommonPrefix(longtext.Substring(i), shorttext.Substring(index));
                int suffixLength = CommonSuffix(longtext.Substring(0, i), shorttext.Substring(0, index));

                if (best_common.Length < suffixLength + prefixLength)
                {
                    best_common = shorttext.Substring(index - suffixLength, suffixLength) + shorttext.Substring(index, prefixLength);
                    best_longtext_a = longtext.Substring(0, i - suffixLength);
                    best_longtext_b = longtext.Substring(i + prefixLength);
                    best_shorttext_a = shorttext.Substring(0, index - suffixLength);
                    best_shorttext_b = shorttext.Substring(index + prefixLength);
                }
            }

            if (best_common.Length * 2 >= longtext.Length)
            {
                return new string[]{best_longtext_a, best_longtext_b, best_shorttext_a, best_shorttext_b, best_common};
            }
            else
            {
                return null;
            }
        }

        /**
         * Reduce the number of edits by eliminating semantically trivial
         * equalities.
         * @param diffs List of Diff objects.
         */
        public void CleanupSemantic(DiffCollection diffCollection)
        {
            bool changes = false;

            // Stack of indices where equalities are found.
            Stack<int> equalities = new Stack<int>();

            // Always equal to equalities[equalitiesLength-1][1]
            string lastequality = null;
            int pointer = 0;  // Index of current position.
                              // Number of characters that changed prior to the equality.

            int length_insertions1 = 0;
            int length_deletions1 = 0;

            // Number of characters that changed after the equality.
            int length_insertions2 = 0;
            int length_deletions2 = 0;

            while (pointer < diffCollection.Count)
            {
                if (diffCollection[pointer].Operation == Operation.EQUAL)
                {  
                    // Equality found.
                    equalities.Push(pointer);

                    length_insertions1 = length_insertions2;
                    length_deletions1 = length_deletions2;

                    length_insertions2 = 0;
                    length_deletions2 = 0;

                    lastequality = diffCollection[pointer].Text;
                }
                else
                {  
                    // an insertion or deletion
                    if (diffCollection[pointer].Operation == Operation.INSERT)
                    {
                        length_insertions2 += diffCollection[pointer].Text.Length;
                    }
                    else
                    {
                        length_deletions2 += diffCollection[pointer].Text.Length;
                    }

                    // Eliminate an equality that is smaller or equal to the edits on both
                    // sides of it.
                    if (lastequality != null && (lastequality.Length <= Math.Max(length_insertions1, length_deletions1)) && 
                        (lastequality.Length <= Math.Max(length_insertions2, length_deletions2)))
                    {
                        // Duplicate record.
                        diffCollection.Insert(equalities.Peek(), new Diff(Operation.DELETE, lastequality));

                        // Change second copy to insert.
                        diffCollection[equalities.Peek() + 1].Operation = Operation.INSERT;

                        // Throw away the equality we just deleted.
                        equalities.Pop();

                        if (equalities.Count > 0)
                        {
                            equalities.Pop();
                        }

                        pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                        length_insertions1 = 0;  // Reset the counters.
                        length_deletions1 = 0;
                        length_insertions2 = 0;
                        length_deletions2 = 0;

                        lastequality = null;
                        changes = true;
                    }
                }

                pointer++;
            }

            // Normalize the diff.
            if (changes)
            {
                this.CleanupMerge(diffCollection);
            }

            this.CleanupSemanticLossless(diffCollection);

            // Find any overlaps between deletions and insertions.
            // e.g: <del>abcxxx</del><ins>xxxdef</ins>
            //   -> <del>abc</del>xxx<ins>def</ins>
            // e.g: <del>xxxabc</del><ins>defxxx</ins>
            //   -> <ins>def</ins>xxx<del>abc</del>
            // Only extract an overlap if it is as big as the edit ahead or behind it.
            pointer = 1;

            while (pointer < diffCollection.Count)
            {
                if (diffCollection[pointer - 1].Operation == Operation.DELETE && diffCollection[pointer].Operation == Operation.INSERT)
                {
                    string deletion = diffCollection[pointer - 1].Text;
                    string insertion = diffCollection[pointer].Text;

                    int overlap_length1 = CommonOverlap(deletion, insertion);
                    int overlap_length2 = CommonOverlap(insertion, deletion);

                    if (overlap_length1 >= overlap_length2)
                    {
                        if (overlap_length1 >= deletion.Length / 2.0 || overlap_length1 >= insertion.Length / 2.0)
                        {
                            // Overlap found.
                            // Insert an equality and trim the surrounding edits.
                            diffCollection.Insert(pointer, new Diff(Operation.EQUAL, insertion.Substring(0, overlap_length1)));
                            diffCollection[pointer - 1].Text =  deletion.Substring(0, deletion.Length - overlap_length1);
                            diffCollection[pointer + 1].Text = insertion.Substring(overlap_length1);

                            pointer++;
                        }
                    }
                    else
                    {
                        if (overlap_length2 >= deletion.Length / 2.0 || overlap_length2 >= insertion.Length / 2.0)
                        {
                            // Reverse overlap found.
                            // Insert an equality and swap and trim the surrounding edits.
                            diffCollection.Insert(pointer, new Diff(Operation.EQUAL, deletion.Substring(0, overlap_length2)));

                            diffCollection[pointer - 1].Operation = Operation.INSERT;
                            diffCollection[pointer - 1].Text = insertion.Substring(0, insertion.Length - overlap_length2);

                            diffCollection[pointer + 1].Operation = Operation.DELETE;
                            diffCollection[pointer + 1].Text = deletion.Substring(overlap_length2);

                            pointer++;
                        }
                    }

                    pointer++;
                }

                pointer++;
            }
        }

        /**
         * Look for single edits surrounded on both sides by equalities
         * which can be shifted sideways to align the edit to a word boundary.
         * e.g: The c<ins>at c</ins>ame. -> The <ins>cat </ins>came.
         * @param diffs List of Diff objects.
         */
        public void CleanupSemanticLossless(DiffCollection diffCollection)
        {
            int pointer = 1;

            // Intentionally ignore the first and last element (don't need checking).
            while (pointer < diffCollection.Count - 1)
            {
                if (diffCollection[pointer - 1].Operation == Operation.EQUAL && diffCollection[pointer + 1].Operation == Operation.EQUAL)
                {
                    // This is a single edit surrounded by equalities.
                    string equality1 = diffCollection[pointer - 1].Text;
                    string edit = diffCollection[pointer].Text;
                    string equality2 = diffCollection[pointer + 1].Text;

                    // First, shift the edit as far left as possible.
                    int commonOffset = this.CommonSuffix(equality1, edit);

                    if (commonOffset > 0)
                    {
                        string commonString = edit.Substring(edit.Length - commonOffset);
                        equality1 = equality1.Substring(0, equality1.Length - commonOffset);
                        edit = commonString + edit.Substring(0, edit.Length - commonOffset);
                        equality2 = commonString + equality2;
                    }

                    // Second, step character by character right,
                    // looking for the best fit.
                    string bestEquality1 = equality1;
                    string bestEdit = edit;
                    string bestEquality2 = equality2;
                    int bestScore = this.CleanupSemanticScore(equality1, edit) + this.CleanupSemanticScore(edit, equality2);

                    while (edit.Length != 0 && equality2.Length != 0 && edit[0] == equality2[0])
                    {
                        equality1 += edit[0];
                        edit = edit.Substring(1) + equality2[0];
                        equality2 = equality2.Substring(1);
                        int score = this.CleanupSemanticScore(equality1, edit) + this.CleanupSemanticScore(edit, equality2);
                        
                        if (score >= bestScore)
                        {
                            bestScore = score;
                            bestEquality1 = equality1;
                            bestEdit = edit;
                            bestEquality2 = equality2;
                        }
                    }

                    if (diffCollection[pointer - 1].Text != bestEquality1)
                    {
                        // We have an improvement, save it back to the diff.
                        if (bestEquality1.Length != 0)
                        {
                            diffCollection[pointer - 1].Text = bestEquality1;
                        }
                        else
                        {
                            diffCollection.RemoveAt(pointer - 1);
                            pointer--;
                        }

                        diffCollection[pointer].Text = bestEdit;

                        if (bestEquality2.Length != 0)
                        {
                            diffCollection[pointer + 1].Text = bestEquality2;
                        }
                        else
                        {
                            diffCollection.RemoveAt(pointer + 1);

                            pointer--;
                        }
                    }
                }

                pointer++;
            }
        }

        /**
         * Given two strings, comAdde a score representing whether the internal
         * boundary falls on logical boundaries.
         * Scores range from 6 (best) to 0 (worst).
         * @param one First string.
         * @param two Second string.
         * @return The score.
         */
        private int CleanupSemanticScore(string one, string two)
        {
            if (one.Length == 0 || two.Length == 0)
            {
                // Edges are the best.
                return 6;
            }
            
            char char1 = one[one.Length - 1];
            char char2 = two[0];

            bool nonAlphaNumeric1 = !Char.IsLetterOrDigit(char1);
            bool nonAlphaNumeric2 = !Char.IsLetterOrDigit(char2);

            bool whitespace1 = nonAlphaNumeric1 && Char.IsWhiteSpace(char1);
            bool whitespace2 = nonAlphaNumeric2 && Char.IsWhiteSpace(char2);

            bool lineBreak1 = whitespace1 && Char.IsControl(char1);
            bool lineBreak2 = whitespace2 && Char.IsControl(char2);

            bool blankLine1 = lineBreak1 && this.BLANKLINEEND.IsMatch(one);
            bool blankLine2 = lineBreak2 && this.BLANKLINESTART.IsMatch(two);

            if (blankLine1 || blankLine2)
            {
                // Five points for blank lines.
                return 5;
            }
            else if (lineBreak1 || lineBreak2)
            {
                // Four points for line breaks.
                return 4;
            }
            else if (nonAlphaNumeric1 && !whitespace1 && whitespace2)
            {
                // Three points for end of sentences.
                return 3;
            }
            else if (whitespace1 || whitespace2)
            {
                // Two points for whitespace.
                return 2;
            }
            else if (nonAlphaNumeric1 || nonAlphaNumeric2)
            {
                // One point for non-alphanumeric.
                return 1;
            }
            return 0;
        }

        // Define some regex patterns for matching boundaries.
        private Regex BLANKLINEEND = new Regex("\\n\\r?\\n\\Z");
        private Regex BLANKLINESTART = new Regex("\\A\\r?\\n\\r?\\n");

        /**
         * Reorder and merge like edit sections.  Merge equalities.
         * Any edit section can move as long as it doesn't cross an equality.
         * @param diffs List of Diff objects.
         */
        public void CleanupMerge(DiffCollection diffCollection)
        {
            // Add a dummy entry at the end.
            diffCollection.Add(new Diff(Operation.EQUAL, String.Empty));

            int pointer = 0;
            int count_delete = 0;
            int count_insert = 0;

            string text_delete = String.Empty;
            string text_insert = String.Empty;

            int commonlength;

            while (pointer < diffCollection.Count)
            {
                switch (diffCollection[pointer].Operation)
                {
                    case Operation.INSERT:
                        count_insert++;
                        text_insert += diffCollection[pointer].Text;
                        pointer++;
                        break;

                    case Operation.DELETE:
                        count_delete++;
                        text_delete += diffCollection[pointer].Text;
                        pointer++;
                        break;

                    case Operation.EQUAL:
                        // Upon reaching an equality, check for prior redundancies.
                        if (count_delete + count_insert > 1)
                        {
                            if (count_delete != 0 && count_insert != 0)
                            {
                                // Factor out any common prefixies.
                                commonlength = this.CommonPrefix(text_insert, text_delete);

                                if (commonlength != 0)
                                {
                                    if ((pointer - count_delete - count_insert) > 0 &&
                                      diffCollection[pointer - count_delete - count_insert - 1].Operation == Operation.EQUAL)
                                    {
                                        diffCollection[pointer - count_delete - count_insert - 1].Text += text_insert.Substring(0, commonlength);
                                    }
                                    else
                                    {
                                        diffCollection.Insert(0, new Diff(Operation.EQUAL, text_insert.Substring(0, commonlength)));

                                        pointer++;
                                    }

                                    text_insert = text_insert.Substring(commonlength);
                                    text_delete = text_delete.Substring(commonlength);
                                }

                                // Factor out any common suffixies.
                                commonlength = this.CommonSuffix(text_insert, text_delete);

                                if (commonlength != 0)
                                {
                                    diffCollection[pointer].Text = text_insert.Substring(text_insert.Length - commonlength) + diffCollection[pointer].Text;
                                    text_insert = text_insert.Substring(0, text_insert.Length - commonlength);
                                    text_delete = text_delete.Substring(0, text_delete.Length - commonlength);
                                }
                            }

                            // Delete the offending records and add the merged ones.
                            if (count_delete == 0)
                            {
                                diffCollection.Splice(pointer - count_insert, count_delete + count_insert,
                                    new Diff(Operation.INSERT, text_insert));
                            }
                            else if (count_insert == 0)
                            {
                                diffCollection.Splice(pointer - count_delete, count_delete + count_insert,
                                    new Diff(Operation.DELETE, text_delete));
                            }
                            else
                            {
                                diffCollection.Splice(pointer - count_delete - count_insert, count_delete + count_insert,
                                    new Diff(Operation.DELETE, text_delete),
                                    new Diff(Operation.INSERT, text_insert));
                            }

                            pointer = pointer - count_delete - count_insert + (count_delete != 0 ? 1 : 0) + (count_insert != 0 ? 1 : 0) + 1;
                        }
                        else if (pointer != 0 && diffCollection[pointer - 1].Operation == Operation.EQUAL)
                        {
                            // Merge this equality with the previous one.
                            diffCollection[pointer - 1].Text += diffCollection[pointer].Text;
                            diffCollection.RemoveAt(pointer);
                        }
                        else
                        {
                            pointer++;
                        }

                        count_insert = 0;
                        count_delete = 0;
                        text_delete = String.Empty;
                        text_insert = String.Empty;

                        break;
                }
            }

            if (diffCollection[diffCollection.Count - 1].Text.Length == 0)
            {
                diffCollection.RemoveAt(diffCollection.Count - 1);  // Remove the dummy entry at the end.
            }

            // Second pass: look for single edits surrounded on both sides by
            // equalities which can be shifted sideways to eliminate an equality.
            // e.g: A<ins>BA</ins>C -> <ins>AB</ins>AC
            bool changes = false;

            pointer = 1;

            // Intentionally ignore the first and last element (don't need checking).
            while (pointer < (diffCollection.Count - 1))
            {
                if (diffCollection[pointer - 1].Operation == Operation.EQUAL && diffCollection[pointer + 1].Operation == Operation.EQUAL)
                {
                    // This is a single edit surrounded by equalities.
                    if (diffCollection[pointer].Text.EndsWith(diffCollection[pointer - 1].Text, StringComparison.Ordinal))
                    {
                        // Shift the edit over the previous equality.
                        diffCollection[pointer].Text = diffCollection[pointer - 1].Text + diffCollection[pointer].Text.Substring(0, diffCollection[pointer].Text.Length -
                                                          diffCollection[pointer - 1].Text.Length);

                        diffCollection[pointer + 1].Text = diffCollection[pointer - 1].Text + diffCollection[pointer + 1].Text;
                        diffCollection.Splice(pointer - 1, 1);

                        changes = true;
                    }
                    else if (diffCollection[pointer].Text.StartsWith(diffCollection[pointer + 1].Text, StringComparison.Ordinal))
                    {
                        // Shift the edit over the next equality.
                        diffCollection[pointer - 1].Text += diffCollection[pointer + 1].Text;
                        diffCollection[pointer].Text = diffCollection[pointer].Text.Substring(diffCollection[pointer + 1].Text.Length) + diffCollection[pointer + 1].Text;
                        diffCollection.Splice(pointer + 1, 1);

                        changes = true;
                    }
                }

                pointer++;
            }
            // If shifts were made, the diff needs reordering and another shift sweep.
            if (changes)
            {
                this.CleanupMerge(diffCollection);
            }
        }

        /**
         * loc is a location in reference, comAdde and return the equivalent location in
         * textToCompare.
         * e.g. "The cat" vs "The big cat", 1->1, 5->8
         * @param diffs List of Diff objects.
         * @param loc Location within reference.
         * @return Location within textToCompare.
         */
        public int Index(DiffCollection diffCollection, int loc)
        {
            int chars1 = 0;
            int chars2 = 0;
            int last_chars1 = 0;
            int last_chars2 = 0;
            Diff lastDiff = null;

            foreach (Diff diff in diffCollection)
            {
                if (diff.Operation != Operation.INSERT)
                {
                    // Equality or deletion.
                    chars1 += diff.Text.Length;
                }
                if (diff.Operation != Operation.DELETE)
                {
                    // Equality or insertion.
                    chars2 += diff.Text.Length;
                }
                if (chars1 > loc)
                {
                    // Overshot the location.
                    lastDiff = diff;
                    break;
                }

                last_chars1 = chars1;
                last_chars2 = chars2;
            }

            if (lastDiff != null && lastDiff.Operation == Operation.DELETE)
            {
                // The location was deleted.
                return last_chars2;
            }

            // Add the remaining character length.
            return last_chars2 + (loc - last_chars1);
        }

        /**
         * Compute and return the source text (all equalities and deletions).
         * @param diffs List of Diff objects.
         * @return Source text.
         */
        public string Reference(DiffCollection diffCollection)
        {
            StringBuilder text = new StringBuilder();
            foreach (Diff diff in diffCollection)
            {
                if (diff.Operation != Operation.INSERT)
                {
                    text.Append(diff.Text);
                }
            }

            return text.ToString();
        }

        /**
         * Compute and return the destination text (all equalities and insertions).
         * @param diffs List of Diff objects.
         * @return Destination text.
         */
        public string TextToCompare(DiffCollection diffCollection)
        {
            StringBuilder text = new StringBuilder();
            foreach (Diff diff in diffCollection)
            {
                if (diff.Operation != Operation.DELETE)
                {
                    text.Append(diff.Text);
                }
            }

            return text.ToString();
        }

        /**
         * Compute the Levenshtein distance; the number of inserted, deleted or
         * substituted characters.
         * @param diffs List of Diff objects.
         * @return Number of changes.
         */
        public int Levenshtein(DiffCollection diffCollection)
        {
            int levenshtein = 0;
            int insertions = 0;
            int deletions = 0;

            foreach (Diff diff in diffCollection)
            {
                switch (diff.Operation)
                {
                    case Operation.INSERT:
                        insertions += diff.Text.Length;
                        break;

                    case Operation.DELETE:
                        deletions += diff.Text.Length;
                        break;

                    case Operation.EQUAL:
                        // A deletion and an insertion is one substitution.
                        levenshtein += Math.Max(insertions, deletions);
                        insertions = 0;
                        deletions = 0;
                        break;
                }
            }

            levenshtein += Math.Max(insertions, deletions);

            return levenshtein;
        }
    }
}
