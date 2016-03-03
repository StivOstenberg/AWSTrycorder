using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
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

namespace AWSTrycorderClientUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string MyEndpoint = "net.tcp://127.0.0.1:8383/Scanner";
        NetTcpBinding bindbert = new NetTcpBinding(SecurityMode.None, true);
        
        ServiceHost host = new ServiceHost(typeof(ScannerEngine.ScannerClass));
        IChannelFactory<ScannerEngine.ScannerInterfaceDefinition> MyScanneriChannel;
        ScannerEngine.ScannerInterfaceDefinition Trycorder;
        static Action S_Event2 = delegate { };


        public MainWindow()
        {
            InitializeComponent();
            bindbert.MaxReceivedMessageSize = 2147483647;//Maximum
            bindbert.MaxBufferSize=2147483647;//Maximum
            

            MyScanneriChannel = new ChannelFactory<ScannerEngine.ScannerInterfaceDefinition>(bindbert);
            StartWCFService();
            var ender = new EndpointAddress(MyEndpoint);
            Trycorder = MyScanneriChannel.CreateChannel(ender);

            Trycorder.Initialize();
            BuildProfileMenuList();
            BuildRegionMenuList();
            BuildComponentMenuList();
            ConfigureComponentSelectComboBox();
        }

        public void StartWCFService()
        {
            try
            {
                //host = new ServiceHost(typeof(ScannerEngine.ScannerClass), new Uri(MyEndpoint));
                {
                    bindbert.MaxReceivedMessageSize = 2147483647;
                    bindbert.MaxBufferSize = 2147483647;

                    host.AddServiceEndpoint(typeof(ScannerEngine.ScannerInterfaceDefinition), bindbert, MyEndpoint);

                    // Enable metadata exchange
                    ServiceMetadataBehavior smb = new ServiceMetadataBehavior() { HttpGetEnabled = false };
                    host.Description.Behaviors.Add(smb);
                    // Enable exeption details
                    ServiceDebugBehavior sdb = host.Description.Behaviors.Find<ServiceDebugBehavior>();
                    sdb.IncludeExceptionDetailInFaults = true;
                    host.Open();



                }


            }
            catch (Exception ex)
            {
                host.Abort();
                MessageBox.Show("Error = " + ex.Message);
            }

        }







        #region UI Setup function


        public void BuildProfileMenuList()
        {
            System.Windows.Controls.MenuItem Proot = (System.Windows.Controls.MenuItem)this.TopMenu.Items[1];
            Proot.Items.Clear();
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            mit.Click += CKAllPMI_Click;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            mit2.Click += UCKAllPMI_Click;
            Proot.Items.Add(mit2);



            foreach (KeyValuePair<string, bool> KVP in Trycorder.GetProfiles())
            {

                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = true;
                mi.Header = KVP.Key;

                mi.IsChecked = KVP.Value;
                mi.StaysOpenOnClick = true;
                mi.Click += ProfileChecked;

                Proot.Items.Add(mi);
            }

            foreach(KeyValuePair<string,string> KVP in Trycorder.GetBadProfiles())
            {
                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = false;
                mi.Header = KVP.Key;
                mi.StaysOpenOnClick = true;
                mi.Background = Brushes.Red;
                mi.ToolTip = KVP.Value;
                Proot.Items.Add(mi);
            }


        }

        public void BuildRegionMenuList()
        {
            System.Windows.Controls.MenuItem Proot = (System.Windows.Controls.MenuItem)this.TopMenu.Items[2];
            Proot.Items.Clear();
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            mit.Click += CkAllRMI_Click;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            mit2.Click += UCkAllRMI_Click;
            Proot.Items.Add(mit2);

            foreach (var aregion in Trycorder.GetRegions())  //Build the Region Select Menu
            {

                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = true;
                mi.Header = aregion.Key;
                mi.IsChecked = aregion.Value;
                mi.Click += RegionChecked;
                mi.StaysOpenOnClick = true;
                Proot.Items.Add(mi);
            }
        }

        public void BuildComponentMenuList()
        {
            System.Windows.Controls.MenuItem Proot = (System.Windows.Controls.MenuItem)this.TopMenu.Items[3];
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            mit.Click += CKAllCMP_Click;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            mit2.Click += UCKAllCMP_Click;
            Proot.Items.Add(mit2);
            foreach(var compy in Trycorder.GetComponents())
            {
                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = true;
                mi.Header = compy.Key;
                mi.IsChecked = compy.Value;
                mi.Click += ComponentChecked;
                mi.StaysOpenOnClick = true;
                Proot.Items.Add(mi);
            }
        }

        public void BuildColumnMenuList()
        {
            System.Windows.Controls.MenuItem Proot = (System.Windows.Controls.MenuItem)this.TopMenu.Items[4];
            Proot.Items.Clear();
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            mit.Click += CKAllCMP_Click;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            mit2.Click += UCKAllCMP_Click;
            Proot.Items.Add(mit2);
            System.Windows.Controls.MenuItem mit3 = new System.Windows.Controls.MenuItem();
            mit3.Header = "MostUsed";
            mit3.StaysOpenOnClick = true;
            mit3.Click += UCKAllCMP_Click;
            Proot.Items.Add(mit3);
            foreach (var acol in DasGrid.Columns)
            {
                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = true;
                mi.IsChecked = true;
                mi.Header = acol.Header;
                mi.Click += ColumnChecked;
                mi.StaysOpenOnClick = true;
                Proot.Items.Add(mi);
            }
        }

        public void ConfigureComponentSelectComboBox()
        {
            SelectedComponentComboBox.Items.Clear();
            foreach(var tribble in Trycorder.GetComponents())
            {
                if (tribble.Value) SelectedComponentComboBox.Items.Add(tribble.Key);
            }
        }

        #endregion

        #region EventHandlers
        private void InitializeTestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string initty = Trycorder.Initialize();
            MessageBox.Show(initty, "Initialize results");

        }
        private void CKAllPMI_Click(object sender, RoutedEventArgs e)
        {
            //Checks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ProfilesMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = true;
                    Trycorder.setProfileStatus(anitem.Header.ToString(), true);

                }
            }
            // if (DaGrid.Items.Count > 0) DoEC2Filter();
        }
        private void UCKAllPMI_Click(object sender, RoutedEventArgs e)
        {
            //UnChecks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ProfilesMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = false;
                    Trycorder.setProfileStatus(anitem.Header.ToString(), false);
                }

            }
            //if (DaGrid.Items.Count > 0) DoEC2Filter();

            #endregion
        }
        private void CkAllRMI_Click(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.MenuItem anitem in RegionsMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = true;
                    Trycorder.SetRegionStatus(anitem.Header.ToString(), true);
                }
            }
            //if (DaGrid.Items.Count > 0) DoEC2Filter();
        }
        private void UCkAllRMI_Click(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.MenuItem anitem in RegionsMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = false;
                    Trycorder.SetRegionStatus(anitem.Header.ToString(), false);
                }
            }
            // if (DaGrid.Items.Count > 0) DoEC2Filter();
        }
        private void ProfileChecked(object sender, RoutedEventArgs e)
        {
            var gopher = sender as MenuItem;
            bool state = gopher.IsChecked;
            string theprofile = gopher.Header.ToString();
            Trycorder.setProfileStatus(theprofile, state);
            
            
        }
        private void RegionChecked(object sender, RoutedEventArgs e)
        {
            var gopher = sender as MenuItem;
            bool state = gopher.IsChecked;
            string theregion = gopher.Header.ToString();
            Trycorder.SetRegionStatus(theregion, state);
        }
        private void CheckStatusMI_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Trycorder.GetDetailedStatus(), "Trycorder Status");
        }
        private void ComponentChecked(object sender, RoutedEventArgs e)
        {
            var gopher = sender as MenuItem;
            bool state = gopher.IsChecked;
            string thecomponent = gopher.Header.ToString();
            Trycorder.SetComponentScanBit(thecomponent, state);
            var currentitem = SelectedComponentComboBox.SelectedValue;
            ConfigureComponentSelectComboBox();
            try
            {
                SelectedComponentComboBox.SelectedValue = currentitem;
            }
            catch
            {
                SelectedComponentComboBox.SelectedIndex = 0;
            }
        }

        private void ColumnChecked(object sender, RoutedEventArgs e)
        {

            var gopher = sender as MenuItem;
            bool state = gopher.IsChecked;
            string thecolumn = gopher.Header.ToString();
            ShowHideColumn(thecolumn, state);

        }

        private void ShowHideColumn(string thecolumn,bool state)
        {

            foreach (var acol in DasGrid.Columns)
            {
                if (acol.Header.Equals(thecolumn))
                {
                    if (state) acol.Visibility = Visibility.Visible;
                    else acol.Visibility = Visibility.Hidden;
                }
            }
        }
        private void CKAllCMP_Click(object sender, RoutedEventArgs e)
        {
            //Checks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ProfilesMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = true;
                    Trycorder.SetComponentScanBit(anitem.Header.ToString(), true);
                }
            }
            ConfigureComponentSelectComboBox();
        }
        private void UCKAllCMP_Click(object sender, RoutedEventArgs e)
        {
            //UnChecks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ComponentsMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = false;
                    Trycorder.SetComponentScanBit(anitem.Header.ToString(), false);
                }
            }
            ConfigureComponentSelectComboBox();


        }

        private DataTable GetSelectedDatatable(string Datatable2Get)
        {
            DataTable DaTable = new DataTable();
            //Bring up new table
            switch (Datatable2Get)
            {
                case "EC2":
                    DaTable = Trycorder.GetEC2Table();
                    break;
                case "IAM":
                    DaTable = Trycorder.GetIAMTable();
                    break;
                case "S3":
                    DaTable = Trycorder.GetS3Table();
                    break;
                case "VPC":
                    DaTable = Trycorder.GetVPCTable();
                    break;
                case "Subnets":
                    DaTable = Trycorder.GetSubnetsTable();
                    break;
                case "RDS":
                    DaTable = Trycorder.GetRDSTable();
                    break;
                default:
                    DaTable = Trycorder.GetEC2Table();
                    break;
            }
            return DaTable;

        }
        private void SelectedComponentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string IChooseYou = "";
            try {IChooseYou= SelectedComponentComboBox.SelectedValue.ToString(); }
            catch { IChooseYou = ""; }
            DataTable DaTable = GetSelectedDatatable(IChooseYou);
            TrycorderMainWindow.Title = "AWSTrycorder - " + DaTable.TableName;
            DasGrid.ItemsSource = DaTable.DefaultView;
            SelectColumncomboBox.Items.Clear();
            SelectColumncomboBox.Items.Add("_Any_");
            foreach (DataColumn head in DaTable.Columns)
            {
                SelectColumncomboBox.Items.Add(head.ColumnName);
                SelectColumncomboBox.SelectedIndex = 0;
            }
            BuildColumnMenuList();


        }
        private void ScanMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string startout = Trycorder.GetStatus();
            if (String.Equals(startout, "Idle")) Task.Factory.StartNew(Trycorder.ScanAll);
            else
            {
                return;
            }
        }

        private void ScanCompleted(object sender, RoutedEventArgs e)
        {
            string an = "";

        }

        private void SetSuckerMI_Click(object sender, RoutedEventArgs e)
        {
            var quepaso = Trycorder.GetDetailedStatus();
            var Gottacatchemall = Trycorder.ScanResults();
            var EC2Table = Gottacatchemall.Tables["EC2Table"];
            var contadeena = EC2Table.Rows.Count;
        }

        private void SearchStringTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {

            DoScan();
        }

        private void LoadCredentialsMI_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CaseCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DoScan();
        }

        private void DoScan()
        {
            try
            {
                var Table2Scan = GetSelectedDatatable(SelectedComponentComboBox.SelectedItem.ToString());
                string filterstring = SearchStringTextbox.Text;
                string column2filter = SelectColumncomboBox.SelectedItem.ToString();
                bool casesense = (bool)CaseCheckbox.IsChecked;
                var filttab = Trycorder.FilterDataTablebyCol(Table2Scan, column2filter, filterstring, casesense);

                DasGrid.ItemsSource = filttab.DefaultView;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
            }
        }

        private void CaseCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DoScan();
        }
    }
}