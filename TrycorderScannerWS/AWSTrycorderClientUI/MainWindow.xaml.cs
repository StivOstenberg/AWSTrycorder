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
        ServiceHost host = null;
        string MyEndpoint = "";
        string MyIP = "";
        string MyPort = "8383";//Need to make this dynamic at some point.
        IChannelFactory<ScannerEngine.ScannerService> MyScanner;
        NetTcpBinding bindbert = new NetTcpBinding();

        public MainWindow()
        {

            InitializeComponent();
            MyEndpoint = "net.tcp://" + "127.0.0.1" + ":8383/AWSTrycorder/";
            this.host = new ServiceHost(typeof(ScannerEngine.ScannerClass));
            bindbert.Security.Mode = SecurityMode.None;
            string myid = WindowsIdentity.GetCurrent().Name.Split('\\')[1].ToLower();
            MyScanner = new ChannelFactory<ScannerEngine.ScannerService >(bindbert);
            var Trycorder = MyScanner.CreateChannel(new EndpointAddress(MyEndpoint));
            StartWCFService();

            var Didit = Trycorder.Initialize();

        }

        public void StartWCFService()
        {


            try
            {

                host = new ServiceHost(typeof(ScannerEngine.ScannerClass), new Uri(MyEndpoint));
                {

                    bindbert.MaxReceivedMessageSize = 400000;
                    bindbert.MaxBufferSize = 400000;
                    host.AddServiceEndpoint(typeof(ScannerEngine.ScannerClass), bindbert, MyEndpoint);


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



    }
}
