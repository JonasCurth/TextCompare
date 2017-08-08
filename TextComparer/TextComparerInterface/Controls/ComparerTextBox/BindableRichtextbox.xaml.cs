using System;
using System.Collections.Generic;
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

namespace TextComparerInterface.Controls.ComparerTextBox
{
    /// <summary>
    /// Interaktionslogik für BindableRichtextbox.xaml
    /// </summary>
    public partial class BindableRichtextbox : UserControl
    {

        private int internalUpdatePending;
        private bool textHasChanged;

        public BindableRichtextbox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty PlaceholderProperty
            = DependencyProperty.Register("Placeholder", typeof(string), typeof(BindableRichtextbox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Placeholder
        {
            get { return (string)GetValue(PlaceholderProperty); }
            set { SetValue(PlaceholderProperty, value); }
        }

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(FlowDocument),
            typeof(BindableRichtextbox), new PropertyMetadata(OnDocumentChanged));

        public FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            /* For unknown reasons, this method gets called twice when the 
             * Document property is set. Until we figure out why, we initialize
             * the flag to 2 and decrement it each time through this method. */

            // Initialize
            BindableRichtextbox thisControl = (BindableRichtextbox)d;

            // Exit if this update was internally generated
            if (thisControl.internalUpdatePending > 0)
            {

                // Decrement flags and exit
                thisControl.internalUpdatePending--;
                return;
            }

            // Set Document property on RichTextBox
            thisControl.TextBox.Document = (e.NewValue == null) ? new FlowDocument() : (FlowDocument)e.NewValue;

            // Reset flag
            thisControl.textHasChanged = false;

            if (thisControl.textHasChanged)
            {
                thisControl.PlaceholderText.Visibility = 
                    (thisControl.TextBox.Document.Blocks.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Set the TextChanged flag
            textHasChanged = true;
        }

        public void UpdateDocumentBindings()
        {
            // Exit if text hasn't changed
            if (!this.textHasChanged)
            {
                return;
            }

            // Set 'Internal Update Pending' flag
            internalUpdatePending = 2;

            // Set Document property
            SetValue(DocumentProperty, this.TextBox.Document);
        }
    }
}
