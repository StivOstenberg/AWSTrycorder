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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Timers;

/// <summary>
/// An instantiable Class with multithreading to make development of other tools easier.  Note:  TO make things work, will need to 
/// set the Messagesize attributes to be large, as we dont want to fail because of insufficient resources.  This is what I used.
///             bindbert.MaxReceivedMessageSize = 2147483647;//Maximum
///            bindbert.MaxBufferSize=2147483647;//Maximum
/// </summary>
namespace ScannerEngine
{
    //[ServiceBehavior(UseSynchronizationContext = false)]// This causes each request to process on a different thread,  not use the UI thread.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]// 


    


    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class ScannerClass : ScannerInterfaceDefinition 
    {
        DataSet DaWorks = new DataSet();
         
        static DataTable EC2Table = AWSFunctions.AWSTables.GetComponentTable("EC2");
        static DataTable S3Table = AWSFunctions.AWSTables.GetComponentTable("S3");
        static DataTable IAMTable = AWSFunctions.AWSTables.GetComponentTable("IAM");
        static DataTable VPCTable = AWSFunctions.AWSTables.GetComponentTable("VPC");
        static DataTable SubnetsTable = AWSFunctions.AWSTables.GetComponentTable("Subnets");
        static DataTable RDSTable = AWSFunctions.AWSTables.GetComponentTable("RDS");
        static DataTable EBSTable = AWSFunctions.AWSTables.GetComponentTable("EBS");
        static DataTable SnapshotsTable = AWSFunctions.AWSTables.GetComponentTable("Snapshots");
        static DataTable SNSSubscriptionTable = AWSFunctions.AWSTables.GetComponentTable("SNSSubs");
        static DataTable ELBTable = AWSFunctions.AWSTables.GetComponentTable("ELB");
        static DataTable ENITable = AWSFunctions.AWSTables.GetComponentTable("ENI");

        static AWSFunctions.ScannerSettings Settings= new AWSFunctions.ScannerSettings();
        AWSFunctions.ScanAWS Scanner = new AWSFunctions.ScanAWS();
        static Action ScanCompletedEvent = delegate { };//I dont know what I am doing here....
        private System.Timers.Timer timer;
        
        /// <summary>
        /// Sets up the initial list of Profiles and Regions in the Settings File.
        /// </summary>
        /// <returns></returns>
        public string Initialize()
        {
            Settings.Initialize();
            Settings.State = "Idle";
            
            this.timer = new System.Timers.Timer(1000 * 60 * Settings.ReScanTimerinMinutes);
            this.timer.Elapsed += OnTimerElapsed;
            this.timer.AutoReset = true;
            this.timer.Start();
            
            Scanner.WriteToEventLog("AWS Scanner started " + DateTime.Now.TimeOfDay);
            return "Initialized";
        }

        

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ScanAll();
        }



        //Settings Stuff
        //External MySQL (Endpoint, Port, User, Password, Certificate?)

        //This section will hold private functions to update our data objects and maybe to log out to external data collectors


        public DataTable GetComponentDataTable(string component)
        {
            switch (component.ToLower())
            {
                case "ec2":
                    int rowcount = EBSTable.Rows.Count;
                    return EC2Table;
                case "snapshots":
                    return SnapshotsTable;
                case "rds":
                    return RDSTable;
                case "ebs":
                    return EBSTable;
                case "iam":
                    return IAMTable;
                case "s3":
                    return S3Table;
                case "snssubs":
                    return SNSSubscriptionTable;
                case "subnets":
                    return SubnetsTable;
                case "vpc":
                    return VPCTable;
                case "elb":
                    return ELBTable;
                case "eni":
                    return ENITable;
                default:
                    return EC2Table;

            }
        }





        public string GetStatus()
        {
            return Settings.State;
        }

