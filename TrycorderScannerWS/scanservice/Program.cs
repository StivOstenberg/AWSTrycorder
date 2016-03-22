using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;


namespace scanservice
{
    class Program
    {
        static string MyEndpoint = "net.tcp://127.0.0.1:8383/Scanner";
        static NetTcpBinding bindbert = new NetTcpBinding(SecurityMode.None, true);
        static ServiceHost host = new ServiceHost(typeof(ScannerEngine.ScannerClass));
        static IChannelFactory<ScannerEngine.ScannerInterfaceDefinition> MyScanneriChannel;
        static ScannerEngine.ScannerInterfaceDefinition Trycorder;

        static void Main(string[] args)
        {


            var ender = new EndpointAddress(MyEndpoint);
            bindbert.MaxReceivedMessageSize = 2147483647;
            bindbert.MaxBufferSize = 2147483647;
            MyScanneriChannel = new ChannelFactory<ScannerEngine.ScannerInterfaceDefinition>(bindbert);

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

            }



            Trycorder = MyScanneriChannel.CreateChannel(ender);
            Trycorder.Initialize();

            Trycorder.ScanAll();
            string lastscan = "";
            string status = "";
            try
            {
                while (true)
                {
                    var newstatus = Trycorder.GetDetailedStatus();
                    var newlast = Trycorder.LastScan();
                    if (!lastscan.Equals(newlast))
                    {
                        Console.Write(newlast);
                        lastscan = newlast;
                    }
                    if (!status.Equals(newstatus))
                    {
                        Console.Write(newstatus);
                        lastscan = newlast;
                    }
                    System.Threading.Thread.Sleep(5000);

                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }


            }

        

    }
}
