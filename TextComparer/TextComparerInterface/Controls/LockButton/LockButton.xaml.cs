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

namespace TextComparerInterface.Controls.LockButton
{
    /// <summary>
    /// Interaktionslogik für CheckButton.xaml
    /// </summary>
    public partial class LockButton : UserControl
    {
        public LockButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsLockedProperty
            = DependencyProperty.Register("Text", typeof(bool), typeof(LockButton),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsLocked
        {
            get { return (bool)GetValue(IsLockedProperty); }
            set { SetValue(IsLockedProperty, value); }
        }
    }
}