        public string GetDetailedStatus()
        {
            string ToReturn = "";
            ToReturn += "EBS :" + Settings.EBSStatus["Status"] + "  " + Settings.EBSStatus["EndTime"] + "  " + Settings.EBSStatus["Volumes"] + " volumes\n";
            ToReturn += "EC2 :" + Settings.EC2Status["Status"]   +"  " + Settings.EC2Status["EndTime"]+ "  " + Settings.EC2Status["Instances"]+ " instances\n";
            ToReturn += "S3 :" + Settings.S3Status["Status"] + "  " + Settings.S3Status["EndTime"] + "  " + Settings.S3Status["Instances"] + " buckets\n";
            ToReturn += "IAM :" + Settings.IAMStatus["Status"] + "  " + Settings.IAMStatus["EndTime"] + "  " + Settings.IAMStatus["Instances"] + " users\n";
            ToReturn += "Subnets :" + Settings.SubnetsStatus["Status"] + "  " + Settings.SubnetsStatus["EndTime"] + "  " + Settings.SubnetsStatus["Instances"] + " subnets\n";
            ToReturn += "VPC :" + Settings.VPCStatus["Status"] + "  " + Settings.VPCStatus["EndTime"] + "  " + Settings.VPCStatus["Instances"] + " VPCs\n";
            ToReturn += "RDS :" + Settings.RDSStatus["Status"] + "  " + Settings.RDSStatus["EndTime"] + "  " + Settings.RDSStatus["Instances"] + " RDSs\n";
            ToReturn += "Snapshots :" + Settings.SnapshotsStatus["Status"] + "  " + Settings.SnapshotsStatus["EndTime"] + "  " + Settings.SnapshotsStatus["Instances"] + " Snapshots\n";
            ToReturn += "SNS Subscriptions :" + Settings.SNSSubs["Status"] + "  " + Settings.SNSSubs["EndTime"] + "  " + Settings.SNSSubs["Instances"] + " Subscriptions\n";
            ToReturn += "Load Balancers:" + Settings.ELBStatus["Status"] + "  " + Settings.ELBStatus["EndTime"] + "  " + Settings.ELBStatus["ELBs"] + " Load Balancers\n";

            if (Settings.ScanDone-Settings.ScanStart>TimeSpan.FromSeconds(5))ToReturn += LastScan();
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
            Message.Add(Settings.ScanDone.ToShortDateString() + " " + Settings.ScanDone.ToShortTimeString() + " in " + duration.Minutes + "m " + duration.Seconds + "s");
            return Scanner.List2String(Message);

        }




