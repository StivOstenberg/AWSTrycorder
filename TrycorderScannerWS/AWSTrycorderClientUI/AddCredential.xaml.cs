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

namespace AWSTrycorderClientUI
{
    /// <summary>
    /// Interaction logic for AddCredential.xaml
    /// </summary>
    public partial class AddCredential : Window
    {
        AWSFunctions.ScanAWS scanner = new AWSFunctions.ScanAWS();
        public AddCredential()
        {
            InitializeComponent();
            
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            resultlabel.Content = scanner.AddCredential(ProfileTextbox.Text,AccessKeyTextbox.Text,SecretKeyTextBox.Text);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            resultlabel.Content = scanner.DeleteCredential(ProfileTextbox.Text);
        }
    }
}
