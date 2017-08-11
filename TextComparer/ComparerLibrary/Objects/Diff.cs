using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Comparer.Utils.Objects
{
    /// <summary>
    /// Class representing one difference.
    /// </summary>
    public class Diff
    {
        public Operation Operation { get; set; }

        public string Text { get; set; }

        public Run Run
        {
            get
            {
                if (!String.IsNullOrEmpty(this.Text))
                {
                    Run run = new Run(this.Text);

                    switch (Operation)
                    {
                        case Operation.DELETE:
                            run.Foreground = Brushes.Blue;
                            break;
                        case Operation.INSERT:
                            run.TextDecorations = TextDecorations.Underline;
                            run.Foreground = Brushes.Red;
                            break;
                        case Operation.EQUAL:
                            run.Foreground = Brushes.Black;
                            break;
                        default:
                            break;
                    }

                    return run;
                }

                return null;
            }
        }

        public Diff(Operation operation, string text)
        {
            this.Operation = operation;
            this.Text = text;
        }

        public override string ToString()
        {
            string prettyText = this.Text.Replace('\n', '\u00b6');
            return "Diff(" + this.Operation + ",\"" + prettyText + "\")";
        }

        public override bool Equals(Object obj)
        {
            if (null == obj)
            {
                return false;
            }

            Diff diff = obj as Diff;
            if (null == (Object)diff)
            {
                return false;
            }

            return diff.Operation == this.Operation && diff.Text == this.Text;
        }

        public bool Equals(Diff obj)
        {
            if (null == obj)
            {
                return false;
            }

            return obj.Operation == this.Operation && obj.Text == this.Text;
        }

        public override int GetHashCode()
        {
            return this.Text.GetHashCode() ^ this.Operation.GetHashCode();
        }
    }

}
