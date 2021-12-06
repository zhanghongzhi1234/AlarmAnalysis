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

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow : Window
    {
        public string textInput;

        public TextInputWindow()
        {
            InitializeComponent();
            btnOK.Click += btnOK_Click;
            btnCancel.Click += btnCancel_Click;
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.textInput = txtContent.Text;
            this.DialogResult = true;
            this.Close();
        }


    }
}