        /// <summary>
        /// CheckoverOverallStatus is run to see if any processes have not completed.
        /// </summary>
        private void CheckOverallStatus()
        {
            var E = String.Equals("Idle", Settings.EC2Status["Status"]);
            var S = String.Equals("Idle", Settings.S3Status["Status"]);
            var I = String.Equals("Idle", Settings.IAMStatus["Status"]);
            var V = String.Equals("Idle", Settings.VPCStatus["Status"]);
            var N = String.Equals("Idle", Settings.SubnetsStatus["Status"]);
            var R = String.Equals("Idle", Settings.RDSStatus["Status"]);
            var A = String.Equals("Idle", Settings.EBSStatus["Status"]);
            var T = String.Equals("Idle", Settings.SnapshotsStatus["Status"]);
            var B = String.Equals("Idle", Settings.ELBStatus["Status"]);
            var eni = String.Equals("Idle", Settings.ENIStatus["Status"]);
            if (E & S & I & N & R & A & T & B & eni)
            {
                Settings.State = "Idle";
                Scanner.WriteToEventLog("AWS Scanner completed " + DateTime.Now.TimeOfDay);
                try {
                    DaWorks.Tables.Clear();
                    DaWorks.Clear();
                }
                catch(Exception ex)
                {
                    var mess = ex.Message;
                }
                try
                {
                    DaWorks.Tables.Add(VPCTable.Copy());
                    DaWorks.Tables.Add(EC2Table.Copy());
                    DaWorks.Tables.Add(IAMTable.Copy());
                    DaWorks.Tables.Add(S3Table.Copy());
                    DaWorks.Tables.Add(SubnetsTable.Copy());
                    DaWorks.Tables.Add(RDSTable.Copy());
                    DaWorks.Tables.Add(EBSTable.Copy());
                    DaWorks.Tables.Add(SnapshotsTable.Copy());
                    DaWorks.Tables.Add(SNSSubscriptionTable.Copy());
                    DaWorks.Tables.Add(ELBTable.Copy());
                    DaWorks.Tables.Add(ENITable.Copy());
                }
                catch(Exception ex)
                {
                    var whyforfail = ex.Message;
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

        public string ScanAll()
        {

            if (Settings.State.Equals("Scanning...")) return("Already Running!");//Dont run if already running.  What if we croak?
            Settings.ScanStart = DateTime.Now;


            //ENI Background Worker Setup.
            if (Settings.Components["ENI"])
            {
                Settings.ENIStatus["Status"] = "Scanning...";
                Settings.ENIStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanENIs(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    ENITable.Clear();
                    ENITable.Merge(e.Result as DataTable);
                    Settings.ENIStatus["Status"] = "Idle";
                    Settings.ENIStatus["EndTime"] = Settings.GetTime();
                    Settings.ENIStatus["NetworkInterfaces"] = EBSTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else ENITable.Clear();



            //EBS Background Worker Setup.
            if (Settings.Components["EBS"])
            {
                Settings.EBSStatus["Status"] = "Scanning...";
                Settings.EBSStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanEBS(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    EBSTable.Clear();
                    EBSTable.Merge(e.Result as DataTable);
                    Settings.EBSStatus["Status"] = "Idle";
                    Settings.EBSStatus["EndTime"] = Settings.GetTime();
                    Settings.EBSStatus["Volumes"] = EBSTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else EBSTable.Clear();

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
                    var b4 = IAMTable.Rows.Count;
                    IAMTable.Clear();
                    var newb = (e.Result as DataTable).Rows.Count;
                    IAMTable.Merge(e.Result as DataTable);
                    var after = IAMTable.Rows.Count;
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
                    e.Result = Scanner.ScanS3(Settings.GetEnabledProfileandRegions);
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
            else S3Table.Clear();

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

            //ELB Background Worker
            if (Settings.Components["ELB"])
            {
                Settings.ELBStatus["Status"] = "Scanning...";
                Settings.ELBStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanELB(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    ELBTable.Clear();
                    ELBTable.Merge(e.Result as DataTable);
                    Settings.ELBStatus["Status"] = "Idle";
                    Settings.ELBStatus["EndTime"] = Settings.GetTime();
                    Settings.ELBStatus["Instances"] = ELBTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else ELBTable.Clear();


            //VPC Background worker
            if (Settings.Components["VPC"])
            {
                Settings.VPCStatus["Status"] = "Scanning...";
                Settings.VPCStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanVPCs(Settings.GetEnabledProfileandRegions);
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
            else VPCTable.Clear();

            //Snapshots Background Worker
            if (Settings.Components["Snapshots"])
            {
                Settings.SnapshotsStatus["Status"] = "Scanning...";
                Settings.SnapshotsStatus["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanSnapshots(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    SnapshotsTable.Clear();
                    SnapshotsTable.Merge(e.Result as DataTable);
                    Settings.SnapshotsStatus["Status"] = "Idle";
                    Settings.SnapshotsStatus["EndTime"] = Settings.GetTime();
                    Settings.SnapshotsStatus["Instances"] = SnapshotsTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else SnapshotsTable.Clear();

            //SNS Background worker
            if (Settings.Components["SNSSubs"])
            {
                Settings.SNSSubs["Status"] = "Scanning...";
                Settings.SNSSubs["StartTime"] = Settings.GetTime();
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (s, e) =>
                {
                    e.Result = Scanner.ScanSNSSubs(Settings.GetEnabledProfileandRegions);
                };
                //The task what executes when the backgroundworker completes.
                worker.RunWorkerCompleted += (s, e) =>
                {
                    SNSSubscriptionTable.Clear();
                    SNSSubscriptionTable.Merge(e.Result as DataTable);
                    Settings.SNSSubs["Status"] = "Idle";
                    Settings.SNSSubs["EndTime"] = Settings.GetTime();
                    Settings.SNSSubs["Instances"] = SNSSubscriptionTable.Rows.Count.ToString();
                    CheckOverallStatus();
                };
                worker.RunWorkerAsync();
            }
            else SNSSubscriptionTable.Clear();

            Scanner.WriteToEventLog("AWS Scan started " + DateTime.Now.TimeOfDay);
            return ("AWS Scan started " + DateTime.Now.TimeOfDay);
        }



        /// <summary>
        /// Filters a datatable returning all results where ANY column matches the search string.
        /// </summary>
        /// <param name="Table2Filter"></param>
        /// <param name="filterstring"></param>
        /// <param name="casesensitive">set to false to ignore case</param>
        /// <returns></returns>
        public DataTable FilterDataTable(DataTable Table2Filter, string filterstring, bool casesensitive, bool contains)
        {
            return Scanner.FilterDataTable(Table2Filter, filterstring, casesensitive);
        }

        /// <summary>
        /// Given a datatable NAME returns a filtered datatable based on current datatable in the scanner.
        /// </summary>
        /// <param name="ComponentName"></param>
        /// <param name="filterstring"></param>
        /// <param name="casesensitive"></param>
        /// <param name="contains"></param>
        /// <returns></returns>
        public DataTable FilterScannerDataTable(string ComponentName, string filterstring, bool casesensitive, bool contains)
        {
            DataTable ToReturn = FilterDataTable(GetComponentDataTable(ComponentName), filterstring, casesensitive, contains);
            return ToReturn;
            
        }

        /// <summary>
        /// Filters a datatable returning all results where a specified column matches the search string.
        /// </summary>
        /// <param name="Table2Filter"></param>
        /// <param name="column2filter"></param>
        /// <param name="filterstring"></param>
        /// <param name="caseinsensitive"></param>
        ///  <param name="contains"></param>
        /// <returns></returns>
        public DataTable FilterDataTablebyCol(DataTable Table2Filter, string column2filter, string filterstring, bool casesensitive ,bool contains)
        {
            string currentname = Table2Filter.TableName;
            string currentsize = Table2Filter.Rows.Count.ToString();

            DataTable ToReturn = Table2Filter.Copy();
            ToReturn.Clear();
            var dareturn = Scanner.FilterDataTable(Table2Filter, column2filter, filterstring, casesensitive,contains);
            ToReturn.Merge(dareturn );
            string newsize = ToReturn.Rows.Count.ToString();
            if (currentsize.Equals(newsize)) ToReturn.TableName = currentname;
            else ToReturn.TableName = currentname + " showing " + newsize + " out of " + currentsize;
            return ToReturn;
        }

        public DataTable FilterScannerDataTablebyCol(String Table2Filter, string column2filter, string filterstring, bool casesensitive,bool contains)
        {
            DataTable ToReturn = FilterDataTablebyCol(GetComponentDataTable(Table2Filter), column2filter,filterstring, casesensitive, contains);
            return ToReturn;

        }

        public string LoadAWSCredentials(string credentialfile)
        {
           return Scanner.LoadCredentials(credentialfile);
        }

        public Dictionary<string,string> GetBadProfiles()
        {
            return Settings.BadProfiles;
        }

        public string RemoveBadProfiles()
        {
            string ToReturn = "";
            ToReturn += Settings.RemoveBadProfilesfromStore();
            return ToReturn;
        }

        public Dictionary<string, bool> GetColumnVisSetting(string component)
        {
            return Settings.GetColumnSettings(component);
        }

        public void SetColumnVisSetting(string component, string column, bool visibility)
        {
            Settings.setcolumnvisibility(component, column, visibility);
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
    }


}
