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

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class ScannerClass : ScannerService
    {

        DataTable EC2Table = AWSFunctions.AWSTables.GetEC2DetailsTable();
        DataTable S3Table = AWSFunctions.AWSTables.GetS3DetailsTable();
        DataTable UserTable = AWSFunctions.AWSTables.GetUsersDetailsTable();
        DataTable VPCTable = AWSFunctions.AWSTables.GetVPCDetailsTable();
        DataTable SubnetTable = AWSFunctions.AWSTables.GetSubnetDetailsTable();
        AWSFunctions.ScannerSettings Settings;
        AWSFunctions.ScanAWS Scanner;

        public string Initialize()
        {
            
            
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

    }


}
