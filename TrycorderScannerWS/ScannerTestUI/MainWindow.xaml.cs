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

using AWSFunctions;



namespace ScannerTestUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
    AWSFunctions.ScanAWS Scanner = new ScanAWS();

        public MainWindow()
        {
            InitializeComponent();
            ProfilesComboBox.ItemsSource = Scanner.GetProfileNames();
            ProfilesComboBox.SelectedIndex = 12;
            FillRegions();
        }

        private void FillRegions()
        {
            RegionListcomboBox.ItemsSource = Scanner.GetRegionNames();
            RegionListcomboBox.SelectedIndex = 0;
        }
        private void GetProfilesClick(object sender, RoutedEventArgs e)
        {
            
             ProfilesComboBox.ItemsSource = Scanner.GetProfileNames();
            
            
        }

        private void ProfilesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ListUsersMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesComboBox.SelectedItem.ToString().Length > 2)
            {
                var datable = Scanner.GetIAMUsers(ProfilesComboBox.SelectedItem.ToString());
                DasGrid.ItemsSource = datable.DefaultView;
            }
            else
            {
                MessageBox.Show("You fool, you must select a profile!");
                return;
            }

        }

        private void ListEC2MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesComboBox.SelectedItem.ToString().Length > 2 & RegionListcomboBox.SelectedItem.ToString().Length >2 )
            {
                var datable = Scanner.GetEC2Instances(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
                DasGrid.ItemsSource = datable.DefaultView;
            }
            else
            {
                MessageBox.Show("You fool, you must select a profile and region to scan EC2!");
            }
        }

        private void ListS3MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesComboBox.SelectedItem.ToString().Length > 2 )
            {
                var datable = Scanner.GetS3Buckets(ProfilesComboBox.SelectedItem.ToString());
                DasGrid.ItemsSource = datable.DefaultView;
            }
            else
            {
                MessageBox.Show("You fool, you must select a profile for to scan S3!");
            }
        }
    }
}
