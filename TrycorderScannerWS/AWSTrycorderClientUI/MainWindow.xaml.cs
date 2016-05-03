// AWS Trycorder:  A tool for gathering data across Amazon accounts
//    Copyright(C) 2016  Stiv Ostenberg

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
 //   but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.





using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


using System.Windows.Media;
using System.IO;
using System.Windows.Threading;

namespace AWSTrycorderClientUI
{
    extern alias swformalias;
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
        AWSFunctions.ScanAWS StivFunk = new AWSFunctions.ScanAWS();

        public MainWindow()
        {
            InitializeComponent();
            var ender = new EndpointAddress(MyEndpoint);
            bindbert.MaxReceivedMessageSize = 2147483647;
            bindbert.MaxBufferSize = 2147483647;

            MyScanneriChannel = new ChannelFactory<ScannerEngine.ScannerInterfaceDefinition>(bindbert);
            Trycorder = MyScanneriChannel.CreateChannel(ender);



            StartWCFService();

            Trycorder.Initialize();
            BuildProfileMenuList();
            BuildRegionMenuList();
            BuildComponentMenuList();
            ConfigureComponentSelectComboBox();

            ///Get a timer to update status on UI.
            /// 
            DispatcherTimer UpdateStatusTimer = new DispatcherTimer();
            UpdateStatusTimer.Tick += new EventHandler(updateStatusTimer_Tick);
            UpdateStatusTimer.Interval = new TimeSpan(0, 0, 5);
            UpdateStatusTimer.Start();


        }

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            string statusreport = Trycorder.GetDetailedStatus();
            if (statusreport.ToLower().Contains("scanning"))
            {
                ScanButton.Content = "Scanning";
                ScanButton.Background = Brushes.Yellow;
            }
            else if (ScanButton.Background != Brushes.Green)
            {
                var selkie = SelectedComponentComboBox.SelectedValue;
                ScanButton.Background = Brushes.Green;
                ScanButton.Content = "Scan";


                if (SelectedComponentComboBox.SelectedValue != null)SelectedComponentComboBox.SelectedValue=selkie ;
                UpdateTable();
                DoScan();
            }

        }

        public void StartWCFService()
        {

            try
            {
                //host = new ServiceHost(typeof(ScannerEngine.ScannerClass), new Uri(MyEndpoint));
                {

                    MyScanneriChannel = new ChannelFactory<ScannerEngine.ScannerInterfaceDefinition>(bindbert);
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
            bool Baddies = false;
            foreach (KeyValuePair<string, string> KVP in Trycorder.GetBadProfiles())
            {
                Baddies = true;
                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = false;
                mi.Header = KVP.Key;
                mi.StaysOpenOnClick = true;
                mi.Background = Brushes.Red;
                mi.ToolTip = KVP.Value;
                Proot.Items.Add(mi);
            }
            if (Baddies) RemoveBadMI.Visibility = System.Windows.Visibility.Visible;
            else RemoveBadMI.Visibility = System.Windows.Visibility.Hidden;

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
            foreach (var compy in Trycorder.GetComponents())
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
            var testval = SelectedComponentComboBox.Items.Count;
            if (testval < 1)
            {
                return;
            }
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            mit.Click += CKAllCol_Click;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            mit2.Click += UCKAllCol_Click;
            Proot.Items.Add(mit2);
            System.Windows.Controls.MenuItem mit3 = new System.Windows.Controls.MenuItem();
            mit3.Header = "MostUsed";
            mit3.StaysOpenOnClick = true;
            mit3.Click += UCKMostCol_Click;
            Proot.Items.Add(mit3);


            if (SelectedComponentComboBox.SelectedValue == null) SelectedComponentComboBox.SelectedIndex = 0;
            var vizlist = Trycorder.GetColumnVisSetting(SelectedComponentComboBox.SelectedValue.ToString());

            foreach (var acol in DasGrid.Columns)
            {
                try {
                    System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                    mi.IsCheckable = true;

                    mi.IsChecked = vizlist[acol.Header.ToString()];
                    mi.Header = acol.Header;
                    mi.Click += ColumnChecked;
                    mi.StaysOpenOnClick = true;
                    Proot.Items.Add(mi);
                    var defvis = vizlist[acol.Header.ToString()];
                    ShowHideColumn(acol.Header.ToString(), vizlist[acol.Header.ToString()]);
                }
                catch
                {

                }

            }
        }

        public void ConfigureComponentSelectComboBox()
        {
            bool alreadyset = false;
            string currentcomp = ""; ;
            if (SelectedComponentComboBox.Items.Count < 0)
            {
                currentcomp = SelectedComponentComboBox.SelectedValue.ToString();
                alreadyset = true;
            }
            SelectedComponentComboBox.Items.Clear();
            foreach (var tribble in Trycorder.GetComponents())
            {
                if (tribble.Value) SelectedComponentComboBox.Items.Add(tribble.Key);
            }
            if (alreadyset)
            {
                SelectedComponentComboBox.SelectedValue = currentcomp;
            }
            else
            {
                SelectedComponentComboBox.SelectedIndex = 0;
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
            Trycorder.SetColumnVisSetting(SelectedComponentComboBox.SelectedValue.ToString(), thecolumn, state);
            ShowHideColumn(thecolumn, state);
        }

        private void ShowHideColumn(string thecolumn, bool isvisable)
        {

            foreach (var acol in DasGrid.Columns)
            {
                if (acol.Header.Equals(thecolumn))
                {
                    if (isvisable) acol.Visibility = Visibility.Visible;
                    else acol.Visibility = Visibility.Hidden;
                }
            }
        }
        private void CKAllCMP_Click(object sender, RoutedEventArgs e)
        {


            //Checks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ComponentsMenuItem.Items)
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
        private void CKAllCol_Click(object sender, RoutedEventArgs e)
        {
            //Checks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ColumnsMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = true;
                }
                foreach (var acol in DasGrid.Columns) acol.Visibility = Visibility.Visible;
            }
        }

        private void UCKAllCol_Click(object sender, RoutedEventArgs e)
        {
            //Checks all Profilemenu items
            foreach (System.Windows.Controls.MenuItem anitem in ColumnsMenuItem.Items)
            {
                if (anitem.IsCheckable)
                {
                    anitem.IsChecked = false;
                }
                foreach (var acol in DasGrid.Columns) acol.Visibility = Visibility.Hidden;
            }
        }

        private void UCKMostCol_Click(object sender, RoutedEventArgs e)
        {
            //Checks all Profilemenu items
            setdefcols();

        }

        private void setdefcols()
        {
            foreach (System.Windows.Controls.MenuItem anitem in ColumnsMenuItem.Items)
            {
                if (anitem.IsCheckable) { anitem.IsChecked = false; }
            }
            switch (SelectedComponentComboBox.SelectedItem.ToString())
            {
                default:
                    break;
            }
        }

        private DataTable GetSelectedDatatable(string Datatable2Get)
        {
            DataTable DaTable = Trycorder.GetComponentDataTable(Datatable2Get);
            return DaTable;

        }
        private void SelectedComponentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTable();
        }

        private void UpdateTable()
        {
            string IChooseYou = "";
            try { IChooseYou = SelectedComponentComboBox.SelectedValue.ToString(); }
            catch { IChooseYou = ""; }
            DataTable DaTable = GetSelectedDatatable(IChooseYou);
            TrycorderMainWindow.Title = "AWSTrycorder - " + DaTable.TableName + " - " + Trycorder.LastScan();
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
            if (String.Equals(startout, "Idle"))
            {
                Task.Factory.StartNew(Trycorder.ScanAll);
                ScanButton.Content = "Scanning";
                ScanButton.Background = Brushes.Yellow;
            }
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

        private string GetDefaultAWSCredFile()
        {
            string awscredsfile = "";
            var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            if (homeDrive != null)
            {
                var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
                if (homePath != null)
                {
                    var fullHomePath = homeDrive + homePath;
                    awscredsfile = Path.Combine(fullHomePath, ".aws\\credentials");
                }
                else
                {
                    throw new Exception("Environment variable error, there is no 'HOMEPATH'");
                }
            }
            else
            {
                throw new Exception("Environment variable error, there is no 'HOMEDRIVE'");
            }
            return awscredsfile;
        }
        private void LoadCredentialsMI_Click(object sender, RoutedEventArgs e)
        {
            string awscredsfile = GetDefaultAWSCredFile();
            if (File.Exists(awscredsfile)) MessageBox.Show(Trycorder.LoadAWSCredentials(awscredsfile), "Credential Load Status");
            else MessageBox.Show("Unable to find " + awscredsfile);
            BuildProfileMenuList();
        }



        private void CaseCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DoScan();
        }

        private void SelectColumncomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ExportCredentialsMI_Click(object sender, RoutedEventArgs e)
        {
            string dafile = GetDefaultAWSCredFile();
            MessageBox.Show(StivFunk.ExportCredentials(dafile), "Export Status");
        }

        private void RemoveBadMI_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Trycorder.RemoveBadProfiles(), "Remove Profiles Results");
            BuildProfileMenuList();
        }

        private void Scanbutton_Click(object sender, RoutedEventArgs e)//Scan Button
        {
            if (ScanButton.Background == Brushes.Green)
            {
                ScanButton.Background = Brushes.Yellow;
                Trycorder.ScanAll();
            }
            else
            {
                MessageBox.Show(Trycorder.GetDetailedStatus(), "Scanning in progress...");
            }



        }

        /// <summary>
        /// Count occurrences of strings.
        /// </summary>
        public static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        private void AddCredMI_Click(object sender, RoutedEventArgs e)
        {
            AddCredential acwin = new AddCredential();
            acwin.Show();
        }


        public string saveas()
        {
            //Note,  requires an alias to System.Windows.Forms to avoid hosing the WPF components.
            var _SD = new swformalias.System.Windows.Forms.SaveFileDialog();
            _SD.Filter = "Excel File (*.xls)|*.xls*";
            _SD.FileName = "TrycorderOut" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + DateTime.Now.Minute;
            _SD.Title = "Save As";
            if (_SD.ShowDialog() == swformalias.System.Windows.Forms.DialogResult.OK)
            {
                return (_SD.FileName);
            }
            return "";
        }

        private void ExcelMI_Click(object sender, RoutedEventArgs e)
        {
            string egsportfile = saveas();
            var all = Trycorder.ScanResults().Tables;
            Dictionary<string, DataTable> Outputter = new Dictionary<string, DataTable>();
            foreach (DataTable something in all)
            {
                var namebert = something.TableName;
                Outputter.Add(namebert, something);
            }
            var result = StivFunk.ExportToExcel(Outputter, egsportfile);
            System.Windows.MessageBox.Show(result, "Export to Excel");
            
        }

        private void PaypalMI_Click(object sender, RoutedEventArgs e)
        {
            StivFunk.PayPalDonate("stiv@stiv.com", "Support development of AWS Trycorder", "US", "USD");
        }

        private void LicenseMI_Click(object sender, RoutedEventArgs e)
        {
            string license = @"
// AWS Trycorder:  A tool for gathering data across Amazon accounts
//    Copyright(C) 2016  Stiv Ostenberg

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
 //   but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.";
            System.Windows.MessageBox.Show(license,"AWS Trycorder License" );
        }


    }
}