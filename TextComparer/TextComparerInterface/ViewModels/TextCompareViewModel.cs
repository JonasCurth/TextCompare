using Commands;
using Compare.Utils;
using Compare.Utils.Objects;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TextComparerInterface.Helper;
using Xceed.Wpf.Toolkit;

namespace TextComparerInterface.ViewModels
{
    class TextCompareViewModel : ViewModelBase
    {
        private string reference;
        private string textToCompare;

        private bool isLockedReference;
        private bool isLockedTextToCompare;

        private DelegateCommand computeCommand;
        private DelegateCommand openReferenceCommand;
        private DelegateCommand openTextToConpareCommand;

        public string Reference
        {
            get
            {
                
                return this.reference;
            }
            set
            {
                this.reference = value;
                OnPropertyChanged("Reference");
            }
        }

        public string TextToCompare
        {
            get
            {
                return this.textToCompare;
            }
            set
            {
                this.textToCompare = value;
                OnPropertyChanged("TextToCompare"); }
        }

        public bool IsLockedReference
        {
            get
            {
                return this.isLockedReference;
            }
            set
            {
                this.isLockedReference = value;
                OnPropertyChanged("IsLockedReference");
            }
        }

        public bool IsLockedTextToCompare
        {
            get
            {
                return this.isLockedTextToCompare;
            }
            set
            {
                this.isLockedTextToCompare = value;
                OnPropertyChanged("IsLockedTextToCompare");
            }
        }

        public ICommand ComputeCommand
        {
            get
            {
                if(null == this.computeCommand)
                {
                    this.computeCommand = new DelegateCommand(this.Compute);
                }

                return this.computeCommand;
            }
        }

        public ICommand OpenReferenceCommand
        {
            get
            {
                if (null == this.openReferenceCommand)
                {
                    this.openReferenceCommand = new DelegateCommand(this.OpenReference);
                }

                return this.openReferenceCommand;
            }
        }

        public ICommand OpenTextToCompareCommand
        {
            get
            {
                if (null == this.openTextToConpareCommand)
                {
                    this.openTextToConpareCommand = new DelegateCommand(this.OpenTextToCompare);
                }

                return this.openTextToConpareCommand;
            }
        }
        
        public void OpenTextToCompare()
        {
            try
            {
                this.TextToCompare = File.ReadAllText(this.OpenFileDialog(), Encoding.Default).ToXAML();
            }
            catch (Exception)
            {
            }
        }

        private string OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".txt";
            openFileDialog.Filter = "Text files (*.txt;)|*.txt;|All files (*.*)|*.*";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            openFileDialog.ShowDialog();

            return openFileDialog.FileName;
        }

        public void OpenReference()
        {
            try
            {
                this.Reference = File.ReadAllText(this.OpenFileDialog(), Encoding.Default).ToXAML();
            }
            catch (Exception)
            {
            }
        }

        public TextCompareViewModel()
        {
            //this.TextToCompare += @"<Section xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xml:space=""preserve"" TextAlignment=""Left"" LineHeight=""Auto"" IsHyphenationEnabled=""False"" xml:lang=""en-us"" FlowDirection=""LeftToRight"" NumberSubstitution.CultureSource=""User"" NumberSubstitution.Substitution=""AsCulture"" FontFamily=""Segoe UI"" FontStyle=""Normal"" FontWeight=""Normal"" FontStretch=""Normal"" FontSize=""12"" Foreground=""#FF000000"" Typography.StandardLigatures=""True"" Typography.ContextualLigatures=""True"" Typography.DiscretionaryLigatures=""False"" Typography.HistoricalLigatures=""False"" Typography.AnnotationAlternates=""0"" Typography.ContextualAlternates=""True"" Typography.HistoricalForms=""False"" Typography.Kerning=""True"" Typography.CapitalSpacing=""False"" Typography.CaseSensitiveForms=""False"" Typography.StylisticSet1=""False"" Typography.StylisticSet2=""False"" Typography.StylisticSet3=""False"" Typography.StylisticSet4=""False"" Typography.StylisticSet5=""False"" Typography.StylisticSet6=""False"" Typography.StylisticSet7=""False"" Typography.StylisticSet8=""False"" Typography.StylisticSet9=""False"" Typography.StylisticSet10=""False"" Typography.StylisticSet11=""False"" Typography.StylisticSet12=""False"" Typography.StylisticSet13=""False"" Typography.StylisticSet14=""False"" Typography.StylisticSet15=""False"" Typography.StylisticSet16=""False"" Typography.StylisticSet17=""False"" Typography.StylisticSet18=""False"" Typography.StylisticSet19=""False"" Typography.StylisticSet20=""False"" Typography.Fraction=""Normal"" Typography.SlashedZero=""False"" Typography.MathematicalGreek=""False"" Typography.EastAsianExpertForms=""False"" Typography.Variants=""Normal"" Typography.Capitals=""Normal"" Typography.NumeralStyle=""Normal"" Typography.NumeralAlignment=""Normal"" Typography.EastAsianWidths=""Normal"" Typography.EastAsianLanguage=""Normal"" Typography.StandardSwashes=""0"" Typography.ContextualSwashes=""0"" Typography.StylisticAlternates=""0""><Paragraph><Run>This is the </Run><Run FontWeight=""Bold"" Foreground=""Red"" FontStyle=""StrikeOut"">RichTextBox</Run></Paragraph></Section>";
        }

        public void Compute()
        {
            Comparer difference = new Comparer();

            string reference = this.Reference.FromXAML();
            string textToCompare = this.TextToCompare.FromXAML();

            try
            {
                List<Diff> diffs = difference.Compare(reference, textToCompare, true);
                this.TextToCompare = diffs.ToXAML();
            }
            catch { }
        }
    }
}
