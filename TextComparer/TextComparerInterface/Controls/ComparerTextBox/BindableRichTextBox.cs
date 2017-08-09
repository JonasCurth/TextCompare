using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TextComparerInterface.Controls.ComparerTextBox
{
    public class BindableRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register("Document", typeof(FlowDocument),
            typeof(BindableRichTextBox), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
    }
}
