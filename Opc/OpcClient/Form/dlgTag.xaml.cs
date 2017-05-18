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
using System.Windows.Shapes;
using OpcLib.Common;

namespace OpcClient.Form
{
    /// <summary>
    /// Interaction logic for dlgTag.xaml
    /// </summary>
    public partial class dlgTag : Window
    {
        public dlgTag()
        {
            InitializeComponent();
        }
        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtId.Focus();
        }

        public TagModel Answer
        {
            get
            {
                return new TagModel() {Name = txtName.Text};                 
            }
        }
    }
}
