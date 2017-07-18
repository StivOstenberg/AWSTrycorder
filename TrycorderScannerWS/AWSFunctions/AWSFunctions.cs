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

using Amazon;

using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.CloudWatch;


using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;

using System.Text;
using MySql.Data.MySqlClient;

namespace AWSFunctions
{

    /// <summary>
    /// Given credentials and arguments, retrieve data from AWS.
    /// </summary>
    public class ScanAWS
    {
        /// <summary>
        /// Opens a file selection dialog box
        /// </summary>
        /// <returns></returns>
        public string Filepicker()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "All Files|*.*|Script (*.py, *.sh)|*.py*;*.sh";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return (ofd.FileName);
            }
            return ("");
        }

        /// <summary>
        /// Opens a filtered file selection dialog box
        /// "All Files|*.*" for example
        /// </summary>
        /// <param name="Filter"></param>
        /// <returns></returns>
        public string Filepicker(string Filter)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = Filter;
            ofd.InitialDirectory = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return (ofd.FileName);
            }
            return ("");
        }

        /// <summary>
        /// Given a path to an AWS credentials file, load the contents into the Windows AWS credential store.
        /// </summary>
        /// <param name="credfile"></param>
        /// <returns></returns>
        public string LoadCredentials(string credfile)
        {
            //Loading a credential file.
            string results = "";
            //Select file

            //Import creds
            var txt = File.ReadAllText(credfile);
            Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

            Dictionary<string, string> currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            ini[""] = currentSection;

            foreach (var line in txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => !string.IsNullOrWhiteSpace(t))
                       .Select(t => t.Trim()))
            {
                if (line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    ini[line.Substring(1, line.LastIndexOf("]") - 1)] = currentSection;
                    continue;
                }

                var idx = line.IndexOf("=");
                if (idx == -1)
                    currentSection[line] = "";
                else
                    currentSection[line.Substring(0, idx)] = line.Substring(idx + 1);
            }

            //Build a list of current keys to use to avoid dupes due to changed "profile" names.
            Dictionary<string, string> currentaccesskeys = new Dictionary<string, string>();
            var Profilenameses = Amazon.Util.ProfileManager.ListProfileNames();
            foreach (var aprofilename in Profilenameses)
            {
                var acred = Amazon.Util.ProfileManager.GetAWSCredentials(aprofilename).GetCredentials();
                currentaccesskeys.Add(aprofilename, acred.AccessKey);
            }


            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in ini)
            {
                string newprofileName = "";
                string newaccessKey = "";
                string newsecretKey = "";
                if (kvp.Key == "") continue;

                newprofileName = kvp.Key.ToString();
                newaccessKey = kvp.Value["aws_access_key_id"].ToString();
                newsecretKey = kvp.Value["aws_secret_access_key"].ToString();

                //See if profile already exists.
                if (Amazon.Util.ProfileManager.ListProfileNames().Contains(newprofileName))
                {
                    var daP = Amazon.Util.ProfileManager.GetAWSCredentials(newprofileName).GetCredentials();
                    //See if the profiles with the same name have the same AK and SK.
                    if (daP.AccessKey == newaccessKey & daP.SecretKey == newsecretKey)
                    {
                        results += newprofileName + " already in store.\n";
                        continue;
                    }
                    else
                    {// The names are the same, but the data are different.
                        results += newprofileName + " keys do not match existing profile!\n";
                        continue;
                    }

                }

                else //Profile does not exist by this name.  
                {
                    if (currentaccesskeys.Values.Contains(newaccessKey))//Do we already have that key?
                    {
                        //We are trying to enter a duplicate profile name for the same key. 
                        string existingprofile = "";
                        foreach (KeyValuePair<string, string> minikvp in currentaccesskeys)
                        {
                            if (minikvp.Value == newaccessKey)
                            {
                                existingprofile = minikvp.Key.ToString();
                            }
                        }

                        results += newprofileName + " already exists as " + existingprofile + "\n";
                        continue;
                    }
                    else
                    {
                        if (newaccessKey.Length.Equals(20) & newsecretKey.Length.Equals(40))
                        {
                            Amazon.Util.ProfileManager.RegisterProfile(newprofileName, newaccessKey, newsecretKey);
                            results += newprofileName + " added to credential store!\n";

                        }
                        else
                        {
                            results += newprofileName + "'s keys are not the correct length!\n";
                            continue;
                        }
                    }
                }

            }
            if (results.Equals(""))
            {
                string message = ini.Count.ToString() + " profiles in " + credfile + " already in credential store.";
                return results;
            }
            else
            {
                return results;
            }

        }


        public string GetFileSize(double byteCount)
        {
            //Note these are based on human numbers, not multiples of 1024 which have different names.
            double kB = 1000;
            double MB = Math.Pow(kB, 2);
            double GB = Math.Pow(kB, 3);
            double TB = Math.Pow(kB, 4);
            double PB = Math.Pow(kB, 5);

            string size = "0 Bytes"; 
            if (byteCount >= PB)
                size = String.Format("{0:##.##}", byteCount / PB) + " PB";
            else if (byteCount >= TB)
                size = String.Format("{0:##.##}", byteCount / TB) + " TB";
            else if (byteCount >= GB)
                size = String.Format("{0:##.##}", byteCount / GB) + " GB";
            else if (byteCount >= MB)
                size = String.Format("{0:##.##}", byteCount / MB) + " MB";
            else if (byteCount >= kB)
                size = String.Format("{0:##.##}", byteCount / kB) + " kB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }
        public string ExportCredentials(string filename)
        {
            string ToReturn = "";
            string newfilename = filename + DateTime.Now.DayOfWeek + "-" + DateTime.Now.Hour + DateTime.Now.Minute;
            string newconfigfile = "# Trycorder Generated Credential Export from .NET credential store #\n";
            foreach (var aprofilename in Amazon.Util.ProfileManager.ListProfileNames().OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase))
            {
                var acred = Amazon.Util.ProfileManager.GetAWSCredentials(aprofilename).GetCredentials();
                string aid = "#";
                try
                {
                    string id = GetAccountID(aprofilename);
                    aid = "######## Account Number: " + id + "##########################################\n";
                }
                catch
                {
                    aid = "######## Account Number: unspecified ##########################################\n";
                }

                // Try to figure out who the credentials belong to.  (Too slow (loading IAM table)
                //var usertable = GetIAMUsers(aprofilename);
                //string query = "Access-Keys contains " + acred.AccessKey;
                //DataRow foundrow = usertable.Select(query)[0];   (query dont not work)
                //string username = foundrow["Username"].ToString();


                //////
                newconfigfile += aid;
                newconfigfile += "[" + aprofilename + "]\n";
                newconfigfile += "aws_access_key_id=" + acred.AccessKey + "\n";
                newconfigfile += "aws_secret_access_key=" + acred.SecretKey + "\n\n";
            }

            if (File.Exists(filename))
            {

                try
                {
                    File.Move(filename, newfilename);
                    ToReturn += "Moved " + filename + " to " + newfilename + "\n";
                }
                catch (Exception ex)
                {
                    return "Unable to move original file " + filename + "\n" + ex.Message;
                }
            }
            try
            {
                File.WriteAllText(filename, newconfigfile);
                ToReturn += "Exported credentials to " + filename;
            }
            catch { return "Failed write to  " + filename; }


            return ToReturn;
        }

        public string DeleteCredential(string Profilename)
        {
            var existingprofiles = GetProfileNames();
            if (existingprofiles.Contains(Profilename))
            {
                try
                {
                    Amazon.Util.ProfileManager.UnregisterProfile(Profilename);
                    return Profilename + " deleted.";
                }
                catch { return "Unable to whack " + Profilename; }
            }
            else { return Profilename + " not found!"; }
        }

        public string AddCredential(string newprofileName, string newaccessKey, string newsecretKey)
        {
            string ToReturn = "";

            //Build a list of current keys to use to avoid dupes due to changed "profile" names.
            Dictionary<string, string> currentaccesskeys = new Dictionary<string, string>();
            foreach (var aprofilename in Amazon.Util.ProfileManager.ListProfileNames())
            {
                var acred = Amazon.Util.ProfileManager.GetAWSCredentials(aprofilename).GetCredentials();
                if (acred.AccessKey.Equals(newaccessKey)) return "Access Key already in store! Profile: " + aprofilename;
                currentaccesskeys.Add(aprofilename, acred.AccessKey);
            }
            try
            {
                if (newaccessKey.Length.Equals(20) & newsecretKey.Length.Equals(40))
                {
                    ToReturn = newprofileName + " added to credential store!\n";
                    Amazon.Util.ProfileManager.RegisterProfile(newprofileName, newaccessKey, newsecretKey);
                }
                else
                {
                    ToReturn = newprofileName + "'s keys are not the correct length!\n";
                }
            }
            catch (Exception ex)
            {
                ToReturn = ex.Message;
            }
            try
            {
            }
            catch
            {
            }

            return ToReturn;
        }

        /// <summary>
        /// Returns the names of the profiles in the Windows AWS Credential Store.
        /// </summary>
        /// <returns></returns>
        public IOrderedEnumerable<string> GetProfileNames()
        {
            var Profiles = Amazon.Util.ProfileManager.ListProfileNames().OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase);
            return Profiles;
        }

        public IEnumerable<RegionEndpoint> GetRegions()
        {
            var Regions = RegionEndpoint.EnumerableAllRegions;
            return Regions;
        }

        public List<String> GetRegionNames()
        {
            List<String> RegionNames = new List<string>();
            var Regions = RegionEndpoint.EnumerableAllRegions;
            foreach (var EP in Regions)
            {
                if (EP.DisplayName.Contains("China") || EP.DisplayName.Contains("GovCloud")) continue;
                RegionNames.Add(EP.DisplayName);
            }
            return RegionNames;
        }
        /// <summary>
        /// Given a Profile and Region, return a table with data about buckets and bucket sizes,
        /// collected from CloudWatch.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <param name="Region2Scan"></param>
        /// <returns></returns>
        public DataTable S3SizeCloudWatch(string aprofile, string Region2Scan)
        {
            DataTable ToReturn =AWSTables.GetS3SizesTable();
            
            string accountid = GetAccountID(aprofile);
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            DataTable metlist = new DataTable();
            try
            {
                var credential = new StoredProfileAWSCredentials(aprofile);
                AmazonCloudWatchClient CWClient = new AmazonCloudWatchClient(credential, Endpoint2scan);
                Amazon.CloudWatch.Model.ListMetricsRequest LMReq = new Amazon.CloudWatch.Model.ListMetricsRequest();               

                //Using to explore the metrics
                LMReq.Namespace = "AWS/S3";
                LMReq.MetricName = "BucketSizeBytes";
                var getmetrics = CWClient.ListMetrics(LMReq).Metrics; // And we get the metrics..

                //This is just stuff I used to view data from the List
                metlist.Columns.Add("MetricName");
                metlist.Columns.Add("NameSpace");

                //These are the dimensions for S3 bucket what we want.
                metlist.Columns.Add("Bucketname");
                metlist.Columns.Add("StorageType");

                foreach (var ametric in getmetrics) // Here, we build a list containing names of buckets and their storage types stored in metlist.
                {
                    var DR = metlist.NewRow();
                    try
                    {
                        DR["MetricName"] = ametric.MetricName;
                        DR["NameSpace"] = ametric.Namespace;
                        var dim = ametric.Dimensions;
                        //These are the dimensions for S3.  
                        DR["StorageType"] =  dim[0].Value;  //We need to capture the storage type for each bucket so we make the appropriate request for the data.
                        DR["BucketName"] = dim[1].Value;
                        metlist.Rows.Add(DR);
                    }
                    catch(Exception ex)
                    {
                        int woeisme = 1;
                    }
                }

                // Okay, collect the daters for these here buckets, now that we know their name and storage types.
                foreach (var abucket in metlist.AsEnumerable())
                {
                    Amazon.CloudWatch.Model.GetMetricStatisticsRequest GMReq = new Amazon.CloudWatch.Model.GetMetricStatisticsRequest();
                    string bucketname = abucket["Bucketname"].ToString();
                    string storagetype = abucket["StorageType"].ToString();
                    
                    try
                    {
                        Amazon.CloudWatch.Model.Dimension dimbo = new Amazon.CloudWatch.Model.Dimension();
                        dimbo.Name = "BucketName";
                        dimbo.Value = bucketname;
                        GMReq.Dimensions.Add(dimbo);
                        Amazon.CloudWatch.Model.Dimension dimbo2 = new Amazon.CloudWatch.Model.Dimension();
                        dimbo2.Name = "StorageType";
                        dimbo2.Value = storagetype;
                        GMReq.Dimensions.Add(dimbo2);

                        //Build the request:
                        GMReq.Namespace = "AWS/S3";
                        GMReq.EndTime = DateTime.Now;
                        GMReq.StartTime = DateTime.Now - TimeSpan.FromDays(21);
                        GMReq.Period = (60 * 60 * 24 * 7);//Seconds in a week.
                        GMReq.Statistics.Add("Minimum");
                        GMReq.Statistics.Add("Maximum");
                        GMReq.Statistics.Add("Average");
                        GMReq.MetricName = "BucketSizeBytes";

                        //Execute request:
                        var metricresponse = CWClient.GetMetricStatistics(GMReq);
                        
                        //Process Return
                        var dp = metricresponse.Datapoints;
                         if(dp.Count==0)
                        {
                            //none
                        }


                        var arow = ToReturn.NewRow();

                        Dictionary<DateTime, Amazon.CloudWatch.Model.Datapoint> sortem = new Dictionary<DateTime, Amazon.CloudWatch.Model.Datapoint>();
                        foreach(var ap in dp)
                        {
                            sortem.Add(ap.Timestamp, ap);
                        }
                        var sorteddates = sortem.Keys.ToList();
                        sorteddates.Sort();
                        var firstpass = true;
                        foreach(var key in sorteddates)
                        {
                            var ap = sortem[key];
                            var min = ap.Minimum;
                            var max = ap.Maximum;
                            var av = ap.Average;
                            var ts = ap.Timestamp;
                            
                            if (firstpass)
                            {
                                firstpass = false;
                                arow["AccountID"] = accountid;
                                arow["Profile"] = aprofile;
                                arow["Bucket"] = bucketname;
                                arow["Region"] = Region2Scan;
                                arow["StartDate"] = ts.ToShortDateString();
                                arow["EndDate"] = ts.ToShortDateString();
                                arow["StartSizeMin"] =GetFileSize( min);
                                arow["StartSizeMax"] =GetFileSize( max);
                                arow["StartSizeAVG"]= GetFileSize(av);
                            }
                            else
                            {
                                arow["EndDate"] = ts.ToShortDateString();
                                arow["EndSizeMin"] = GetFileSize(min);
                                arow["EndSizeAVG"] = GetFileSize(av);
                                arow["EndSizeMax"] = GetFileSize(max);
                                arow["EndSizeMaxBytes"] = Math.Round(av);

                            }
                        }
                        ToReturn.Rows.Add(arow.ItemArray);
                    }
                    catch(Exception ex)
                    {

                    }
                }


            }
            catch(Exception ex)
            {

            }
            int buckets = metlist.Rows.Count;

            int returning = ToReturn.Rows.Count;
            return ToReturn;
        }

        public DataTable GetS3Buckets(string aprofile,string Region2Scan)
        {
            string accountid = GetAccountID(aprofile);
            Amazon.Runtime.AWSCredentials credential;
            DataTable ToReturn = AWSTables.GetComponentTable("S3");
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }


            //Query Cloudwatch to get list of buckets and sizes in this here region
            var Sizetable = S3SizeCloudWatch(aprofile, Region2Scan);//This is calling another of my functions to get data.
            int sizerows = Sizetable.Rows.Count;
            if(sizerows>0)
            {
                int lookatme = 1;
                var rabbit = Sizetable.Rows[0];

            }


            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);


                
                
                AmazonS3Client S3Client = new AmazonS3Client(credential, Endpoint2scan);
                ListBucketsResponse response = S3Client.ListBuckets();

                //Why are we getting USEast buckets?  Because the Cloudwatch returns buckets by region, but list buckets does care about regions!


                foreach (S3Bucket abucket in response.Buckets)
                {
                    DataRow abucketrow = ToReturn.NewRow(); //Create a new empty row to hold data
                    var name = abucket.BucketName;
                    DataRow bucketsizedata = AWSTables.GetS3SizesTable().NewRow();   //Create a new empty row to hold data
                    Boolean havesize = true;

                    try//Lookup the record in the Sizetable for this bucket.
                    {
                        //This is equivalent to the LINQ query.
                        Boolean foundinsizetable = false;
                        List<string> bn = new List<string>();
                        foreach(var rabbit in Sizetable.AsEnumerable())
                        {
                            String thisun = rabbit["Bucket"].ToString();
                            bn.Add(thisun);
                            if (thisun.Equals(name))
                            {
                                bucketsizedata = rabbit;
                                foundinsizetable = true;
                            }
                        }
                        if(!foundinsizetable)
                        {
                            bn.Sort();
                            if (bn.Contains(name))
                            {
                                string rabbit = "Yes it does!";
                            }
                            //why not?
                        }



                        if (bucketsizedata == null)
                        {
                            abucketrow["EndDate"] = AWSTables.Shrug;
                            havesize = false;
                        }

                    }
                    catch
                    {

                    }


                    try
                    {
                        GetBucketLocationRequest gbr = new GetBucketLocationRequest();
                        gbr.BucketName = name;
                        GetBucketLocationResponse location = S3Client.GetBucketLocation(gbr);

                        var region = location.Location.Value;

                        if (region.Equals(""))
                        {
                            
                            region = "us-east-1";//Because a region is required even if not relevant.
                        }

                        if (!region.Contains(Endpoint2scan.SystemName))
                        {
                            name = name;//Bucketname
                            region = region;//BucketRegion
                            string target = Endpoint2scan.SystemName;
                            continue;
                        }
                        var pointy = RegionEndpoint.GetBySystemName(region);

                        //Build a config that references the buckets region.
                        AmazonS3Config S3C = new AmazonS3Config();
                        S3C.RegionEndpoint = pointy;
                        AmazonS3Client BS3Client = new AmazonS3Client(credential, S3C);

                        var authregion = "";
                        var EP = BS3Client.Config.RegionEndpoint.DisplayName;
                        if (String.IsNullOrEmpty(BS3Client.Config.RegionEndpoint.DisplayName)) authregion = "";
                        else
                        {
                            authregion = BS3Client.Config.AuthenticationRegion;
                        }

                        string authservice = "";

                        if (string.IsNullOrEmpty(BS3Client.Config.AuthenticationServiceName)) authservice = "";
                        else
                        {
                            authservice = BS3Client.Config.AuthenticationServiceName;
                        }

                        var createddate = abucket.CreationDate;
                        string owner = "";
                        string grants = "";
                        string tags = "";
                        string lastaccess = "";
                        string defaultpage = "";
                        string website = "";
                        //Now start pulling der einen data.

                        //Here we are pulling the lists of people who have access to this bucket...
                        GetACLRequest GACR = new GetACLRequest();
                        GACR.BucketName = name;
                        var ACL = BS3Client.GetACL(GACR);
                        var grantlist = ACL.AccessControlList;
                        owner = grantlist.Owner.DisplayName;
                        foreach (var agrant in grantlist.Grants)
                        {
                            if (grants.Length > 1) grants += "\n";
                            var gName = agrant.Grantee.DisplayName;
                            var gType = agrant.Grantee.Type.Value;
                            var aMail = agrant.Grantee.EmailAddress;

                            if (gType.Equals("Group"))
                            {
                                grants += gType + " - " + agrant.Grantee.URI + " - " + agrant.Permission + " - " + aMail;
                            }
                            else
                            {
                                grants += gName + " - " + agrant.Permission + " - " + aMail;
                            }
                        }

                        //Here we are pulling the information about the bucket website address if it exists.
                        GetBucketWebsiteRequest GBWReq = new GetBucketWebsiteRequest();
                        GBWReq.BucketName = name;
                        GetBucketWebsiteResponse GBWRes = BS3Client.GetBucketWebsite(GBWReq);
                        defaultpage = GBWRes.WebsiteConfiguration.IndexDocumentSuffix;

                        


                        if (defaultpage != null)
                        {
                            website = @"http://" + name + @".s3-website-" + region + @".amazonaws.com/" + defaultpage;
                        }
                        abucketrow["AccountID"] = accountid;
                        abucketrow["Profile"] = aprofile;
                        abucketrow["Bucket"] = name;
                        abucketrow["Region"] = region;
                        abucketrow["RegionEndpoint"] = EP;
                        abucketrow["AuthRegion"] = authregion;
                        abucketrow["AuthService"] = authservice;

                        abucketrow["CreationDate"] = createddate.ToString();
                        
                        abucketrow["LastAccess"] = lastaccess;
                        abucketrow["Owner"] = owner;
                        abucketrow["Grants"] = grants;
                        abucketrow["StartDate"] = "NA";
                        abucketrow["StartSizeMin"] = "NA";
                        abucketrow["StartSizeAVG"] = "NA";
                        abucketrow["StartSizeMax"] = "NA";

                        abucketrow["EndDate"] = "NA";
                        abucketrow["EndSizeMin"] = "NA";
                        abucketrow["EndSizeAVG"] = "NA";
                        abucketrow["EndSizeMax"] = "NA";
                        abucketrow["EndSizeMaxBytes"] = 0;

                        if (havesize)
                        {
                            abucketrow["StartDate"] = bucketsizedata["StartDate"];
                            abucketrow["StartSizeMin"] = bucketsizedata["StartSizeMin"];
                            abucketrow["StartSizeAVG"] = bucketsizedata["StartSizeAVG"];
                            abucketrow["StartSizeMax"] = bucketsizedata["StartSizeMax"];

                            abucketrow["EndDate"] = bucketsizedata["EndDate"];
                            abucketrow["EndSizeMin"] = bucketsizedata["EndSizeMin"];
                            abucketrow["EndSizeAVG"] = bucketsizedata["EndSizeAVG"];
                            abucketrow["EndSizeMax"] = bucketsizedata["EndSizeMax"];
                            abucketrow["EndSizeMaxBytes"] = bucketsizedata["EndSizeMaxBytes"];
                        }



                        abucketrow["WebsiteHosting"] = website;
                        abucketrow["Logging"] = "X";
                        abucketrow["Events"] = "X";
                        abucketrow["Versioning"] = "X";
                        abucketrow["LifeCycle"] = "X";
                        abucketrow["Replication"] = "X";
                        abucketrow["Tags"] = "X";
                        abucketrow["RequesterPays"] = "X";
                        ToReturn.Rows.Add(abucketrow.ItemArray);
                    }
                    catch (Exception ex)
                    {

                        abucketrow["AccountID"] = accountid;
                        abucketrow["Profile"] = aprofile;
                        abucketrow["Bucket"] = name;
                        abucketrow["Region"] = ex.InnerException.Message;

                        if (havesize)
                        {
                            abucketrow["StartDate"] = bucketsizedata["StartDate"];
                            abucketrow["StartSizeMin"] = bucketsizedata["StartSizeMin"];
                            abucketrow["StartSizeAVG"] = bucketsizedata["StartSizeAVG"];
                            abucketrow["StartSizeMax"] = bucketsizedata["StartSizeMax"];

                            abucketrow["EndDate"] = bucketsizedata["EndDate"];
                            abucketrow["EndSizeMin"] = bucketsizedata["EndSizeMin"];
                            abucketrow["EndSizeAVG"] = bucketsizedata["EndSizeAVG"];
                            abucketrow["EndSizeMax"] = bucketsizedata["EndSizeMax"];
                            abucketrow["EndSizeMaxBytes"] = bucketsizedata["EndSizeMaxBytes"];

                        }
                        else
                        {
                            abucketrow["StartDate"] = "NA";
                            abucketrow["StartSizeMin"] = "NA";
                            abucketrow["StartSizeAVG"] = "NA";
                            abucketrow["StartSizeMax"] = "NA";

                            abucketrow["EndDate"] = "NA";
                            abucketrow["EndSizeMin"] = "NA";
                            abucketrow["EndSizeAVG"] = "NA";
                            abucketrow["EndSizeMax"] = "NA";
                            abucketrow["EndSizeMaxBytes"] = 0;
                        }

                        ToReturn.Rows.Add(abucketrow.ItemArray);
                    }
                }




            }
            catch(Exception ex)
            {
                //Croak
            }
            int sizesreturned = Sizetable.Rows.Count;
            int rowsreturned = ToReturn.Rows.Count;
            return ToReturn;
        }

        /// <summary>
        /// Give a list of Profiles, return details on S3 buckets
        /// </summary>
        /// <param name="Profiles2Scan"></param>
        /// <returns></returns>
        public DataTable ScanS3(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("S3");
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            ParallelOptions po = new ParallelOptions();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            po.MaxDegreeOfParallelism = 128;
            
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetS3Buckets(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                WriteToEventLog("Failed scanning S3\n" + ex.Message, EventLogEntryType.Error);
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                try
                {
                    ToReturn.Merge(rabbit);
                }
                catch (Exception ex)
                {
                }
            }
            return ToReturn;
        }
        public DataTable GetSQSQ(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = new DataTable();

            string accountid = GetAccountID(aprofile);

            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            Amazon.Runtime.AWSCredentials credential;

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var sqsq = new Amazon.SQS.AmazonSQSClient(credential, Endpoint2scan);
                var daqs = sqsq.ListQueues("").QueueUrls;

            }
            catch
            {

            }




            return ToReturn;

        }


        public DataTable ScanSNSSubs(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("SNSSubs");
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 128;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value),  GetSNSSubscriptions(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetSNSSubscriptions(string aprofile, string Region2Scan)
        {

            DataTable ToReturn = AWSTables.GetSNSSubsTable();
            

            string accountid = GetAccountID(aprofile);

            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            Amazon.Runtime.AWSCredentials credential;

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);

                //SNS testing here
                var snsclient = new AmazonSimpleNotificationServiceClient(credential,Endpoint2scan);
                var subbies = snsclient.ListSubscriptions().Subscriptions;
                //var toppies = snsclient.ListTopics().Topics;

                foreach(var asub in subbies)
                {
                    var myrow = ToReturn.NewRow();
                    myrow["AccountID"] = accountid   ;
                    myrow["Profile"] = aprofile   ;
                    myrow["Region"] = Region2Scan   ;

                    myrow["Endpoint"] = asub.Endpoint   ;
                    myrow["Owner"] = asub.Owner   ;
                    myrow["Protocol"] = asub.Protocol   ;
                    myrow["SubscriptionARN"] = asub.SubscriptionArn   ;
                    myrow["TopicARN"] = asub.TopicArn   ;

                    if (asub.SubscriptionArn.Contains(accountid))
                    {
                        myrow["CrossAccount"] = "No"   ;
                    }
                    else
                    {
                        myrow["CrossAccount"] = "Yup";
                    }
                    var checkker = myrow["CrossAccount"];
                    ToReturn.Rows.Add(myrow);
                }
            }
            catch(Exception ex)
            {
                WriteToEventLog("SNS scan of " + aprofile + " failed\n" + ex.Message.ToString(), EventLogEntryType.Error);
            }
            return ToReturn;
        }


        public DataTable ScanEBS(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("EBS");
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 128;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetEBSDetails(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetEBSDetails(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetEBSDetailsTable();

            string accountid = GetAccountID(aprofile);

            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            Amazon.Runtime.AWSCredentials credential;

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential, Endpoint2scan);

                // Describe volumes has a max limit,  so we have to make sure we collect all the data we need.
                DescribeVolumesRequest requesty = new DescribeVolumesRequest();
                requesty.MaxResults = 1000;

                var volres = ec2.DescribeVolumes();
                var volyumes = volres.Volumes;
                List<Volume> vollist = new List<Volume>();

                while (volres.NextToken != null)
                {
                    foreach (var av in volyumes)
                    {
                        try { vollist.Add(av); }
                        catch (Exception ex)
                        {
                            WriteToEventLog("EBS on " + aprofile + "/" + Region2Scan + " failed:\n" + ex.Message,EventLogEntryType.Error);
                        }
                    }
                    requesty.NextToken = volres.NextToken;
                    volres = ec2.DescribeVolumes(requesty);
                }

                foreach (var av in volyumes) vollist.Add(av);

                foreach (var onevol in vollist)
                {
                    var arow = ToReturn.NewRow();
                    arow["AccountID"] = accountid;
                    arow["Profile"] = aprofile;
                    arow["Region"] = Region2Scan;

                    arow["AZ"] = onevol.AvailabilityZone;
                    arow["CreateTime"] = onevol.CreateTime.ToString();
                    arow["Encrypted"] = onevol.Encrypted.ToString();
                    arow["IOPS"] = onevol.Iops;
                    arow["KMSKeyID"] = onevol.KmsKeyId;
                    arow["Size-G"] = onevol.Size;
                    arow["SnapshotID"] = onevol.SnapshotId;
                    arow["State"] = onevol.State.Value;

                    arow["VolumeID"] = onevol.VolumeId;
                    arow["VolumeType"] = onevol.VolumeType.Value;

                    //**********  Some extra handling required**************///
                    List<string> taglist = new List<string>();
                    foreach (var atag in onevol.Tags)
                    {
                        taglist.Add(atag.Key + ": " + atag.Value);
                    }
                    arow["Tags"] = List2String(taglist);

                    var atachs = onevol.Attachments;
                    arow["Attachments"] = onevol.Attachments.Count.ToString();
                    if (onevol.Attachments.Count > 0)
                    {
                        arow["AttachTime"] = atachs[0].AttachTime;
                        arow["DeleteonTerm"] = atachs[0].DeleteOnTermination;
                        arow["Device"] = atachs[0].Device;
                        arow["InstanceID"] = atachs[0].InstanceId;
                        arow["AttachState"] = atachs[0].State;
                    }

                    ToReturn.Rows.Add(arow);

                }

            }
            catch (Exception ex)
            {
                WriteToEventLog("EBS on " + aprofile + " failed:\n" + ex.Message, EventLogEntryType.Error);
            }




            return ToReturn;

        }



        public DataTable ScanSnapshots(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("Snapshots");
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 256;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetSnapshotDetails(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetSnapshotDetails(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetSnapshotDetailsTable();

            string accountid = GetAccountID(aprofile);

            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            Amazon.Runtime.AWSCredentials credential;

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential, Endpoint2scan);

                // Describe snapshots has a max limit,  so we have to make sure we collect all the data we need.
                DescribeSnapshotsRequest requesty = new DescribeSnapshotsRequest();
                requesty.MaxResults = 1000;
                //Ouch!  It lists all snaps we have access to. We only want ones we own and pay for..
                //And it doesnt seem to return the ones we own. WTF????
                requesty.OwnerIds.Add("self");

                var snapres = ec2.DescribeSnapshots(requesty);
                var snappies = snapres.Snapshots;
                int nummie = snappies.Count;
                Dictionary<string, Snapshot> snaplist = new Dictionary<string, Snapshot>();

                while (snapres.NextToken != null)
                {
                    foreach (var av in snappies)
                    {
                        try
                        {
                            if (!snaplist.Keys.Contains(av.SnapshotId)) snaplist.Add(av.SnapshotId, av);
                            else
                            {
                                var goob = snaplist[av.SnapshotId];
                                if (goob.Equals(av))
                                {
                                    string itsadupe = "Yar";
                                }
                            }//Eliminate dupes
                        }
                        catch (Exception ex)
                        {
                            WriteToEventLog("Snapshots on " + aprofile + "/" + Region2Scan + " failed:\n" + ex.Message, EventLogEntryType.Error);
                        }
                    }
                    requesty.NextToken = snapres.NextToken;
                    snapres = ec2.DescribeSnapshots(requesty);
                }



                foreach (var av in snappies)
                {
                    if (!snaplist.Keys.Contains(av.SnapshotId)) snaplist.Add(av.SnapshotId, av);
                    else
                    {
                        var goob = snaplist[av.SnapshotId];
                        if (goob.Equals(av))
                        {
                            string itsadupe = "Yar";
                        }
                    }//Eliminate dupes.
                }

                foreach (var onesnap in snaplist.Values)
                {
                    var arow = ToReturn.NewRow();
                    if (!accountid.Equals(onesnap.OwnerId)) continue;
                    arow["AccountID"] = accountid;

                    var rr = onesnap.GetType();
                    arow["Profile"] = aprofile;
                    arow["Region"] = Region2Scan;
                    arow["SnapshotID"] = onesnap.SnapshotId;
                    arow["Description"] = onesnap.Description;
                    arow["VolumeID"] = onesnap.VolumeId;
                    arow["VolumeSize-GB"] = onesnap.VolumeSize;

                    arow["Encrypted"] = onesnap.Encrypted.ToString();
                    arow["KMSKeyID"] = onesnap.KmsKeyId;
                    arow["OwnerAlias"] = onesnap.OwnerAlias;
                    arow["OwnerID"] = onesnap.OwnerId;
                    arow["Progress"] = onesnap.Progress;
                    arow["StartTime"] = onesnap.StartTime.ToString();
                    arow["State"] = onesnap.State.Value;
                    arow["StateMessage"] = onesnap.StateMessage;

                    var DKI = onesnap.DataEncryptionKeyId;
                    if (String.IsNullOrEmpty(DKI)) { }
                    else
                    {
                        arow["DataEncryptionKeyID"] = onesnap.DataEncryptionKeyId.ToString();
                    }


                    //**********  Some extra handling required**************///
                    List<string> taglist = new List<string>();
                    foreach (var atag in onesnap.Tags)
                    {
                        taglist.Add(atag.Key + ": " + atag.Value);
                    }
                    arow["Tags"] = List2String(taglist);




                    ToReturn.Rows.Add(arow);

                }

            }
            catch (Exception ex)
            {
                WriteToEventLog("Snapshots on " + aprofile + " failed:\n" + ex.Message, EventLogEntryType.Error);
            }

            return ToReturn;

        }


        /// <summary>
        /// Given a List, convert to string with each item on list on separate row.
        /// </summary>
        /// <param name="stringlist"></param>
        /// <returns></returns>
        public string List2String(List<string> stringlist)
        {
            string toreturn = "";
            foreach (string astring in stringlist)
            {
                if (toreturn.Length > 2) toreturn += "\n";
                toreturn += astring;
            }
            return toreturn;
        }

        /// <summary>
        /// Given a profile name,  get the AccountID the profile is associated with.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <returns></returns>
        public string GetAccountID(string aprofile)
        {
            List<User> myUserList = new List<User>();
            string accountid = "";
            var credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            var iam = new AmazonIdentityManagementServiceClient(credential);

            try
            {
                myUserList = iam.ListUsers().Users;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            try
            {

                accountid = myUserList[0].Arn.Split(':')[4];//Get the ARN and extract the AccountID ID  
            }
            catch
            {
                accountid = "?";
            }
            return accountid;
        }
        /// <summary>
        /// Gets the data for EC2 Instances in a given Profile and Region.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <param name="Region2Scan"></param>
        /// <returns></returns>
        public DataTable GetEC2Instances(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetComponentTable("EC2");
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            Amazon.Runtime.AWSCredentials credential;
            credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            var ec2 = new Amazon.EC2.AmazonEC2Client(credential, Endpoint2scan);
            string accountid = GetAccountID(aprofile);
            var request = new DescribeInstanceStatusRequest();



            

            request.IncludeAllInstances = true;
            DescribeInstanceStatusResponse instatresponse = new DescribeInstanceStatusResponse();
            var indatarequest = new DescribeInstancesRequest();
            try
            {
                instatresponse = ec2.DescribeInstanceStatus(request);
            }
            catch (Exception ex)
            {
                string test = "";//Quepaso? 
            }

            //Get a list of the InstanceIDs.
            foreach (var instat in instatresponse.InstanceStatuses)
            {
                indatarequest.InstanceIds.Add(instat.InstanceId);
                indatarequest.InstanceIds.Sort();
            }

            DescribeInstancesResponse DescResult = ec2.DescribeInstances();
           
            int count = instatresponse.InstanceStatuses.Count();

            //Build data dictionary of instances
            Dictionary<String, Instance> Bunchadata = new Dictionary<string, Instance>();
            foreach (var urtburgle in DescResult.Reservations)
            {
                foreach (var instancedata in urtburgle.Instances)
                {
                    try { Bunchadata.Add(instancedata.InstanceId, instancedata); }
                    catch (Exception ex) {
                        var ff ="";//a duplicate??
                    };
                }
            }

            //Go through list of instances...
            foreach (var instat in instatresponse.InstanceStatuses)
            {
                
                string instanceid = instat.InstanceId;
                Instance thisinstance = new Instance();
                try
                {
                    thisinstance = Bunchadata[instanceid];
                }
                catch(Exception ex)
                {

                    continue;
                }
                DataRow thisinstancedatarow = ToReturn.NewRow();
                //Collect the datases
                string instancename = "";
                var status = instat.Status.Status;
                string AZ = instat.AvailabilityZone;
                var istate = instat.InstanceState.Name;

                string profile = aprofile;
                string myregion = Region2Scan;
                int eventnumber = instat.Events.Count();
                List<string> eventlist = new List<string>();
                var reservations = DescResult.Reservations;

                var myinstance = new Reservation();

                var atreq = new DescribeInstanceAttributeRequest();
                atreq.InstanceId = instanceid;
                atreq.Attribute = "disableApiTermination";
                var atresp = ec2.DescribeInstanceAttribute(atreq).InstanceAttribute;
                string TerminationProtection = atresp.DisableApiTermination.ToString();

                List<String> innies = new List<String>();
                foreach (Reservation arez in DescResult.Reservations)
                {
                    var checky = arez.Instances[0].InstanceId;
                    innies.Add(checky);
                    if (arez.Instances[0].InstanceId.Equals(instanceid))
                    {
                        myinstance = arez;
                    }
                }
                innies.Sort();

                List<string> tags = new List<string>();
                var loadtags = thisinstance.Tags.AsEnumerable();
                foreach (var atag in loadtags)
                {
                    tags.Add(atag.Key + ": " + atag.Value);
                    if (atag.Key.Equals("Name")) instancename = atag.Value;
                }

                Dictionary<string, string> taglist = new Dictionary<string, string>();
                foreach (var rekey in loadtags)
                {
                    taglist.Add(rekey.Key, rekey.Value);
                }

                if (eventnumber > 0)
                {
                    foreach (var anevent in instat.Events)
                    {
                        eventlist.Add(anevent.Description + " : " + anevent.NotAfter.ToString());
                    }
                }
                String platform = "";
                try { platform = thisinstance.Platform.Value; }
                catch { platform = "Linux"; }
                if (String.IsNullOrEmpty(platform)) platform = "Linux";


                String Priv_IP = "";
                try { Priv_IP = thisinstance.PrivateIpAddress; }
                catch { }
                if (String.IsNullOrEmpty(Priv_IP))
                {
                    Priv_IP = "?";
                }
                
                String disinstance = thisinstance.InstanceId;

                String publicIP = "";
                try { publicIP = thisinstance.PublicIpAddress; }
                catch { }
                if (String.IsNullOrEmpty(publicIP)) publicIP = "";

                

                String publicDNS = "";
                try { publicDNS = thisinstance.PublicDnsName; }
                catch { }
                if (String.IsNullOrEmpty(publicDNS)) publicDNS = "";

                string myvpcid = "";
                try
                { myvpcid = thisinstance.VpcId; }
                catch { }
                if (String.IsNullOrEmpty(myvpcid)) myvpcid = "";
                

                string mysubnetid = "";
                try { mysubnetid = thisinstance.SubnetId; }
                catch { }
                if (String.IsNullOrEmpty(mysubnetid)) mysubnetid = "";


                //Virtualization type (HVM, Paravirtual)
                string ivirtType = "";
                try
                { ivirtType = thisinstance.VirtualizationType; }
                catch { }
                if (String.IsNullOrEmpty(ivirtType)) ivirtType = "?";

                // InstanceType (m3/Large etc)
                String instancetype = "";
                try
                { instancetype = thisinstance.InstanceType.Value; }
                catch { }
                if (String.IsNullOrEmpty(instancetype)) instancetype = "?";


                //Test section to try to pull out AMI data
                string AMI = "";
                string AMIName = "";
                string AMIDesc = "";
                string AMILocation = "";
                string AMIState = "";
                try { AMI = thisinstance.ImageId; }
                catch { }
                if (string.IsNullOrEmpty(AMI)) AMI = "";
                else
                {
                    DescribeImagesRequest DIR = new DescribeImagesRequest();
                    DIR.ImageIds.Add(AMI);
                    var imresp = ec2.DescribeImages(DIR);
                    var idata = imresp.Images;
                    try {
                        if (idata.Count > 0)
                        {
                            AMIDesc = idata[0].Description;
                            AMIName = idata[0].Name;
                            AMILocation = idata[0].ImageLocation;
                            AMIState = idata[0].State;

                        }
                        else
                        {
                            AMIDesc = "na";
                            AMIName = "na";
                            AMILocation = "na";
                            AMIState = "na";
                        }
                        if (String.IsNullOrEmpty(AMIDesc)) { AMIDesc = ""; }
                        if (String.IsNullOrEmpty(AMIName)) { AMIName = ""; }
                    }
                    catch(Exception ex)
                    {
                        string whyforerror = ex.Message;
                    }
                 
                }

                //
                var SGs = thisinstance.SecurityGroups;
                List<string> SGids = new List<string>();
                List<String> SGNames = new List<string>();
                foreach (var wabbit in SGs)
                {
                    SGids.Add(wabbit.GroupId);
                    SGNames.Add(wabbit.GroupName);
                }



                //Add to table
                if (SGids.Count < 1) SGids.Add("NullOrEmpty");
                if (SGNames.Count < 1) SGNames.Add("");
                if (String.IsNullOrEmpty(SGids[0])) SGids[0] = "NullOrEmpty";
                if (String.IsNullOrEmpty(SGNames[0])) SGNames[0] = "";

                if (String.IsNullOrEmpty(instancename)) instancename = "";


                //EC2DetailsTable.Rows.Add(accountid, profile, myregion, instancename, instanceid, AMI, AMIDesc, AZ, platform, status, eventnumber, eventlist, tags, Priv_IP, publicIP, publicDNS, istate, ivirtType, instancetype, sglist);
                //Is list for Profile and Region, so can key off of InstanceID. In theory InstanceID is unique

                //Build our dictionary of values and keys for this instance  This is dependent on the table created by GetEC2DetailsTable()
                Dictionary<string, string> datafields = new Dictionary<string, string>();
                thisinstancedatarow["AccountID"] = accountid;
                thisinstancedatarow["Profile"] = profile;
                thisinstancedatarow["Region"] = myregion;
                thisinstancedatarow["InstanceName"] = instancename;
                thisinstancedatarow["InstanceID"] = instanceid;
                thisinstancedatarow["TerminationProtection"] = TerminationProtection;
                thisinstancedatarow["AMI"] = AMI;
                thisinstancedatarow["AMIState"] = AMIState;
                thisinstancedatarow["AMILocation"] = AMILocation;
                thisinstancedatarow["AMIDescription"] = AMIDesc;
                thisinstancedatarow["AvailabilityZone"] = AZ;
                thisinstancedatarow["Status"] = status;
                thisinstancedatarow["Events"] = eventnumber.ToString();
                thisinstancedatarow["EventList"] = List2String(eventlist);
                thisinstancedatarow["Tags"] = List2String(tags);
                thisinstancedatarow["PrivateIP"] = Priv_IP;
                thisinstancedatarow["PublicIP"] = publicIP;
                thisinstancedatarow["PublicDNS"] = publicDNS;
                thisinstancedatarow["PublicDNS"] = publicDNS;
                thisinstancedatarow["VPC"] = myvpcid;
                thisinstancedatarow["SubnetID"] = mysubnetid;
                thisinstancedatarow["InstanceState"] = istate.Value;
                thisinstancedatarow["VirtualizationType"] = ivirtType;
                thisinstancedatarow["InstanceType"] = instancetype;
                thisinstancedatarow["SecurityGroups"] = List2String(SGids);
                thisinstancedatarow["SGNames"] = List2String(SGNames);
                //Add this instance to the data returned.
                ToReturn.Rows.Add(thisinstancedatarow);


            }//End for of instances



            return ToReturn;
        }//EndGetEC2



        /// <summary>
        /// Given a list of profiles, return a datatable with VPC details for those profiles.
        /// </summary>
        /// <param name="List of Profiles"></param>
        /// <returns>Datatable of VPC details</returns>
        public DataTable ScanVPCs(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("VPC");
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 64;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetVPCs(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                try
                {
                    ToReturn.Merge(rabbit);
                }
                catch (Exception ex)
                {

                }
            }
            return ToReturn;
        }

        public DataTable GetVPCs(String aprofile, string Region2Scan)
        {
            string accountid = GetAccountID(aprofile);
            DataTable ToReturn = AWSTables.GetVPCDetailsTable();
            Amazon.Runtime.AWSCredentials credential;
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential, Endpoint2scan);
                var vippies = ec2.DescribeVpcs().Vpcs;

                foreach (var avpc in vippies)
                {
                    DataRow thisvpc = ToReturn.NewRow();
                    thisvpc["AccountID"] = accountid;
                    thisvpc["Profile"] = aprofile;
                    thisvpc["Region"] = Region2Scan;
                    thisvpc["VpcID"] = avpc.VpcId;
                    thisvpc["CidrBlock"] = avpc.CidrBlock;
                    thisvpc["IsDefault"] = avpc.IsDefault.ToString();
                    thisvpc["DHCPOptionsID"] = avpc.DhcpOptionsId;
                    thisvpc["InstanceTenancy"] = avpc.InstanceTenancy;
                    thisvpc["State"] = avpc.State;
                    var tagger = avpc.Tags;
                    List<string> tlist = new List<string>();
                    foreach (var atag in tagger)
                    {
                        tlist.Add(atag.Key + ": " + atag.Value);
                    }
                    thisvpc["Tags"] = List2String(tlist);

                    ToReturn.Rows.Add(thisvpc);
                }


            }//End of the big Try
            catch (Exception ex)
            {
                WriteToEventLog("VPC scan of " + aprofile + " failed:" + ex.Message.ToString(), EventLogEntryType.Error);
            }

            return ToReturn;
        }

        /// <summary>
        /// Given a List of Profiles, return IAM user data for each.
        /// </summary>
        /// <returns></returns>
        public DataTable ScanIAM(IEnumerable<string> Profiles2Scan)
        {

            DataTable ToReturn = AWSFunctions.AWSTables.GetUsersDetailsTable();
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 128;
            try
            {
                Parallel.ForEach(Profiles2Scan, po, (profile) => {
                    MyData.TryAdd((profile), GetIAMUsers(profile));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                WriteToEventLog("Failed scanning IAM\n" + ex.Message, EventLogEntryType.Error);
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                foreach (DataRow arow in rabbit.Rows)
                {
                    try
                    {
                        ToReturn.ImportRow(arow);
                    }
                    catch (Exception ex)
                    {
                        string fail = "";
                    }
                }


                // ToReturn.Merge(rabbit);  Had to remove due to merge constraints. Wanted more granular debugging.
            }
            return ToReturn;
        }

        public DataTable GetIAMUsers(string aprofile)
        {
            DataTable IAMTable = AWSTables.GetComponentTable("IAM"); //Blank table to fill out.

            Dictionary<string, string> UserNameIdMap = new Dictionary<string, string>();//Usernames to UserIDs to fill in row later.
            Amazon.Runtime.AWSCredentials credential;
            try
            {
                string accountid = GetAccountID(aprofile);
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var iam = new AmazonIdentityManagementServiceClient(credential);
                Dictionary<string, string> unamelookup = new Dictionary<string, string>();

                var myUserList = iam.ListUsers().Users;


                foreach (var rabbit in myUserList)
                {
                    unamelookup.Add(rabbit.UserId, rabbit.UserName);
                }
                var createcredreport = iam.GenerateCredentialReport();
                bool notdone = true;
                var genstart = DateTime.Now;
                while (notdone)
                {
                    var status = createcredreport.State;
                    if (status == ReportStateType.COMPLETE) notdone = false;
                    else
                    {
                        if (DateTime.Now > genstart + TimeSpan.FromMinutes(2))
                        {
                            DataRow auserdata = IAMTable.NewRow();
                            auserdata["AccountID"] = accountid;
                            auserdata["Profile"] = aprofile;
                            auserdata["UserID"] = "Credential Report";
                            auserdata["UserName"] = "Not ready";
                            IAMTable.Rows.Add(auserdata);
                            return IAMTable;

                        }
                        //Sometimes reports take a LOOOOONG time.

                    }
                }

                foreach (var auser in myUserList)
                {
                    UserNameIdMap.Add(auser.UserName, auser.UserId);
                }

                Amazon.IdentityManagement.Model.GetCredentialReportResponse credreport = new GetCredentialReportResponse();
                DateTime getreportstart = DateTime.Now;
                DateTime getreportfinish = DateTime.Now;

                try
                {
                    credreport = iam.GetCredentialReport();
                    //Wait for report to finish... how?

                    var goombah = credreport.ResponseMetadata.Metadata;

                    //while(credreport.ResponseMetadata.Metadata)


                    getreportfinish = DateTime.Now;
                    var dif = getreportstart - getreportfinish;  //Just a check on how long it takes.


                    //Extract data from CSV Stream into DataTable
                    var streambert = credreport.Content;

                    streambert.Position = 0;
                    StreamReader sr = new StreamReader(streambert);
                    string myStringRow = sr.ReadLine();
                    var headers = myStringRow.Split(",".ToCharArray()[0]);
                    if (myStringRow != null) myStringRow = sr.ReadLine();//Dump the header line
                    Dictionary<string, string> mydata = new Dictionary<string, string>();
                    while (myStringRow != null)
                    {
                        DataRow auserdata = IAMTable.NewRow();
                        var arow = myStringRow.Split(",".ToCharArray()[0]);

                        //Letsa dumpa da data...
                        auserdata["AccountID"] = accountid;
                        auserdata["Profile"] = aprofile;

                        string thisid = "";
                        string username = "";
                        try
                        {
                            thisid = UserNameIdMap[arow[0]];
                            auserdata["UserID"] = thisid;
                            auserdata["UserName"] = unamelookup[thisid];
                            if (unamelookup[thisid] == "<root_account>")
                            {
                                auserdata["UserID"] = "*-" + accountid + "-* root";
                            }
                            username = unamelookup[thisid];
                        }
                        catch
                        {
                            auserdata["UserID"] = "*-" + accountid + "-* root";
                            auserdata["UserName"] = "<root_account>";
                        }



                        auserdata["ARN"] = arow[1];
                        auserdata["CreateDate"] = arow[2];
                        auserdata["PwdEnabled"] = arow[3];
                        auserdata["PwdLastUsed"] = arow[4];
                        auserdata["PwdLastChanged"] = arow[5];
                        auserdata["PwdNxtRotation"] = arow[6].ToString();
                        auserdata["MFA Active"] = arow[7];

                        auserdata["AccessKey1-Active"] = arow[8];//access_key_1_active
                        auserdata["AccessKey1-Rotated"] = arow[9];//access_key_1_last_rotated
                        auserdata["AccessKey1-LastUsedDate"] = arow[10];//access_key_1_last_used_date
                        auserdata["AccessKey1-LastUsedRegion"] = arow[11];//access_key_1_last_used_region
                        auserdata["AccessKey1-LastUsedService"] = arow[12];//access_key_1_last_used_service

                        auserdata["AccessKey2-Active"] = arow[13];//access_key_2_active
                        auserdata["AccessKey2-Rotated"] = arow[14];//access_key_2_last_rotated
                        auserdata["AccessKey2-LastUsedDate"] = arow[15];//access_key_2_last_used_date
                        auserdata["AccessKey2-LastUsedRegion"] = arow[16];//access_key_2_last_used_region
                        auserdata["AccessKey2-LastUsedService"] = arow[17];//access_key_2_last_used_service

                        auserdata["Cert1-Active"] = arow[18];//cert_1_active
                        auserdata["Cert1-Rotated"] = arow[19];//cert_1_last_rotated
                        auserdata["Cert2-Active"] = arow[20];//cert_2_active
                        auserdata["Cert2-Rotated"] = arow[21];//cert_2_last_rotated

                        var extradata = GetUserDetails(aprofile, username);

                        auserdata["User-Policies"] = extradata["Policies"];
                        auserdata["Access-Keys"] = extradata["AccessKeys"];
                        auserdata["Groups"] = extradata["Groups"];

                        IAMTable.Rows.Add(auserdata);




                        myStringRow = sr.ReadLine();
                    }
                    sr.Close();
                    sr.Dispose();



                }
                catch (Exception ex)
                {
                    WriteToEventLog("IAM scan of " + aprofile + " failed\n" + ex.Message.ToString(), EventLogEntryType.Error);
                    //Deal with this later if necessary.
                }

                //Done stream, now to fill in the blanks...


            }
            catch//The final catch
            {
                string btest = "";
                //Deal with this later if necessary.
            }

            return IAMTable;
        }//EndIamUserScan

        /// <summary>
        /// Given a profile and user, collect additional information.
        /// </summary>
        /// <param name="aprofile">An AWS Profile name stored in Windows Credential Store</param>
        /// <param name="auser">The Name of a User</param>
        /// <returns>Dictionary containing keys for each type of data[AccessKeys], [Groups], [Policies]</returns>
        public Dictionary<string, string> GetUserDetails(string aprofile, string username)
        {
            var credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            var iam = new AmazonIdentityManagementServiceClient(credential);
            Dictionary<string, string> ToReturn = new Dictionary<string, string>();
            string policylist = "";
            string aklist = "";
            string groups = "";
            try
            {
                ListAccessKeysRequest LAKREQ = new ListAccessKeysRequest();
                LAKREQ.UserName = username;
                var LAKRES = iam.ListAccessKeys(LAKREQ);
                foreach (var blivet in LAKRES.AccessKeyMetadata)
                {
                    if (aklist.Length > 1) aklist += "\n";
                    aklist += blivet.AccessKeyId + "  :  " + blivet.Status;
                }
            }
            catch { aklist = ""; }

            try
            {
                ListAttachedUserPoliciesRequest LAUPREQ = new ListAttachedUserPoliciesRequest();
                LAUPREQ.UserName = username;
                var LAUPRES = iam.ListAttachedUserPolicies(LAUPREQ);
                foreach (var apol in LAUPRES.AttachedPolicies)
                {
                    if (policylist.Length > 1) policylist += "\n";
                    policylist += apol.PolicyName;
                }
            }
            catch { policylist = ""; }

            try
            {
                var groopsreq = new ListGroupsForUserRequest();
                groopsreq.UserName = username;
                var LG = iam.ListGroupsForUser(groopsreq);
                foreach (var agroup in LG.Groups)
                {
                    if (groups.Length > 1) groups += "\n";
                    groups += agroup.GroupName;
                }
            }
            catch { groups = ""; }

            ToReturn.Add("Groups", groups);
            ToReturn.Add("Policies", policylist);
            ToReturn.Add("AccessKeys", aklist);
            return ToReturn;
        }


        public DataTable CreateUserRequestTable(IEnumerable<string> Profiles2Scan)
        {
            var ToReturn = AWSTables.CreateUsersRequestTable();

            //Need to get a list of the Groups in each account to build der menu.




            return ToReturn;

        }

        public List<string> GetGroups(string aprofile)
        {
            List<string> ToReturn = new List<string>();
            Amazon.Runtime.AWSCredentials credential;
            try
            {
                string accountid = GetAccountID(aprofile);
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var iam = new AmazonIdentityManagementServiceClient(credential);
                ListGroupsRequest req = new ListGroupsRequest();
                req.MaxItems = 100;
                
                var GROOPLIST = iam.ListGroups(req).Groups;
                foreach(var agroup in GROOPLIST)
                {
                    ToReturn.Add(agroup.GroupName);
                }
            }
            catch(Exception ex)
            {

            }



                return ToReturn;

        }
        /// <summary>
        /// Just playing with this for now.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public string CreateIAMAccount(string aprofile, string username, string password)
        {
            string IRReturning = "Yop";
            var credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            var iam = new AmazonIdentityManagementServiceClient(credential);

            CreateUserRequest request = new CreateUserRequest();
            request.UserName = username;

            var repo = iam.CreateUser(request);
            var det = repo.ResponseMetadata;

            CreateAccessKeyRequest KeyRequest = new CreateAccessKeyRequest();
            KeyRequest.UserName = username;




            return IRReturning;
        }

        public DataTable ScanCerts(IEnumerable<string> Profiles2Scan)
        {

            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("Certs");
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 64;
            try
            {
                Parallel.ForEach(Profiles2Scan, po, (profile) => {
                    MyData.TryAdd((profile), GetIAMUsers(profile));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                WriteToEventLog("Failed scanning Certs\n" + ex.Message, EventLogEntryType.Error);
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                foreach (DataRow arow in rabbit.Rows)
                {
                    try
                    {
                        ToReturn.ImportRow(arow);
                    }
                    catch (Exception ex)
                    {
                        string fail = "";
                    }
                }


                // ToReturn.Merge(rabbit);  Had to remove due to merge constraints. Wanted more granular debugging.
            }
            return ToReturn;
        }

        public DataTable GetCertDetails(string aprofile)
        {
            string accountid = GetAccountID(aprofile);
            DataTable ToReturn = AWSTables.GetCertDetailsTable();
            Amazon.Runtime.AWSCredentials credential;
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var iam = new AmazonIdentityManagementServiceClient(credential);
                var cervix = iam.ListServerCertificates();


                //How to get certificate details????


                foreach (var acert in cervix.ServerCertificateMetadataList)
                {
                    DataRow disone = ToReturn.NewRow();
                    disone["AccountID"] = accountid;
                    disone["Profile"] = aprofile;
                    disone["CertName"] = acert.ServerCertificateName;

                    //Cert Details
                    //disone["Status"] = acert.;
                    //disone["InUse"] = acert.xxx;
                    //disone["DomainName"] = acert.xxx;
                    //disone["AdditionalNames"] = acert.xxx;
                    disone["Identifier"] = acert.ServerCertificateId;
                    //disone["SerialNumber"] = acert.xxx;
                    //disone["AssociatedResources"] = acert.xxx;
                    //disone["RequestedAt"] = acert.xxx;
                    //disone["IssuedAt"] = acert.xxx;
                    //disone["NotBefore"] = acert.xxx;
                    disone["NotAfter"] = acert.Expiration;
                    //disone["PublicKeyInfo"] = acert.xxx;
                    //disone["SignatureAlgorithm"] = acert.xxx;
                    disone["ARN"] = acert.Arn;
                    ToReturn.Rows.Add(disone);
                }

            }//End of the big Try
            catch (Exception ex)
            {
                //Whyfor did it fail?
                string w = "";
            }

            return ToReturn;
        }

        public DataTable ScanDNS(IEnumerable<string> Profiles2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetDNSDetailTable();



            return ToReturn;
        }

        public DataTable GetDNS(string aprofile)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetDNSDetailTable();
            string accountid = GetAccountID(aprofile);
            Amazon.Runtime.AWSCredentials credential;
            try
            {
                credential = GetCredentials(aprofile);
                var getem = new AmazonRoute53Client(credential);

                //Get list of Zones
                
                //Get List of records
                

            }
            catch (Exception ex)
            {
                string rabbit = "";
            }
            return ToReturn;
        }


        public DataTable ScanSubnets(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetSubnetDetailsTable();
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 64;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetSubnets(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {


                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetSubnets(string aprofile, string Region2Scan)
        {

            string accountid = GetAccountID(aprofile);

            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            Amazon.Runtime.AWSCredentials credential;
            DataTable ToReturn = AWSTables.GetComponentTable("Subnets");

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential, Endpoint2scan);
                var subbies = ec2.DescribeSubnets().Subnets;

                foreach (var asubnet in subbies)
                {
                    DataRow disone = ToReturn.NewRow();
                    disone["AccountID"] = accountid;
                    disone["Profile"] = aprofile;
                    disone["AvailabilityZone"] = asubnet.AvailabilityZone;
                    disone["AvailableIPCount"] = asubnet.AvailableIpAddressCount.ToString();
                    disone["Cidr"] = asubnet.CidrBlock;
                    //Trickybits.  Cidr to IP
                    //var dater = Network2IpRange(asubnet.CidrBlock);
                    System.Net.IPNetwork danetwork = System.Net.IPNetwork.Parse(asubnet.CidrBlock);

                    disone["_Network"] = danetwork.Network;
                    disone["_Netmask"] = danetwork.Netmask;
                    disone["_Broadcast"] = danetwork.Broadcast;
                    disone["_FirstUsable"] = danetwork.FirstUsable;
                    disone["_LastUsable"] = danetwork.LastUsable;

                    ///
                    disone["DefaultForAZ"] = asubnet.DefaultForAz.ToString();
                    disone["MapPubIPonLaunch"] = asubnet.MapPublicIpOnLaunch.ToString();
                    disone["State"] = asubnet.State;
                    disone["SubnetID"] = asubnet.SubnetId;
                    var tagger = asubnet.Tags;
                    List<string> taglist = new List<string>();
                    foreach (var atag in tagger)
                    {
                        taglist.Add(atag.Key + ": " + atag.Value);
                        if (atag.Key.Equals("Name")) disone["SubnetName"] = atag.Value;
                    }

                    disone["Tags"] = List2String(taglist);
                    disone["VpcID"] = asubnet.VpcId;

                    ToReturn.Rows.Add(disone);
                }


            }
            catch (Exception ex)
            {
                string rabbit = "";
            }




            return ToReturn;
        }

        public DataTable ScanRDS(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("RDS");
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 64;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetRDS(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetRDS(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetRDSDetailsTable();
            string accountid = GetAccountID(aprofile);
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            Amazon.Runtime.AWSCredentials credential;


            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var RDS = new Amazon.RDS.AmazonRDSClient(credential, Endpoint2scan);
                var RDSi = RDS.DescribeDBInstances();
                foreach (var anRDS in RDSi.DBInstances)
                {
                    DataRow disone = ToReturn.NewRow();
                    //Handle the List Breakdowns
                    var sgs = anRDS.DBSecurityGroups;
                    List<string> sglist = new List<string>();
                    foreach (var sg in sgs) { sglist.Add(sg.DBSecurityGroupName + ": " + sg.Status); }
                    var DBSecurityGroups = List2String(sglist);

                    List<string> vsg = new List<string>();
                    var w = anRDS.VpcSecurityGroups;
                    foreach (var sg in w) { vsg.Add(sg.VpcSecurityGroupId + ": " + sg.Status); }
                    var VPCSecurityGroups = List2String(vsg);

                    //StraightMappings + Mappings of breakdowns.

                    disone["AccountID"] = GetAccountID(aprofile);
                    disone["Profile"] = aprofile;
                    disone["AvailabilityZone"] = anRDS.AvailabilityZone;
                    disone["InstanceID"] = anRDS.DBInstanceIdentifier;
                    disone["Name"] = anRDS.DBName;
                    disone["Status"] = anRDS.DBInstanceStatus;
                    disone["EndPoint"] = anRDS.Endpoint.Address + ":" + anRDS.Endpoint.Port;

                    disone["InstanceClass"] = anRDS.DBInstanceClass;
                    disone["IOPS"] = anRDS.Iops.ToString();

                    disone["StorageType"] = anRDS.StorageType;
                    disone["AllocatedStorage"] = anRDS.AllocatedStorage;
                    disone["Engine"] = anRDS.Engine;
                    disone["EngineVersion"] = anRDS.EngineVersion;
                    disone["AutoMinorUpgrade"] = anRDS.AutoMinorVersionUpgrade.ToString();
                    disone["Created"] = anRDS.InstanceCreateTime.ToString();
                    ToReturn.Rows.Add(disone);
                }


            }
            catch (Exception ex)
            {
                string rabbit = "";
            }






            return ToReturn;
        }

        public DataTable ScanELB(IEnumerable<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetComponentTable("ELB");
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan.AsEnumerable();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 64;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetELBs(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                ToReturn.Merge(rabbit);
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        public DataTable GetELBs(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetELBsDetailTable();
            string accountid = GetAccountID(aprofile);
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }
            Amazon.Runtime.AWSCredentials credential;

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ELBClient = new Amazon.ElasticLoadBalancing.AmazonElasticLoadBalancingClient(credential, Endpoint2scan);
                var ELBs = ELBClient.DescribeLoadBalancers().LoadBalancerDescriptions;


                foreach (var anELB in ELBs)
                {
                    DataRow disone = ToReturn.NewRow();
                    //Handle the List Breakdowns

                    var AZs = List2String( anELB.AvailabilityZones);
                    var sgs = List2String(anELB.SecurityGroups);

                    //The tricky part.  Trying to figure out ELB listeners with Certificates associated.
                    List<string> Listeners = new List<string>();
                    List<string> CertListen = new List<string>();
                    var SiteCert = new X509Certificate2();
                    string whyforfail = "";
                    string CheckDaHealth = "";
                    foreach (var alistener in anELB.ListenerDescriptions)
                    {

                        var protocol = alistener.Listener.Protocol;
                        var externalport = alistener.Listener.LoadBalancerPort;
                        var host = anELB.DNSName;
                        var health = anELB.HealthCheck.Target;
                        

                        //Try to convert the health check string to an actual URL to test.
                        string[] banana = health.Split(':');
                        string urlpath = banana[1];

                        int index = urlpath.IndexOf('/');
                        if (index >= 0)
                        {
                            urlpath= urlpath.Substring(index + 1);
                        }
                        string chekkit= protocol + @"://" + host + ":"+externalport +  @"/" + urlpath;

                        //Try a test
                        
                        try
                        {

                            WebRequest request = WebRequest.Create(chekkit);
                            request.Timeout = 5000;
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            if (response == null )
                            {
                                CheckDaHealth += "Null Response";
                            }
                            if(response.StatusCode != HttpStatusCode.OK)
                            {
                                CheckDaHealth += response.StatusDescription;
                            }
                            CheckDaHealth += "Ok ";
                        }
                        catch(Exception ex)
                        {
                            CheckDaHealth += chekkit +": "+ex.Message;
                        }


                            string connection = protocol + @":\\" + host + ":" + externalport;


                        Listeners.Add(connection);
                        if (!String.IsNullOrEmpty(alistener.Listener.SSLCertificateId))//If it has a certificate.
                        {
                            CertListen.Add(connection);
                            try
                            {
                                //SiteCert = GetCertificate(connection);

                                SiteCert = GetSSLCertificate2(host,externalport);

                            }
                            catch(Exception ex)
                            {
                                whyforfail = ex.Message;
                                var humph = "";
                            }
                        }
                        
                    }


                    List<string> instances = new List<string>();
                    foreach(var aninstance in anELB.Instances)
                    {
                        instances.Add(aninstance.InstanceId);
                    }




                    //StraightMappings + Mappings of breakdowns.

                    disone["AccountID"] = GetAccountID(aprofile);
                    disone["Profile"] = aprofile;
                    disone["AvailabilityZone"] = AZs;
                    disone["Name"] = anELB.LoadBalancerName;

                    disone["DNSName"] = anELB.DNSName;

                    disone["Scheme"] = anELB.Scheme;
                    disone["InstanceCount"] = anELB.Instances.Count;
                    disone["Instances"] = List2String(instances);

                    disone["Listeners"] = List2String(Listeners);
                    disone["CertListeners"] = List2String(CertListen);

                    disone["HealthCheck"] = anELB.HealthCheck.Target;
                    disone["Status"] = CheckDaHealth;
                    disone["SecurityGroups"] = sgs;


                    //Disabling for now.
                    try
                    {
                        if (!(SiteCert == null))
                        {
                            disone["NotBefore"] = SiteCert.NotBefore;
                            disone["NotAfter"] = SiteCert.NotAfter;
                            disone["Issuer"] = SiteCert.Issuer;
                            disone["Subject"] = SiteCert.Subject;
                            disone["Thumbprint"] = SiteCert.Thumbprint;
                        }
                        else { disone["Subject"] = whyforfail; }
                    }
                    catch(Exception ex)
                    {
                        var e = 1;
                    }

                    ToReturn.Rows.Add(disone);
                }


            }
            catch (Exception ex)
            {
                string rabbit = "";
            }






            return ToReturn;
        }

        public DataTable ScanEC2(List<KeyValuePair<string, string>> ProfilesandRegions2Scan)
        {
            DataTable ToReturn = AWSFunctions.AWSTables.GetEC2DetailsTable();
            var start = DateTime.Now;
            ConcurrentDictionary<string, DataTable> MyData = new ConcurrentDictionary<string, DataTable>();
            var myscope = ProfilesandRegions2Scan;
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 256;
            try
            {
                Parallel.ForEach(myscope, po, (KVP) => {
                    MyData.TryAdd((KVP.Key + ":" + KVP.Value), GetEC2Instances(KVP.Key, KVP.Value));
                });
            }
            catch (Exception ex)
            {
                ToReturn.TableName = ex.Message.ToString();
                WriteToEventLog("Failed scanning EC2\n" + ex.Message, EventLogEntryType.Error);
                return ToReturn;
            }
            foreach (var rabbit in MyData.Values)
            {
                try
                {
                    foreach (DataRow arow in rabbit.Rows)
                    {
                        try
                        {
                            ToReturn.ImportRow(arow);
                        }
                        catch (Exception ex)
                        {
                            WriteToEventLog(arow[0] + "\n" + ex.Message, EventLogEntryType.Error);
                        }
                    }
                }
                catch
                {
                    //whyforfail

                }
            }
            var end = DateTime.Now;
            var duration = end - start;
            string dur = duration.TotalSeconds.ToString();
            return ToReturn;
        }

        /// <summary>
        /// Using function to get credentials, so we can handle different credential stores...  In Progress.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <returns></returns>
        public AWSCredentials GetCredentials(string aprofile)
        {
            var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();
            AWSCredentials awsCredentials;
            if (chain.TryGetAWSCredentials(aprofile, out awsCredentials))
            {
                // use awsCredentials
                return awsCredentials;
            }
            else
            {
                return awsCredentials;
            }




        }
        
        public DataTable FilterDataTable(DataTable Table2Filter, string filterstring, bool casesensitive)
        {
            if (Table2Filter.Rows.Count < 1) return Table2Filter;// No data to process..  Boring!
            DataTable ToReturn = Table2Filter.Copy();
            ToReturn.Clear();

            string currentname = Table2Filter.TableName;
            int originalnumberofrows = Table2Filter.Rows.Count;

            //Loop through Data table provided by datarows
            foreach (DataRow arow in Table2Filter.AsEnumerable())
            {
                //Loop through each column in datarow.
                foreach (DataColumn acolumn in Table2Filter.Columns)
                {
                    string temp = arow[acolumn].ToString();

                    if (casesensitive)
                    {
                        if (arow[acolumn].ToString().Contains(filterstring))
                        {
                            ToReturn.ImportRow(arow);
                            break;
                        }
                    }
                    else
                    {
                        if (arow[acolumn].ToString().ToUpper().Contains(filterstring.ToUpper()))
                        {
                            ToReturn.ImportRow(arow);
                            break;
                        }
                    }
                }

                //If match (case or caseless) add datarow to toreturn and break search of columns.
            }




            return ToReturn;
        }
        public DataTable FilterDataTable(DataTable Table2Filter, string column2filter, string filterstring, bool casesensitive)
        {
            DataTable ToReturn = new DataTable();
            string currentname = Table2Filter.TableName;
            int originalnumberofrows = Table2Filter.Rows.Count;

            if (Table2Filter.Rows.Count < 1) return Table2Filter;// No data to process..  Boring!
            bool anycolumn = false;
            if (column2filter.Contains("_Any_"))
            {
                anycolumn = true;
            }
            //Lets try to build sum queeries.



            string CASEquery = "p=> ";
            string nocasequery = "p=> ";
            int colno = Table2Filter.Columns.Count;

            for (int i = 0; i < Table2Filter.Columns.Count; i++)
            {
                if (i == colno)
                {
                    CASEquery += @"p.Field<string>(""+Table2Filter.Columns[i]+  "").ToString().Contains(FilterTagText.Text) ; ";
                    nocasequery += @"p.Field<string>(""+Table2Filter.Columns[i]+  "").ToString().ToLower().Contains(FilterTagText.Text) ; ";
                }
                else
                {
                    CASEquery += @"p.Field<string>(""+Table2Filter.Columns[i]+  "").ToString().Contains(FilterTagText.Text) || ";
                    nocasequery += @"p.Field<string>(""+Table2Filter.Columns[i]+  "").ToString().ToLower().Contains(FilterTagText.Text) || ";
                }
            }


            //Scan any columns.  
            if (anycolumn)
            {
                return (FilterDataTable(Table2Filter, filterstring, casesensitive));
            }

            //Scan one column
            else
            {
                DataTable newt = new DataTable();
                if (casesensitive)
                {
                    
                    try
                    {
                         newt = Table2Filter.AsEnumerable().Where(p => p.Field<string>(column2filter).ToString().ToUpper().Contains(filterstring.ToUpper())).CopyToDataTable();
                        var debugme = newt.Rows[0][11];
                    }
                    catch
                    {

                    }
                    if (newt.Rows.Count > 0) ToReturn = newt;
                    else//If empty search,  copy the source table to keep column names, then clear to indicate no values found.
                    {
                        ToReturn.Merge(Table2Filter);
                        ToReturn.Clear();
                    }
                }
                else
                {
                    try
                    {
                        newt = Table2Filter.AsEnumerable().Where(p => p.Field<string>(column2filter).ToString().Contains(filterstring)).CopyToDataTable();
                    }
                    catch { }
                    if (newt.Rows.Count > 0) ToReturn = newt;
                    else//If empty search,  copy the source table to keep column names, then clear to indicate no values found.
                    {
                        ToReturn.Merge(Table2Filter);
                        ToReturn.Clear();
                    }
                }
            }

            if (ToReturn.Rows.Count == originalnumberofrows) ToReturn.TableName = currentname;
            else ToReturn.TableName = currentname + " filtered: " + ToReturn.Rows.Count.ToString() + " out of " + originalnumberofrows.ToString();
            return ToReturn;
        }
        /// <summary>
        /// Note, must be run once as Administrator to set up the Event Source?
        /// </summary>
        /// <param name="sEvent"></param>
        public void WriteToEventLog(string sEvent)
        {
            WriteToEventLog(sEvent, EventLogEntryType.Information);
        }

        public void WriteToEventLog(string sEvent,EventLogEntryType eventtype)
        {
            var sSource = "Trycorder ScanAWS Lib";
            var sLog = "Application";
            try
            {
                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, sLog);
                EventLog.WriteEntry(sSource, sEvent,eventtype);
                
            }
            catch
            {
                //Probably cant write to event log. Need to run as Admin once!
            }
        }

        public string ExportToExcel(Dictionary<string, DataTable> DataTables, string ExcelFilePath)
        {

            try
            {
                Microsoft.Office.Interop.Excel.Application Excel = new Microsoft.Office.Interop.Excel.Application();
                // load excel, and create a new workbook
                var wb = Excel.Workbooks.Add();


                foreach (string DT2WS in DataTables.Keys)
                {
                    wb.Sheets.Add();
                    int ColumnsCount;
                    var aDT = DataTables[DT2WS];

                    if (aDT == null || (ColumnsCount = aDT.Columns.Count) == 0)
                        throw new Exception("ExportToExcel: Null or empty input table!\n");

                    // single worksheet
                    Microsoft.Office.Interop.Excel._Worksheet Worksheet = Excel.ActiveSheet;
                    Worksheet.Name = DT2WS;

                    object[] Header = new object[ColumnsCount];

                    // column headings               
                    for (int i = 0; i < ColumnsCount; i++)
                    {
                        Header[i] = aDT.Columns[i].ColumnName;
                    }

                    Microsoft.Office.Interop.Excel.Range HeaderRange = Worksheet.get_Range((Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[1, 1]), (Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[1, ColumnsCount]));
                    HeaderRange.Value = Header;
                    //HeaderRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightGray);
                    HeaderRange.Font.Bold = true;

                    // DataCells
                    int RowsCount = aDT.Rows.Count;
                    object[,] Cells = new object[RowsCount, ColumnsCount];

                    for (int j = 0; j < RowsCount; j++)
                        for (int i = 0; i < ColumnsCount; i++)
                            Cells[j, i] = aDT.Rows[j][i];

                    Worksheet.get_Range((Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[2, 1]), (Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[RowsCount + 1, ColumnsCount])).Value = Cells;
                    Worksheet.get_Range((Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[2, 1]), (Microsoft.Office.Interop.Excel.Range)(Worksheet.Cells[RowsCount + 1, ColumnsCount])).NumberFormat = "@";

                }



                //Ok, now to output the file...



                // check filepath
                if (ExcelFilePath != null && ExcelFilePath != "")
                {
                    try
                    {
                        wb.SaveAs(ExcelFilePath);
                        Excel.Quit();
                        return "Excel File Saved!";

                    }
                    catch (Exception ex)
                    {
                        return "ExportToExcel: Excel file could not be saved! Check filepath.\n" + ex.Message;
                    }
                }
                else    // no filepath is given
                {
                    Excel.Visible = true;
                    return "ExportToExcel: No filepath given!";
                }
            }
            catch (Exception ex)
            {
                return "ExportToExcel: \n" + ex.Message;
            }
        }


        public X509Certificate2 GetSSLCertificate2(string DNSName, int port)
        {

            X509Certificate2 cert = null;
            using (TcpClient client = new TcpClient())
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;           
                client.Connect(DNSName, port);

                SslStream ssl = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                try
                {
                    ssl.AuthenticateAsClient(DNSName);
                }
                catch (AuthenticationException e)
                {
                    //log.Debug(e.Message);
                    ssl.Close();
                    client.Close();
                    return cert;
                }
                catch (Exception e)
                {
                    //log.Debug(e.Message);
                    ssl.Close();
                    client.Close();
                    return cert;
                }
                cert = new X509Certificate2(ssl.RemoteCertificate);
                ssl.Close();
                client.Close();
                return cert;
            }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not /// Do allow this client to communicate with unauthenticated servers. 
            return true;
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



        /// <summary>
        /// Try to pull some other misc cloudwatch daters.
        /// </summary>
        /// <param name="aprofile"></param>
        /// <param name="Region2Scan"></param>
        /// <returns></returns>
        public DataTable GenericCloudWatch(string aprofile, string Region2Scan, string leNamespace, string leMEtricName)
        {
            DataTable ToReturn = AWSTables.GetS3SizesTable();

            string accountid = GetAccountID(aprofile);
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            //Convert the Region2Scan to an AWS Endpoint.
            foreach (var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if (aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            DataTable metlist = new DataTable();
            try
            {
                var credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                AmazonCloudWatchClient CWClient = new AmazonCloudWatchClient(credential, Endpoint2scan);
                Amazon.CloudWatch.Model.ListMetricsRequest LMReq = new Amazon.CloudWatch.Model.ListMetricsRequest();
                

                //Using to explore the metrics
                LMReq.Namespace = leNamespace;
                LMReq.MetricName = leMEtricName;
                var getmetrics = CWClient.ListMetrics(LMReq).Metrics;

                //This is just stuff I used to view data from the List
                metlist.Columns.Add("MetricName");
                metlist.Columns.Add("NameSpace");

                //These are the dimensions for S3.
                metlist.Columns.Add("Bucketname");
                metlist.Columns.Add("StorageType");

                foreach (var ametric in getmetrics)
                {
                 //What are we doing???
                }

                // Okay, collect the daters for these here buckets




                foreach (var abucket in metlist.AsEnumerable())
                {
                    Amazon.CloudWatch.Model.GetMetricStatisticsRequest GMReq = new Amazon.CloudWatch.Model.GetMetricStatisticsRequest();
                    string bucketname = abucket[2].ToString();
                    string storagetype = abucket[3].ToString();

                    try
                    {
                        Amazon.CloudWatch.Model.Dimension dimbo = new Amazon.CloudWatch.Model.Dimension();
                        dimbo.Name = "BucketName";
                        dimbo.Value = bucketname;
                        GMReq.Dimensions.Add(dimbo);
                        Amazon.CloudWatch.Model.Dimension dimbo2 = new Amazon.CloudWatch.Model.Dimension();
                        dimbo2.Name = "StorageType";
                        dimbo2.Value = storagetype;
                        GMReq.Dimensions.Add(dimbo2);

                        //Build the request:
                        GMReq.Namespace = "AWS/S3";
                        GMReq.EndTime = DateTime.Now;
                        GMReq.StartTime = DateTime.Now - TimeSpan.FromDays(21);
                        GMReq.Period = (60 * 60 * 24 * 7);//Seconds in a week.
                        GMReq.Statistics.Add("Minimum");
                        GMReq.Statistics.Add("Maximum");
                        GMReq.Statistics.Add("Average");
                        GMReq.MetricName = "BucketSizeBytes";

                        //Execute request:
                        var metricresponse = CWClient.GetMetricStatistics(GMReq);

                        //Process Return
                        var dp = metricresponse.Datapoints;
                        if (dp.Count == 0)
                        {
                            //none
                        }


                        var arow = ToReturn.NewRow();

                        Dictionary<DateTime, Amazon.CloudWatch.Model.Datapoint> sortem = new Dictionary<DateTime, Amazon.CloudWatch.Model.Datapoint>();
                        foreach (var ap in dp)
                        {
                            sortem.Add(ap.Timestamp, ap);
                        }
                        var sorteddates = sortem.Keys.ToList();
                        sorteddates.Sort();
                        var firstpass = true;
                        foreach (var key in sorteddates)
                        {
                            var ap = sortem[key];
                            var min = ap.Minimum;
                            var max = ap.Maximum;
                            var av = ap.Average;
                            var ts = ap.Timestamp;

                            if (firstpass)
                            {
                                firstpass = false;
                                arow["AccountID"] = accountid;
                                arow["Profile"] = aprofile;
                                arow["Bucket"] = bucketname;
                                arow["Region"] = Region2Scan;
                                arow["StartDate"] = ts.ToShortDateString();
                                arow["EndDate"] = ts.ToShortDateString();
                                arow["StartSizeMin"] = GetFileSize(min);
                                arow["StartSizeMax"] = GetFileSize(max);
                                arow["StartSizeAVG"] = GetFileSize(av);
                            }
                            else
                            {
                                arow["EndDate"] = ts.ToShortDateString();
                                arow["EndSizeMin"] = GetFileSize(min);
                                arow["EndSizeAVG"] = GetFileSize(av);
                                arow["EndSizeMax"] = GetFileSize(max);
                                arow["EndSizeMaxBytes"] = Math.Round(av);

                            }
                        }
                        ToReturn.Rows.Add(arow.ItemArray);
                    }
                    catch (Exception ex)
                    {

                    }
                }


            }
            catch (Exception ex)
            {

            }
            int buckets = metlist.Rows.Count;

            int returning = ToReturn.Rows.Count;
            return ToReturn;
        }



    }//EndScanAWS

    public class AWSTables
    {
        /// <summary>
        /// Returns a datatable associated with a particular component.
        /// </summary>
        /// <param name="The name of a supported component"></param>
        /// <returns></returns>
        public static DataTable GetComponentTable(string component)
        {
            switch (component.ToLower())
            {
                case "ec2":
                    return GetEC2DetailsTable();
                case "snapshots":
                    return GetSnapshotDetailsTable();
                case "rds":
                    return GetRDSDetailsTable();
                case "ebs":
                    return GetEBSDetailsTable();
                case "iam":
                    return GetUsersDetailsTable();
                case "s3":
                    return GetS3DetailsTable();
                case "snssubs":
                    return GetSNSSubsTable();
                case "subnets":
                    return GetSubnetDetailsTable();
                case "vpc":
                    return GetVPCDetailsTable();
                case "elb":
                    return GetELBsDetailTable();
                case "dns":
                    return GetDNSDetailTable();
                default:
                    return GetEC2DetailsTable();
            }
        }

        public static DataTable GetELBsDetailTable()
        {
            DataTable table = new DataTable();
            table.TableName = "LoadBalancerTable";
            // Here we create a DataTable .
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("AvailabilityZone", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Scheme", typeof(string));
            table.Columns.Add("DNSName", typeof(string));

            table.Columns.Add("PortConf", typeof(string));
            table.Columns.Add("InstanceCount", typeof(int));
            table.Columns.Add("Instances", typeof(string));
            table.Columns.Add("Listeners", typeof(string));
            table.Columns.Add("CertListeners", typeof(string));
            table.Columns.Add("HealthCheck", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("SecurityGroups", typeof(string));

            table.Columns.Add("Issuer", typeof(string));
            table.Columns.Add("Subject", typeof(string));
            table.Columns.Add("NotBefore", typeof(string));
            table.Columns.Add("NotAfter", typeof(string));
            table.Columns.Add("Thumbprint", typeof(string));

            //Can we set the view on this table to expand IEnumerables?
            DataView mydataview = table.DefaultView;

            return table;
        }

        public static DataTable GetRDSDetailsTable()
        {
            DataTable table = new DataTable();
            table.TableName = "RDSTable";
            // Here we create a DataTable .
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("AvailabilityZone", typeof(string));
            table.Columns.Add("InstanceID", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Status", typeof(string));


            table.Columns.Add("EndPoint", typeof(string));

            table.Columns.Add("InstanceClass", typeof(string));
            table.Columns.Add("IOPS", typeof(string));
            table.Columns.Add("AllocatedStorage", typeof(string));
            table.Columns.Add("StorageType", typeof(string));

            table.Columns.Add("Engine", typeof(string));
            table.Columns.Add("EngineVersion", typeof(string));
            table.Columns.Add("AutoMinorUpgrade", typeof(string));
            table.Columns.Add("Created", typeof(string));



            //This code ensures we croak if the InstanceID is not unique.  How to catch that?
            // UniqueConstraint makeInstanceIDUnique =
            //   new UniqueConstraint(new DataColumn[] { table.Columns["InstanceID"] });
            // table.Constraints.Add(makeInstanceIDUnique);

            //Can we set the view on this table to expand IEnumerables?
            DataView mydataview = table.DefaultView;




            return table;
        }
        public static DataTable GetEC2DetailsTable()
        {
            DataTable table = new DataTable();
            table.TableName = "EC2Table";
            // Here we create a DataTable .
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("InstanceName", typeof(string));
            table.Columns.Add("InstanceID", typeof(string));
            table.Columns.Add("TerminationProtection", typeof(string));
            table.Columns.Add("AMI", typeof(string));
            table.Columns.Add("AMIState", typeof(string));
            table.Columns.Add("AMIName", typeof(string));
            table.Columns.Add("AMILocation", typeof(string));
            table.Columns.Add("AMIDescription", typeof(string));
            table.Columns.Add("AvailabilityZone", typeof(string));
            table.Columns.Add("Platform", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Events", typeof(string));
            table.Columns.Add("EventList", typeof(string));
            table.Columns.Add("Tags", typeof(string));
            table.Columns.Add("PrivateIP", typeof(string));
            table.Columns.Add("PublicIP", typeof(string));
            table.Columns.Add("PublicDNS", typeof(string));
            table.Columns.Add("VPC", typeof(string));
            table.Columns.Add("SubnetID", typeof(string));
            table.Columns.Add("InstanceState", typeof(string));
            table.Columns.Add("VirtualizationType", typeof(string));
            table.Columns.Add("InstanceType", typeof(string));
            table.Columns.Add("SecurityGroups", typeof(string));
            table.Columns.Add("SGNames", typeof(string));

            //This code ensures we croak if the InstanceID is not unique.  How to catch that?
            UniqueConstraint makeInstanceIDUnique =
                new UniqueConstraint(new DataColumn[] { table.Columns["InstanceID"] });
            table.Constraints.Add(makeInstanceIDUnique);
            //Can we set the view on this table to expand IEnumerables?
            DataView mydataview = table.DefaultView;
            return table;
        }
        public static DataTable GetUsersDetailsTable()
        {
            // Here we create a DataTable .
            DataTable table = new DataTable();

            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("UserID", typeof(string));
            //Information from Credential Report
            table.Columns.Add("Username", typeof(string));//user
            table.Columns.Add("ARN", typeof(string));//arn
            table.Columns.Add("CreateDate", typeof(string));//user_creation_time
            table.Columns.Add("PwdEnabled", typeof(string));//password_enabled
            table.Columns.Add("PwdLastUsed", typeof(string));//password_last_used
            table.Columns.Add("PwdLastChanged", typeof(string));//password_last_changed
            table.Columns.Add("PwdNxtRotation", typeof(string));//password_next_rotation
            table.Columns.Add("MFA Active", typeof(string));//mfa_active

            table.Columns.Add("AccessKey1-Active", typeof(string));//access_key_1_active
            table.Columns.Add("AccessKey1-Rotated", typeof(string));//access_key_1_last_rotated
            table.Columns.Add("AccessKey1-LastUsedDate", typeof(string));//access_key_1_last_used_date
            table.Columns.Add("AccessKey1-LastUsedRegion", typeof(string));//access_key_1_last_used_region
            table.Columns.Add("AccessKey1-LastUsedService", typeof(string));//access_key_1_last_used_service

            table.Columns.Add("AccessKey2-Active", typeof(string));//access_key_2_active
            table.Columns.Add("AccessKey2-Rotated", typeof(string));//access_key_2_last_rotated
            table.Columns.Add("AccessKey2-LastUsedDate", typeof(string));//access_key_2_last_used_date
            table.Columns.Add("AccessKey2-LastUsedRegion", typeof(string));//access_key_2_last_used_region
            table.Columns.Add("AccessKey2-LastUsedService", typeof(string));//access_key_2_last_used_service

            table.Columns.Add("Cert1-Active", typeof(string));//cert_1_active
            table.Columns.Add("Cert1-Rotated", typeof(string));//cert_1_last_rotated
            table.Columns.Add("Cert2-Active", typeof(string));//cert_2_active
            table.Columns.Add("Cert2-Rotated", typeof(string));//cert_2_last_rotated

            table.Columns.Add("User-Policies", typeof(string));
            table.Columns.Add("Access-Keys", typeof(string));
            table.Columns.Add("Groups", typeof(string));
            UniqueConstraint makeUserIDUnique = new UniqueConstraint(new DataColumn[] { table.Columns["UserID"], table.Columns["ARN"] });
            table.Constraints.Add(makeUserIDUnique);
            table.TableName = "IAMTable";
            return table;
        }


        public static DataTable CreateUsersRequestTable()
        {

            // Here we create a DataTable .
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Username", typeof(string));

            //Add, update, or remove password for user.
            ToReturn.Columns.Add("EnablePassword", typeof(Boolean));
            ToReturn.Columns.Add("Password", typeof(string));
            ToReturn.Columns.Add("RemoveExistingPassword", typeof(Boolean));//Would Overwrite first Key

            
            ToReturn.Columns.Add("CreateKey", typeof(Boolean));
            ToReturn.Columns.Add("CreateNewKeyifONEExists", typeof(Boolean));//Would create a second Key
            ToReturn.Columns.Add("OverwriteKeyifONEExists", typeof(Boolean));//Would Overwrite first Key
            ToReturn.Columns.Add("ClearAllExisting", typeof(Boolean));//Remove all keys first.
            //If Two already exist,  return an error. Dont want this causing clutter.

            ToReturn.Columns.Add("Policy", typeof(List<string>));
            ToReturn.Columns.Add("RemoveAllPolicies", typeof(Boolean));//Remove Existing Policies.
            //ReadOnlyAccess
            //AdministratorAccess
            //Nun

            ToReturn.Columns.Add("Group", typeof(List<string>));
            ToReturn.Columns.Add("RemoveAllGroups", typeof(Boolean));//Remove Existing Groups.
            //Get list of Groups on account.




            return ToReturn;
        }


        public static DataTable GetS3DetailsTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("Bucket", typeof(string));
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("RegionEndpoint", typeof(string));
            table.Columns.Add("AuthRegion", typeof(string));
            table.Columns.Add("AuthService", typeof(string));

            table.Columns.Add("CreationDate", typeof(string));
            table.Columns.Add("LastAccess", typeof(string));// This works, but data returned is bogus.
            table.Columns.Add("Owner", typeof(string));
            table.Columns.Add("Grants", typeof(string));

            table.Columns.Add("StartDate", typeof(string));
            table.Columns.Add("StartSizeMin", typeof(string));
            table.Columns.Add("StartSizeAVG", typeof(string));
            table.Columns.Add("StartSizeMax", typeof(string));

            table.Columns.Add("EndDate", typeof(string));
            table.Columns.Add("EndSizeMin", typeof(string));
            table.Columns.Add("EndSizeAVG", typeof(string));
            table.Columns.Add("EndSizeMax", typeof(string));
            table.Columns.Add("EndSizeMaxBytes", typeof(double));


            table.Columns.Add("WebsiteHosting", typeof(string));
            table.Columns.Add("Logging", typeof(string));
            table.Columns.Add("Events", typeof(string));
            table.Columns.Add("Versioning", typeof(string));
            table.Columns.Add("LifeCycle", typeof(string));
            table.Columns.Add("Replication", typeof(string));
            table.Columns.Add("Tags", typeof(string));
            table.Columns.Add("RequesterPays", typeof(string));
            table.TableName = "S3Table";
            return table;
        }
        public static DataTable GetVPCDetailsTable()
        {
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("AccountID", typeof(string));
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));
            ToReturn.Columns.Add("VpcID", typeof(string));
            ToReturn.Columns.Add("CidrBlock", typeof(string));
            ToReturn.Columns.Add("IsDefault", typeof(string));
            ToReturn.Columns.Add("DHCPOptionsID", typeof(string));
            ToReturn.Columns.Add("InstanceTenancy", typeof(string));
            ToReturn.Columns.Add("State", typeof(string));
            ToReturn.Columns.Add("Tags", typeof(string));

            ToReturn.TableName = "VPCTable";

            return ToReturn;
        }

        public static DataTable GetAccountsTable()
        {
            DataTable ToReturn = new DataTable();
            
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("AccountID", typeof(string));
            ToReturn.Columns.Add("Access Key", typeof(string));
            ToReturn.Columns.Add("DupeDisable", typeof(bool));

            ToReturn.TableName = "AccountsTable";

            return ToReturn;
        }

        public static DataTable GetEBSDetailsTable()
        {
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("AccountID", typeof(string));
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));
            ToReturn.Columns.Add("InstanceID", typeof(string));

            ToReturn.Columns.Add("Attachments", typeof(string));
            ToReturn.Columns.Add("AttachState", typeof(string));
            ToReturn.Columns.Add("AttachTime", typeof(string));
            ToReturn.Columns.Add("DeleteonTerm", typeof(string));
            ToReturn.Columns.Add("Device", typeof(string));

            ToReturn.Columns.Add("AZ", typeof(string));
            ToReturn.Columns.Add("CreateTime", typeof(string));
            ToReturn.Columns.Add("Encrypted", typeof(string));
            ToReturn.Columns.Add("IOPS", typeof(int));
            ToReturn.Columns.Add("KMSKeyID", typeof(string));
            ToReturn.Columns.Add("Size-G", typeof(int));
            ToReturn.Columns.Add("SnapshotID", typeof(string));
            ToReturn.Columns.Add("State", typeof(string));
            ToReturn.Columns.Add("Tags", typeof(string));
            ToReturn.Columns.Add("VolumeID", typeof(string));
            ToReturn.Columns.Add("VolumeType", typeof(string));

            ToReturn.TableName = "EBSTable";

            return ToReturn;
        }

        public static DataTable GetSnapshotDetailsTable()
        {
            DataTable ToReturn = new DataTable();

            ToReturn.Columns.Add("AccountID", typeof(string));
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));
            ToReturn.Columns.Add("SnapshotID", typeof(string));
            ToReturn.Columns.Add("Description", typeof(string));
            ToReturn.Columns.Add("VolumeID", typeof(string));
            ToReturn.Columns.Add("VolumeSize-GB", typeof(int));

            ToReturn.Columns.Add("DataEncryptionKeyID", typeof(string));
            ToReturn.Columns.Add("Encrypted", typeof(string));
            ToReturn.Columns.Add("KMSKeyID", typeof(string));
            ToReturn.Columns.Add("OwnerAlias", typeof(string));
            ToReturn.Columns.Add("OwnerID", typeof(string));

            ToReturn.Columns.Add("Progress", typeof(string));
            ToReturn.Columns.Add("StartTime", typeof(string));
            ToReturn.Columns.Add("State", typeof(string));
            ToReturn.Columns.Add("StateMessage", typeof(string));
            ToReturn.Columns.Add("Tags", typeof(string));

            ToReturn.TableName = "SnapshotTable";

            return ToReturn;
        }

        public static DataTable GetS3SizesTable()
        {
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("AccountID", typeof(string));
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));
            ToReturn.Columns.Add("Bucket", typeof(string));

            ToReturn.Columns.Add("StartDate", typeof(string));
            ToReturn.Columns.Add("StartSizeMin", typeof(string));
            ToReturn.Columns.Add("StartSizeAVG", typeof(string));
            ToReturn.Columns.Add("StartSizeMax", typeof(string));

            ToReturn.Columns.Add("EndDate", typeof(string));
            ToReturn.Columns.Add("EndSizeMin", typeof(string));
            ToReturn.Columns.Add("EndSizeAVG", typeof(string));
            ToReturn.Columns.Add("EndSizeMax", typeof(string));
            ToReturn.Columns.Add("EndSizeMaxBytes", typeof(double));



            //ToReturn.Columns.Add("MinItems", typeof(string));
            //ToReturn.Columns.Add("MaxItems", typeof(string));
            ToReturn.TableName = "S3SizesTable";

            return ToReturn;
        }

        public static DataTable GetSubnetDetailsTable()
        {
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("AccountID", typeof(string));//
            ToReturn.Columns.Add("Profile", typeof(string));
            //VPC Details
            ToReturn.Columns.Add("VpcID", typeof(string));
            ToReturn.Columns.Add("VPCName", typeof(string));

            ToReturn.Columns.Add("SubnetID", typeof(string));
            ToReturn.Columns.Add("SubnetName", typeof(string));
            ToReturn.Columns.Add("AvailabilityZone", typeof(string));
            ToReturn.Columns.Add("Cidr", typeof(string));
            ToReturn.Columns.Add("AvailableIPCount", typeof(string));

            //These columns are derived data, not actually pulled from AWS.
            ToReturn.Columns.Add("_Network", typeof(string));
            ToReturn.Columns.Add("_Netmask", typeof(string));
            ToReturn.Columns.Add("_Broadcast", typeof(string));
            ToReturn.Columns.Add("_FirstUsable", typeof(string));
            ToReturn.Columns.Add("_LastUsable", typeof(string));




            ToReturn.Columns.Add("DefaultForAZ", typeof(string));
            ToReturn.Columns.Add("MapPubIPonLaunch", typeof(string));
            ToReturn.Columns.Add("State", typeof(string));
            ToReturn.Columns.Add("Tags", typeof(string));
            ToReturn.TableName = "SubnetsTable";
            return ToReturn;
        }

        public static DataTable GetCertDetailsTable()
        {
            DataTable ToReturn = new DataTable();
            ToReturn.Columns.Add("AccountID", typeof(string));//
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("CertName", typeof(string));

            //Cert Details
            ToReturn.Columns.Add("Status", typeof(string));
            ToReturn.Columns.Add("InUse", typeof(string));
            ToReturn.Columns.Add("DomainName", typeof(string));
            ToReturn.Columns.Add("AdditionalNames", typeof(string));
            ToReturn.Columns.Add("Identifier", typeof(string));
            ToReturn.Columns.Add("SerialNumber", typeof(string));
            ToReturn.Columns.Add("AssociatedResources", typeof(string));
            ToReturn.Columns.Add("RequestedAt", typeof(string));
            ToReturn.Columns.Add("IssuedAt", typeof(string));
            ToReturn.Columns.Add("NotBefore", typeof(string));
            ToReturn.Columns.Add("NotAfter", typeof(string));
            ToReturn.Columns.Add("PublicKeyInfo", typeof(string));
            ToReturn.Columns.Add("SignatureAlgorithm", typeof(string));
            ToReturn.Columns.Add("ARN", typeof(string));


            ToReturn.TableName = "CertsTable";
            return ToReturn;
        }

        public static DataTable GetSNSSubsTable()
        {
            DataTable ToReturn = new DataTable();

            ToReturn.Columns.Add("AccountID", typeof(string));//
            ToReturn.Columns.Add("Profile", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));

            ToReturn.Columns.Add("Endpoint", typeof(string));
            ToReturn.Columns.Add("Owner", typeof(string));
            ToReturn.Columns.Add("Protocol", typeof(string));
            ToReturn.Columns.Add("SubscriptionARN", typeof(string));
            ToReturn.Columns.Add("TopicName", typeof(string));
            ToReturn.Columns.Add("TopicARN", typeof(string));
            ToReturn.Columns.Add("CrossAccount", typeof(string));


            ToReturn.TableName = "SNSSubscriptionTable";
            return ToReturn;
        }

        public static DataTable GetDNSDetailTable()
        {
            DataTable ToReturn = new DataTable();

            ToReturn.Columns.Add("AccountID", typeof(string));//
            ToReturn.Columns.Add("Profile", typeof(string));


            ToReturn.Columns.Add("Name", typeof(string));
            ToReturn.Columns.Add("Type", typeof(string));
            ToReturn.Columns.Add("Value", typeof(string));
            ToReturn.Columns.Add("EvalTargetHealth", typeof(string));
            ToReturn.Columns.Add("HealthCheckID", typeof(string));
            ToReturn.Columns.Add("TTL", typeof(string));
            ToReturn.Columns.Add("Region", typeof(string));
            ToReturn.Columns.Add("Weight", typeof(string));
            ToReturn.Columns.Add("Geolocation", typeof(string));
            ToReturn.Columns.Add("SetID", typeof(string));


            ToReturn.TableName = "DNSTable";
            return ToReturn;
        }

        public static string Shrug = "¯\\_(ツ)_/¯";
    }


    public class ScannerSettings
    {
        ScanAWS stivawslib = new ScanAWS();

        public int ReScanTimerinMinutes { get; set; } = 20;
        private Dictionary<string, Dictionary<string, bool>> Columnsettings = new Dictionary<string, Dictionary<string, bool>>();
        private Dictionary<string, List<string>> DefaultColumns = new Dictionary<string, List<string>>();

        public void Initialize()
        {

            foreach (string aregion in stivawslib.GetRegionNames())
            {
                try { ScannableRegions.Add(aregion, true); }
                catch { }
            }
            foreach (string aprofile in stivawslib.GetProfileNames())
            {
                try
                {
                    string accountid = stivawslib.GetAccountID(aprofile);

                    if (accountid.StartsWith("Error"))
                    {
                        BadProfiles.Add(aprofile, accountid);
                    }
                    else
                    {
                        ScannableProfiles.Add(aprofile, true);
                        ProfileAccountMappings.Add(aprofile, accountid);
                    }

                }
                catch (Exception ex)
                {
                    BadProfiles.Add(aprofile, ex.Message);
                }
            }
            setdefaultcols();


            //Build the initial column settings.
            foreach (var acomp in Components.Keys)
            {
                Dictionary<string, bool> compdict = new Dictionary<string, bool>();
                foreach (DataColumn acol in AWSTables.GetComponentTable(acomp).Columns)
                {
                    bool isvis = false;
                    if (DefaultColumns[acomp].Contains(acol.ColumnName)) isvis = true;
                    if (isvis) compdict.Add(acol.ColumnName, true);
                    else { compdict.Add(acol.ColumnName, false); }
                }
                Columnsettings.Add(acomp, compdict);
            }


            return;
        }


        private void setdefaultcols()
        {
            //Set the columns we want visible by default per component.
            DefaultColumns["EBS"] = new List<string>() { "Profile", "Region", "InstanceID", "Attachments", "AttachState", "AttachTime", "DeleteonTerm", "AZ", "CreateTime", "IOPS", "Size-G", "State", "Tags", "VolumeID", "VolumeType" };
            DefaultColumns["EC2"] = new List<string>() { "Profile", "Region", "InstanceName","InstanceID", "AMIDescription", "AvailabilityZone", "Platform", "Status", "Tags", "PrivateIP", "PublicIP", "PublicDNS", "SecurityGroups", "SGNames" };
            DefaultColumns["IAM"] = new List<string>() { "Profile", "UserID", "Username", "ARN", "CreateDate", "PwdEnabled", "PwdLastUsed", "MFA Active", "AccessKey1-Active", "AccessKey1-LastUsedDate", "AccessKey1-LastUsedRegion", "AccessKey1-LastUsedService", "User-Policies", "Access-Keys", "Groups" };
            DefaultColumns["S3"] = new List<string>() { "Profile", "Bucket", "Region", "RegionEndpoint", "AuthRegion", "AuthService", "CreationDate", "LastAccess", "Owner", "EndDate", "EndSizeMax", "EndSizeMaxBytes","Grants", "WebsiteHosting", "Versioning", "Tags" };
            DefaultColumns["RDS"] = new List<string>() { "Profile", "AvailabilityZone", "InstanceID", "Name", "Status", "EndPoint", "InstanceClass", "IOPS", "AllocatedStorage", "StorageType", "Engine", "EngineVersion", "AutoMinorUpgrade", "Created" };
            DefaultColumns["VPC"] = new List<string>() {  "Profile", "VpcID", "CidrBlock", "IsDefault", "DHCPOptionsID", "InstanceTenancy", "State", "Tags" }; 
            DefaultColumns["Snapshots"] = new List<string>() { "Profile", "Region", "SnapshotID", "Description", "VolumeID", "VolumeSize-GB", "Encrypted", "OwnerID", "Progress", "StartTime", "State", "Tags" };
            DefaultColumns["Subnets"] = new List<string>() {  "Profile", "VpcID", "VPCName", "SubnetID", "SubnetName", "AvailabilityZone", "Cidr", "AvailableIPCount", "=Network", "=Netmask", "=Broadcast", "=FirstUsable", "=LastUsable", "DefaultForAZ", "MapPubIPonLaunch", "State", "Tags" };
            DefaultColumns["SNSSubs"] = new List<string>() {  "Profile", "Endpoint", "Protocol","TopicName", "TopicARN" , "CrossAccount" };
            DefaultColumns["ELB"] = new List<string>() { "Profile", "Name", "Region" , "Instances" , "Listeners", "HealthCheck", "Status"};
            DefaultColumns["DNS"] = new List<string>() { "Profile", "Name", "Type", "Value", "TTL" };
        }
    
        /// <summary>
        /// A list of the components we can scan, along with a setting on to whether we WANT them scanned.
        /// </summary>
        public Dictionary<string, bool> Components { get; set; } = new Dictionary<string, bool>()
        {
            {"EBS",true },
            {"EC2",true },
            {"IAM",true },
            {"S3",true },
            {"RDS",true },
            {"VPC",true },
            {"Snapshots",true },
            {"SNSSubs",true },
            {"Subnets",true},
            {"ELB",true},
            {"DNS",true}
        };

        public DateTime ScanStart = DateTime.Now;

        public DateTime ScanDone = DateTime.Now;
        /// <summary>
        /// Sets a flag to indicate whether a component should be scanned or no.
        /// </summary>
        /// <param name="Comp">The name of the component</param>
        /// <param name="status">Boolean true for scan false to not scan</param>
        public void SetComponentsScan(string Comp, bool status)
        {
            Components[Comp] = status;
        }


        public List<String> GetComponents2Scan()
        {
            List<string> ToReturn = new List<string>();
            foreach (KeyValuePair<string, bool> KVP in Components)
            {
                if (KVP.Value) ToReturn.Add(KVP.Key);
            }
            return ToReturn;
        }

        public String State { get; set; } = "Idle";


        #region Dictionaries
        public Dictionary<string, string> EC2Status = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> ELBStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "ELBs","" }
        };

        public Dictionary<string, string> EBSStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Volumes","" }
        };

        public Dictionary<string, string> S3Status = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> SnapshotsStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> VPCStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> IAMStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> SubnetsStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> RDSStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };

        public Dictionary<string, string> DNSStatus = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Records","" }
        };

        public Dictionary<string, string> SNSSubs = new Dictionary<string, string>
        {
            { "Status","Idle" },
            { "StartTime","" },
            { "EndTime","" },
            { "Result","" },
            { "Instances","" }
        };
        #endregion
        public string GetTime()
        {
            string CurrentTime = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
            return CurrentTime;
        }

        public Dictionary<string, string> ProfileAccountMappings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> ScannableRegions { get; set; } = new Dictionary<string, bool>();

        public void setRegionEnabled(string region, Boolean state)
        {
            ScannableRegions[region] = state;
        }

        /// <summary>
        /// A list containing only the names of Profiles with the Scan bit set.
        /// </summary>
        /// <returns></returns>
        public List<String> GetActiveProfiles()
        {
            List<string> ToReturn = new List<string>();
            foreach (var KVP in ScannableProfiles)
            {
                if (KVP.Value) ToReturn.Add(KVP.Key);
            }
            return ToReturn;
        }

        public string RemoveBadProfilesfromStore()
        {
            string ToReturn = "";

            var badones = BadProfiles.Keys.ToList<string>();
            foreach (var naughty in badones)
            {
                try
                {
                    if (ToReturn.Length > 2) ToReturn += "\n";
                    ToReturn += RemoveProfilefromStore(naughty);
                    BadProfiles.Remove(naughty);

                }
                catch (Exception ex)
                {
                    ToReturn += "Failed to remove " + naughty + ": " + ex.Message;
                }
            }
            return ToReturn;
        }

        public string RemoveProfilefromStore(string aprofile)
        {
            string ToReturn = "";
            try
            {
                Amazon.Util.ProfileManager.UnregisterProfile(aprofile);
                ToReturn += "Removed " + aprofile;
            }
            catch (Exception ex)
            {
                ToReturn += "Failed to remove " + aprofile + ": " + ex.Message;
            }

            return ToReturn;
        }


        public List<String> GetBadProfiles()
        {
            List<String> ToReturn = new List<string>();

            foreach (var KVP in ScannableProfiles)
            {
                if (KVP.Value) ToReturn.Add(KVP.Key + ": " + KVP.Value);
            }
            return ToReturn;
        }

        /// <summary>
        /// A Dictionary of Profile names with a boolean indicating whether they are to be scanned.
        /// </summary>
        public Dictionary<string, bool> ScannableProfiles { get; set; } = new Dictionary<string, bool>();

        public Dictionary<string, string> BadProfiles { get; set; } = new Dictionary<string, string>();
        public void setProfileEnabled(string profile, Boolean state)
        {
            ScannableProfiles[profile] = state;
        }

        public List<string> GetEnabledProfiles
        {
            get
            {
                List<string> ToReturn = new List<string>();
                foreach (var profiles in ScannableProfiles)
                {
                    if (profiles.Value) ToReturn.Add(profiles.Key);
                }
                return ToReturn;
            }
        }

        public List<KeyValuePair<string, string>> GetEnabledProfileandRegions
        {
            get
            {
                List<KeyValuePair<string, string>> ToReturn = new List<KeyValuePair<string, string>>();
                foreach (var aprofile in ScannableProfiles)
                {
                    if (aprofile.Value)
                    {
                        foreach (var region in ScannableRegions)
                        {
                            if (region.Value)
                            {
                                var KVP = new KeyValuePair<string, string>(aprofile.Key, region.Key);
                                ToReturn.Add(KVP);

                            }
                        }
                    }
                }

                return ToReturn;
            }
        }


        /// <summary>
        /// This is where I define the default columns I want to be visible.
        /// </summary>
        /// <param name="component">The name of the Amazon component we want columns for.</param>
        /// <returns></returns>
        public Dictionary<string, bool> GetColumnSettings(string component)
        {
            Dictionary<string, bool> ToReturn = Columnsettings[component];
            return ToReturn;
        }

        public void setcolumnvisibility(string component, string column, bool isvisible)
        {
            Columnsettings[component][column] = isvisible;
        }

    }//End of settings



    public class MySQLDBHelper
    {

/*
* ----------------------------------------------------------------------------
* "THE BEER-WARE LICENSE" (Revision 42):
* <fg@code-works.de> wrote this class. As long as you retain this notice you
* can do whatever you want with this stuff. If we meet some day, and you think
* this stuff is worth it, you can buy me a beer in return. Frank Gehann
* ----------------------------------------------------------------------------
*/

        /// <summary>
        /// Creates a multivalue insert for MySQL from a given DataTable
        /// </summary>
        /// <param name="table">reference to the Datatable we're building our String on</param>
        /// <param name="table_name">name of the table the insert is created for</param>
        /// <returns>Multivalue insert String</returns>
        public static string MySQLBulkInsert(ref DataTable table, String table_name)
        {


            try
            {
                StringBuilder queryBuilder = new StringBuilder();
                DateTime dt;

                queryBuilder.AppendFormat("INSERT INTO `{0}` (", table_name);

                // more than 1 column required and 1 or more rows
                if (table.Columns.Count > 1 && table.Rows.Count > 0)
                {
                    // build all columns
                    queryBuilder.AppendFormat("`{0}`", table.Columns[0].ColumnName);

                    if (table.Columns.Count > 1)
                    {
                        for (int i = 1; i < table.Columns.Count; i++)
                        {
                            queryBuilder.AppendFormat(", `{0}` ", table.Columns[i].ColumnName);
                        }
                    }

                    queryBuilder.AppendFormat(") VALUES (", table_name);

                    // build all values for the first row
                    // escape String & Datetime values!
                    if (table.Columns[0].DataType == typeof(String))
                    {
                        queryBuilder.AppendFormat("'{0}'", MySqlHelper.EscapeString(table.Rows[0][table.Columns[0].ColumnName].ToString()));
                    }
                    else if (table.Columns[0].DataType == typeof(DateTime))
                    {
                        dt = (DateTime)table.Rows[0][table.Columns[0].ColumnName];
                        queryBuilder.AppendFormat("'{0}'", dt.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else if (table.Columns[0].DataType == typeof(Int32))
                    {
                        queryBuilder.AppendFormat("{0}", table.Rows[0].Field<Int32?>(table.Columns[0].ColumnName) ?? 0);
                    }
                    else
                    {
                        queryBuilder.AppendFormat(", {0}", table.Rows[0][table.Columns[0].ColumnName].ToString());
                    }

                    for (int i = 1; i < table.Columns.Count; i++)
                    {
                        // escape String & Datetime values!
                        if (table.Columns[i].DataType == typeof(String))
                        {
                            queryBuilder.AppendFormat(", '{0}'", MySqlHelper.EscapeString(table.Rows[0][table.Columns[i].ColumnName].ToString()));
                        }
                        else if (table.Columns[i].DataType == typeof(DateTime))
                        {
                            dt = (DateTime)table.Rows[0][table.Columns[i].ColumnName];
                            queryBuilder.AppendFormat(", '{0}'", dt.ToString("yyyy-MM-dd HH:mm:ss"));

                        }
                        else if (table.Columns[i].DataType == typeof(Int32))
                        {
                            queryBuilder.AppendFormat(", {0}", table.Rows[0].Field<Int32?>(table.Columns[i].ColumnName) ?? 0);
                        }
                        else
                        {
                            queryBuilder.AppendFormat(", {0}", table.Rows[0][table.Columns[i].ColumnName].ToString());
                        }
                    }

                    queryBuilder.Append(")");
                    queryBuilder.AppendLine();

                    // build all values all remaining rows
                    if (table.Rows.Count > 1)
                    {
                        // iterate over the rows
                        for (int row = 1; row < table.Rows.Count; row++)
                        {
                            // open value block
                            queryBuilder.Append(", (");

                            // escape String & Datetime values!
                            if (table.Columns[0].DataType == typeof(String))
                            {
                                queryBuilder.AppendFormat("'{0}'", MySqlHelper.EscapeString(table.Rows[row][table.Columns[0].ColumnName].ToString()));
                            }
                            else if (table.Columns[0].DataType == typeof(DateTime))
                            {
                                dt = (DateTime)table.Rows[row][table.Columns[0].ColumnName];
                                queryBuilder.AppendFormat("'{0}'", dt.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            else if (table.Columns[0].DataType == typeof(Int32))
                            {
                                queryBuilder.AppendFormat("{0}", table.Rows[row].Field<Int32?>(table.Columns[0].ColumnName) ?? 0);
                            }
                            else
                            {
                                queryBuilder.AppendFormat(", {0}", table.Rows[row][table.Columns[0].ColumnName].ToString());
                            }

                            for (int col = 1; col < table.Columns.Count; col++)
                            {
                                // escape String & Datetime values!
                                if (table.Columns[col].DataType == typeof(String))
                                {
                                    queryBuilder.AppendFormat(", '{0}'", MySqlHelper.EscapeString(table.Rows[row][table.Columns[col].ColumnName].ToString()));
                                }
                                else if (table.Columns[col].DataType == typeof(DateTime))
                                {
                                    dt = (DateTime)table.Rows[row][table.Columns[col].ColumnName];
                                    queryBuilder.AppendFormat(", '{0}'", dt.ToString("yyyy-MM-dd HH:mm:ss"));
                                }
                                else if (table.Columns[col].DataType == typeof(Int32))
                                {
                                    queryBuilder.AppendFormat(", {0}", table.Rows[row].Field<Int32?>(table.Columns[col].ColumnName) ?? 0);
                                }
                                else
                                {
                                    queryBuilder.AppendFormat(", {0}", table.Rows[row][table.Columns[col].ColumnName].ToString());
                                }
                            } // end for (int i = 1; i < table.Columns.Count; i++)

                            // close value block
                            queryBuilder.Append(")");
                            queryBuilder.AppendLine();

                        } // end for (int r = 1; r < table.Rows.Count; r++)

                        // sql delimiter =)
                        queryBuilder.Append(";");

                    } // end if (table.Rows.Count > 1)

                    return queryBuilder.ToString();
                }
                else
                {
                    return "";
                } // end if(table.Columns.Count > 1 && table.Rows.Count > 0)
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}//End AWSFunctions




//Da end







