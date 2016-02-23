using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ScannerEngine
{
    [ServiceBehavior(UseSynchronizationContext = false)]// This causes each request to process on a different thread,  not use the UI thread.

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class ScannerClass : ScannerInterfaceDefinition 
    {

        DataTable EC2Table = AWSFunctions.AWSTables.GetEC2DetailsTable();
        DataTable S3Table = AWSFunctions.AWSTables.GetS3DetailsTable();
        DataTable UserTable = AWSFunctions.AWSTables.GetUsersDetailsTable();
        DataTable VPCTable = AWSFunctions.AWSTables.GetVPCDetailsTable();
        DataTable SubnetTable = AWSFunctions.AWSTables.GetSubnetDetailsTable();
        AWSFunctions.ScannerSettings Settings= new AWSFunctions.ScannerSettings();
        AWSFunctions.ScanAWS Scanner = new AWSFunctions.ScanAWS();

        /// <summary>
        /// Sets up the initial list of Profiles and Regions in the Settings File.
        /// </summary>
        /// <returns></returns>
        public string Initialize()
        {
            Settings.doScanEC2 = true;
            Dictionary<string, bool> Regions2Scan = new Dictionary<string, bool>();
            foreach(var aregion in Scanner.GetRegionNames())
            {
                Regions2Scan.Add(aregion, true);
            }
            Settings.ScannableRegions = Regions2Scan;
            Dictionary<string, bool> Profiles2Scan = new Dictionary<string, bool>();
            foreach(var aprofile in Scanner.GetProfileNames())
            {
                Profiles2Scan.Add(aprofile, true);
            }
            Settings.ScannableProfiles = Profiles2Scan;
            Settings.State = "Idle";
            return "Initialized";
        }

        // This section will hold the private data the functions will interact with
        //Accounts List with Credentials and an Enable/Disable field  Last success +  Fail Count.
        //AWSRegionList(RegionEnglish, Region Name, Enable)
        //AWS Component Table [Enable, Last Complete Scan,  Last Incomplete,  ScanStatus]
        //Scanner Status (Running, Stopping, Ready)


        //Settings Stuff
        //External MySQL (Endpoint, Port, User, Password, Certificate?)

        //This section will hold private functions to update our data objects and maybe to log out to external data collectors

        public DataTable GetEC2Table()
        {
            return EC2Table;
        }

        public string GetStatus()
        {
            return Settings.State;
        }
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        

        public string ScanAll()
        {
            CancellationTokenSource killme = new CancellationTokenSource(); //Used to terminate Scans.
            Settings.State = "Scanning..";
            if (Settings.doScanEC2) ScanEC2(killme);
          Settings.State = "Idle";
           
           return "Ribbitflux";
        }

        private string ScanEC2(CancellationTokenSource killme)
        {
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();

            var myscope = Settings.GetEnabledProfileandRegions.AsEnumerable();

            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = killme.Token;
            po.MaxDegreeOfParallelism = 30;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), Scanner.GetEC2Instances(KVP.Key, KVP.Value));
                  });


            }
            catch
            {
                //Awww..  All dead she
                string death = "";
            }

            EC2Table.Clear();
            foreach(var rabbit in MyData.Values)
            {
                EC2Table.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = start - end;
            string dur = duration.TotalSeconds.ToString();

            return "Done EC2 in " + dur + " seconds.";
        }

        /// <summary>
        /// Gets a list of all profiles (accounts) on the system,  and a boolean indicating whether it is to be processed.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, bool> GetProfiles()
        {
            return Settings.ScannableProfiles;
        }

        /// <summary>
        /// Gets a list of all regions on the system,  and a boolean indicating whether it is to be processed.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, bool> GetRegions()
        {
            return Settings.ScannableRegions;
        }

        public void PayPalDonate(string youremail, string description, string country, string currency)
        {
            string PayPalURL = "";
            PayPalURL += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + youremail +
                "&lc=" + country +
                "&item_name=" + description +
                "&currency_code=" + currency +
                "&bn=" + "PP%2dDonationsBF";
            System.Diagnostics.Process.Start(PayPalURL);
        }

        public void SetRegionStatus(string region, bool state)
        {
            Settings.setRegionEnabled(region, state);
        }

        public void setProfileStatus(string aprofile, bool state)
        {
            Settings.setProfileEnabled(aprofile, state);
        }
    }


}
