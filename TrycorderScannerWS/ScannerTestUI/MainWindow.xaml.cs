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
            if (ProfilesComboBox.SelectedItem.ToString().Length > 2 && RegionListcomboBox.SelectedItem.ToString().Length >1)
            {
                var datable = Scanner.GetS3Buckets(ProfilesComboBox.SelectedItem.ToString(),RegionListcomboBox.SelectedItem.ToString());
                DasGrid.ItemsSource = datable.DefaultView;
            }
            else
            {
                MessageBox.Show("You fool, you must select a profile and region for to scan S3!");
            }
        }

        private void ListSubnetsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesComboBox.SelectedItem.ToString().Length > 2)
            {
                var datable = Scanner.GetSubnets(ProfilesComboBox.SelectedItem.ToString(),RegionListcomboBox.SelectedItem.ToString());
                DasGrid.ItemsSource = datable.DefaultView;
            }
            else
            {
                MessageBox.Show("You fool, you must select a profile for to scan Subnets!");
            }
        }

        private void initializeMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ListCertsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetCertDetails(ProfilesComboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void ListSQSMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetSQSQ(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void ListEBSMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetEBSDetails(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void ListSNSMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetSNSSubscriptions(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            var goob = datable.Rows[0];
            var cross = goob[8];
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void S3SizesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.S3SizeCloudWatch(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());

            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void CreateUserRequest_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<string> myprofile = new string[] { ProfilesComboBox.SelectedItem.ToString() };

            var datable = Scanner.CreateUserRequestTable(myprofile);
        }

        private void ELBsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetELBs(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void DNSsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<string> myprofile = new string[] { ProfilesComboBox.SelectedItem.ToString() };
            var datable = Scanner.ScanDNS(myprofile);
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void ENIsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetENIs(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void BeanStalkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable= Scanner.GetBeans(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }

        private void ASGsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var datable = Scanner.GetASGs(ProfilesComboBox.SelectedItem.ToString(), RegionListcomboBox.SelectedItem.ToString());
            DasGrid.ItemsSource = datable.DefaultView;
        }
    }
}
