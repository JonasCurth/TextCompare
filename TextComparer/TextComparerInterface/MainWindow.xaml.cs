using Comparer.Utils.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextComparerInterface.ViewModels;

namespace TextComparerInterface
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TextCompareViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            this.vm = this.DataContext as TextCompareViewModel;
            this.vm.PropertyChanged += this.OnPropertyChanged;
            

        }
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Reference"))
            {
                this.OnReferenceChanged();
            }
            else if (e.PropertyName.Equals("TextToCompare"))
            {
                this.OnTextToCompareChanged();
            }
        }

        private void OnReferenceChanged()
        {
            this.ReferenceWindow.Editor.Document = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            paragraph.Inlines.Add(this.vm.Reference);

            this.ReferenceWindow.Editor.Document.Blocks.Add(paragraph);
            
        }

        private void OnTextToCompareChanged()
        {
            this.TextToCompareWindow.Editor.Document = new FlowDocument();
            Paragraph paragraph = new Paragraph();

            string[] runs = this.vm.TextToCompare.Split(new char[] { '|' });

            List<Run> runList = new List<Run>();

            foreach (var runString in runs)
            {
                if(runs.Length == 1)
                {
                    paragraph.Inlines.Add(new Run(this.vm.TextToCompare));
                    break;
                }
                else
                {
                    string text = runString.Substring(3, runString.Length - 6);
                    Run run = new Run(text);

                    if (runString.StartsWith($"[{(int)Operation.DELETE}]"))
                    {
                        run.Foreground = Brushes.Blue;
                    }
                    else if (runString.StartsWith($"[{(int)Operation.INSERT}]"))
                    {
                        run.Foreground = Brushes.Red;
                        run.TextDecorations = TextDecorations.Strikethrough;
                    }

                    runList.Add(run);
                }

                paragraph.Inlines.AddRange(runList);
            }
            
            this.TextToCompareWindow.Editor.Document.Blocks.Add(paragraph);
        }
    }
}
