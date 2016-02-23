using System;
using System.Collections.Generic;
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
        NetTcpBinding bindbert = new NetTcpBinding(SecurityMode.None,true);
        ServiceHost host = new ServiceHost(typeof(ScannerEngine.ScannerClass));
        IChannelFactory<ScannerEngine.ScannerInterfaceDefinition> MyScanneriChannel ;
        ScannerEngine.ScannerInterfaceDefinition Trycorder;

        public MainWindow()
        {
            InitializeComponent();
            MyScanneriChannel = new ChannelFactory<ScannerEngine.ScannerInterfaceDefinition>(bindbert);
            StartWCFService();
            var ender = new EndpointAddress(MyEndpoint);
            Trycorder = MyScanneriChannel.CreateChannel(ender);
            Trycorder.Initialize();
            BuildProfileMenuList();
                  
        }

        public void StartWCFService()
        {
            try
            {
                //host = new ServiceHost(typeof(ScannerEngine.ScannerClass), new Uri(MyEndpoint));
                {
                    bindbert.MaxReceivedMessageSize = 400000;
                    bindbert.MaxBufferSize = 400000;
                    
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

        #region Event Handlers

        private void ScanMenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void InitializeTestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string initty = Trycorder.Initialize();
            MessageBox.Show(initty, "Initialize results");

        }
        #endregion

        #region UI Setup function
        public void BuildRegionMenu()
        {
            
        }

        public void BuildProfileMenuList()
        {
            System.Windows.Controls.MenuItem Proot = (System.Windows.Controls.MenuItem)this.TopMenu.Items[1];
            Proot.Items.Clear();
            System.Windows.Controls.MenuItem mit = new System.Windows.Controls.MenuItem();
            mit.Header = "Select All";
            mit.StaysOpenOnClick = true;
            Proot.Items.Add(mit);
            System.Windows.Controls.MenuItem mit2 = new System.Windows.Controls.MenuItem();
            mit2.Header = "Select None";
            mit2.StaysOpenOnClick = true;
            Proot.Items.Add(mit2);
            System.Windows.Controls.MenuItem mit3 = new System.Windows.Controls.MenuItem();
            Proot.Items.Add(mit3);

            
            foreach (KeyValuePair<string,bool> KVP  in Trycorder.GetProfiles())
            {

                System.Windows.Controls.MenuItem mi = new System.Windows.Controls.MenuItem();
                mi.IsCheckable = true;
                mi.Header = KVP.Key;

                mi.IsChecked = KVP.Value;
                mi.StaysOpenOnClick = true;
                //mi.Click += ProfileChecked;
                
                Proot.Items.Add(mi);
            }
        }

        #endregion
    }
}
