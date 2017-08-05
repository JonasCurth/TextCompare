using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using utils.Objects;

namespace TextComparerInterface.Helper
{
    public static class XAMLWrapper
    {
        public static string SectionStartTag = @"<Section xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xml:space=""preserve"">";
        public static string SectionEndTag = "</Section>";

        public static string ParagraphStartTag = "<Paragraph>";
        public static string ParagraphEndTag = "</Paragraph>";

        public static string RunStartTag = "<Run>";
        public static string RunEndTag = "</Run>";

        public static string ForegroundProperty = "Foreground=";
        public static string FontWeightProperty = "FontWeight=";
        public static string FontStyleProperty = "FontStyle=";
        //<Paragraph><Run>This is the </Run><Run FontWeight=""Bold"">RichTextBox</Run></Paragraph></Section>";

        public static string FromXAML(this string xaml)
        {
            if (String.IsNullOrEmpty(xaml))
            {
                return null;
            }

            if (1 < Regex.Matches(xaml, ParagraphEndTag).Count)
            {
                xaml = Regex.Replace(xaml, ParagraphEndTag, "\r\n");
            }

            return Regex.Replace(xaml, "<.*?>|&.*?;|>", String.Empty);
        }

        public static string ToXAML(this string plaintext)
        {
            if(String.IsNullOrEmpty(plaintext))
            {
                return null;
            }

            if(plaintext.Contains(SectionStartTag) && plaintext.Contains(SectionStartTag))
            {
                return plaintext;
            }

            StringBuilder xaml = new StringBuilder();
            String[] lines = plaintext./*Replace("\r\n", "|").Replace('\u00b6', '|').*/Split(new char[] { '\u00b6' });
            xaml.Append(SectionStartTag);

            foreach (string line in lines)
            {
                string xamlLine = ParagraphStartTag +
                                    RunStartTag + line +
                                    RunEndTag + ParagraphEndTag;

                xaml.Append(xamlLine);                                    
            }

            xaml.Append(SectionEndTag);

            return xaml.ToString();
        }

        public static string ToXAML(this List<Diff> diffs)
        {
            if(null == diffs)
            {
                return null;
            }

            StringBuilder xaml = new StringBuilder();
            xaml.Append(SectionStartTag);
            xaml.Append(ParagraphStartTag);

            foreach (Diff diff in diffs)
            {
                if (diff.Text.Contains('\u00b6'))
                {
                    string[] lines = diff.Text.Split(new Char[] { '\u00b6' });

                    foreach (string line in lines)
                    {
                        string xamlLine = RunStartTag +
                                            line + RunStartTag +
                                            ParagraphEndTag + ParagraphStartTag;
                        xaml.Append(xamlLine);
                    }
                }
                else
                {
                    StringBuilder runStartTag = new StringBuilder();
                    runStartTag.Append("<Run ");

                    switch (diff.Operation)
                    {
                        case Operation.DELETE:
                            runStartTag.Append($@"{ForegroundProperty}""Blue""");
                            break;
                        case Operation.INSERT:
                            runStartTag.Append($@"{ForegroundProperty}""Red""");
                            break;
                        case Operation.EQUAL:
                            break;
                    }

                    runStartTag.Append(">");


                    string xamlLine = runStartTag + /*(Operation.INSERT == diff.Operation ? "<Underline>" : "") +*/
                                        diff.Text + /*(Operation.INSERT == diff.Operation ? "</Underline>" : "") +*/
                                        RunEndTag;

                    xaml.Append(xamlLine);
                }                
            }

            xaml.Append(ParagraphEndTag);
            xaml.Append(SectionEndTag);

            return xaml.ToString();
        }

        private static string ConvertHex(this string text)
        {
            StringBuilder newText = new StringBuilder();
            foreach (char item in text)
            {
                newText.Append($"&#{(int)item};");
            }

            return newText.ToString();
        }

    }
}
