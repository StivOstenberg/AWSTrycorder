using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

/// <summary>
/// An instantiable Class with multithreading to make development of other tools easier.  Note:  TO make things work, will need to 
/// set the Messagesize attributes to be large, as we dont want to fail because of insufficient resources.  This is what I used.
///             bindbert.MaxReceivedMessageSize = 2147483647;//Maximum
///            bindbert.MaxBufferSize=2147483647;//Maximum
/// </summary>
namespace ScannerEngine
{
    [ServiceBehavior(UseSynchronizationContext = false)]// This causes each request to process on a different thread,  not use the UI thread.

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class ScannerClass : ScannerInterfaceDefinition 
    {
        DataSet DaWorks = new DataSet();

        DataTable EC2Table = AWSFunctions.AWSTables.GetEC2DetailsTable();
        DataTable S3Table = AWSFunctions.AWSTables.GetS3DetailsTable();
        DataTable IAMTable = AWSFunctions.AWSTables.GetUsersDetailsTable();
        DataTable VPCTable = AWSFunctions.AWSTables.GetVPCDetailsTable();
        DataTable SubnetsTable = AWSFunctions.AWSTables.GetSubnetDetailsTable();
        DataTable RDSTable = AWSFunctions.AWSTables.GetRDSDetailsTable();

        AWSFunctions.ScannerSettings Settings= new AWSFunctions.ScannerSettings();
        AWSFunctions.ScanAWS Scanner = new AWSFunctions.ScanAWS();
        static Action ScanCompletedEvent = delegate { };//I dont know what I am doing here....


        /// <summary>
        /// Sets up the initial list of Profiles and Regions in the Settings File.
        /// </summary>
        /// <returns></returns>
        public string Initialize()
        {
            Settings.Initialize();
            Settings.State = "Idle";
            return "Initialized";
        }

        //Settings Stuff
        //External MySQL (Endpoint, Port, User, Password, Certificate?)

        //This section will hold private functions to update our data objects and maybe to log out to external data collectors
        [OperationBehavior]
        public DataTable GetEC2Table()
        {
            return EC2Table ;
        }

        public DataTable GetRDSTable()
        {
            return RDSTable;
        }

        public string GetStatus()
        {
            return Settings.State;
        }

        public string GetDetailedStatus()
        {
            string ToReturn = "";
            ToReturn += "EC2 :" + Settings.EC2Status["Status"]   +"  " + Settings.EC2Status["EndTime"]+ "  " + Settings.EC2Status["Instances"]+ " instances\n";
            ToReturn += "S3 :" + Settings.S3Status["Status"] + "  " + Settings.S3Status["EndTime"] + "  " + Settings.S3Status["Instances"] + " buckets\n";
            ToReturn += "IAM :" + Settings.IAMStatus["Status"] + "  " + Settings.IAMStatus["EndTime"] + "  " + Settings.IAMStatus["Instances"] + " users\n";
            ToReturn += "Subnets :" + Settings.SubnetsStatus["Status"] + "  " + Settings.SubnetsStatus["EndTime"] + "  " + Settings.SubnetsStatus["Instances"] + " subnets\n";
            ToReturn += "VPC :" + Settings.VPCStatus["Status"] + "  " + Settings.VPCStatus["EndTime"] + "  " + Settings.VPCStatus["Instances"] + " VPCs\n";
            ToReturn += "RDS :" + Settings.RDSStatus["Status"] + "  " + Settings.RDSStatus["EndTime"] + "  " + Settings.RDSStatus["Instances"] + " RDSs\n";

            if(Settings.ScanDone-Settings.ScanStart>TimeSpan.FromSeconds(5))ToReturn += LastScan();
            return ToReturn;
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

 


        public string LastScan()
        {
            List<string> Message = new List<string>();
            TimeSpan duration = Settings.ScanDone -Settings.ScanStart;
            Message.Add( "Last Scan Completed: " + Settings.ScanDone);
            Message.Add("Completion time: " +duration.ToString() );
            return Scanner.List2String(Message);

        }





        private void CheckOverallStatus()
        {
            var E = String.Equals("Idle", Settings.EC2Status["Status"]);
            var S = String.Equals("Idle", Settings.S3Status["Status"]);
            var I = String.Equals("Idle", Settings.IAMStatus["Status"]);
            var V = String.Equals("Idle", Settings.VPCStatus["Status"]);
            var N = String.Equals("Idle", Settings.SubnetsStatus["Status"]);
            var R = String.Equals("Idle", Settings.RDSStatus["Status"]);
            if (E & S & I & N & R)
            {
                Settings.State = "Idle";
                DaWorks.Clear();
                try
                {
                    DaWorks.Tables.Add(VPCTable);
                    DaWorks.Tables.Add(EC2Table);
                    DaWorks.Tables.Add(IAMTable);
                    DaWorks.Tables.Add(S3Table);
                    DaWorks.Tables.Add(SubnetsTable);
                }
                catch
                {
                }
                Settings.ScanDone = DateTime.Now;
            }
        }


        /// <summary>
        /// Gets a list of all profiles (accounts) on the system,  and a boolean indicating whether it is to be processed.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, bool> GetProfiles()
        {
            return Settings.ScannableProfiles;
        }

        
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

        public Dictionary<string, bool> GetComponents()
        {
            return Settings.Components;
        }

        public void SetComponentScanBit(string component, bool state)
        {
            Settings.Components[component] = state;
        }

        public DataSet ScanResults()
        {
            return DaWorks;
        }

        public void ScanAll()
        {

            if (Settings.State.Equals("Scanning...")) return;//Dont run if already running.  What if we croak?
            Settings.ScanStart = DateTime.Now;

            //EC2 Background Worker Setup.
            if (Settings.Components["EC2"])
            {
                Settings.EC2Status["Status"] = "Scanning...";
                Settings.EC2Status["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanEC2(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    EC2Table.Clear();
                    EC2Table.Merge(e.Result as DataTable);
                    Settings.EC2Status["Status"] = "Idle";
                    Settings.EC2Status["EndTime"] = Settings.GetTime();
                    Settings.EC2Status["Instances"] = EC2Table.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();

            }
            else EC2Table.Clear();


            //IAM Background Worker
            if (Settings.Components["IAM"])
            {
                Settings.IAMStatus["Status"] = "Scanning...";
                Settings.IAMStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanIAM(Settings.GetEnabledProfiles.AsEnumerable());
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    IAMTable.Clear();
                    IAMTable.Merge(e.Result as DataTable);
                    Settings.IAMStatus["Status"] = "Idle";
                    Settings.IAMStatus["EndTime"] = Settings.GetTime();
                    Settings.IAMStatus["Instances"] = IAMTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();

            }
            else IAMTable.Clear();

            //S3 Background Worker
            if (Settings.Components["S3"])
            {
                Settings.S3Status["Status"] = "Scanning...";
                Settings.S3Status["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanS3(Settings.GetEnabledProfiles.AsEnumerable());
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    S3Table.Clear();
                    S3Table.Merge(e.Result as DataTable);
                    Settings.S3Status["Status"] = "Idle";
                    Settings.S3Status["EndTime"] = Settings.GetTime();
                    Settings.S3Status["Instances"] = S3Table.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else IAMTable.Clear();

            //Subnets Background Worker
            if (Settings.Components["Subnets"])
            {
                Settings.SubnetsStatus["Status"] = "Scanning...";
                Settings.SubnetsStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanSubnets(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    SubnetsTable.Clear();
                    SubnetsTable.Merge(e.Result as DataTable);
                    Settings.SubnetsStatus["Status"] = "Idle";
                    Settings.SubnetsStatus["EndTime"] = Settings.GetTime();
                    Settings.SubnetsStatus["Instances"] = SubnetsTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else SubnetsTable.Clear();

            //RDS Background Worker
            if (Settings.Components["RDS"])
            {
                Settings.RDSStatus["Status"] = "Scanning...";
                Settings.RDSStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanRDS(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    RDSTable.Clear();
                    RDSTable.Merge(e.Result as DataTable);
                    Settings.RDSStatus["Status"] = "Idle";
                    Settings.RDSStatus["EndTime"] = Settings.GetTime();
                    Settings.RDSStatus["Instances"] = RDSTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else RDSTable.Clear();



            //VPC Background worker
            if (Settings.Components["VPC"])
            {
                Settings.VPCStatus["Status"] = "Scanning...";
                Settings.VPCStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanVPCs(Settings.GetActiveProfiles());
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    VPCTable.Clear();
                    VPCTable.Merge(e.Result as DataTable);
                    Settings.VPCStatus["Status"] = "Idle";
                    Settings.VPCStatus["EndTime"] = Settings.GetTime();
                    Settings.VPCStatus["Instances"] = VPCTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else IAMTable.Clear();
        }

        public DataTable GetIAMTable()
        {
            return IAMTable;
        }

        public DataTable GetS3Table()
        {
            return S3Table;
        }

        public DataTable GetSubnetsTable()
        {
            return SubnetsTable;
        }

        public DataTable GetVPCTable()
        {
            return VPCTable;
        }

        /// <summary>
        /// Filters a datatable returning all results where ANY column matches the search string.
        /// </summary>
        /// <param name="Table2Filter"></param>
        /// <param name="filterstring"></param>
        /// <param name="caseinsensitive"></param>
        /// <returns></returns>
        public DataTable FilterDataTable(DataTable Table2Filter, string filterstring, bool caseinsensitive)
        {
            return Scanner.FilterDataTable(Table2Filter, filterstring, caseinsensitive);
        }

        /// <summary>
        /// Filters a datatable returning all results where a specified column matches the search string.
        /// </summary>
        /// <param name="Table2Filter"></param>
        /// <param name="column2filter"></param>
        /// <param name="filterstring"></param>
        /// <param name="caseinsensitive"></param>
        /// <returns></returns>
        public DataTable FilterDataTablebyCol(DataTable Table2Filter, string column2filter, string filterstring, bool casesensitive)
        {
            string currentname = Table2Filter.TableName;
            string currentsize = Table2Filter.Rows.Count.ToString();

            DataTable ToReturn = Table2Filter.Copy();
            ToReturn.Clear();
            var dareturn = Scanner.FilterDataTable(Table2Filter, column2filter, filterstring, casesensitive);
            ToReturn.Merge(dareturn );
            string newsize = ToReturn.Rows.Count.ToString();
            if (currentsize.Equals(newsize)) ToReturn.TableName = currentname;
            else ToReturn.TableName = currentname + " showing " + newsize + " out of " + currentsize;
            return ToReturn;
        }

        public string LoadAWSCredentials(string credentialfile)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string,string> GetBadProfiles()
        {
            return Settings.BadProfiles;
        }
    }


}
