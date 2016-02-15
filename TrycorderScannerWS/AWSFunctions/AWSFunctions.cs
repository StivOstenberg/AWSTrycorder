using Amazon;

using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;


using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace AWSFunctions
{
    
    /// <summary>
    /// Given credentials and arguments, retrieve data from AWS.
    /// </summary>
    public class ScanAWS
    {
        
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
                RegionNames.Add(EP.DisplayName);
            }
            return RegionNames;

        }


        public Dictionary<string, Dictionary<string, string>> GetIAMUsers(string aprofile)
        {
            Dictionary<string, Dictionary<string, string>> ToReturn = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> UserNameIdMap = new Dictionary<string, string>();
            Amazon.Runtime.AWSCredentials credential;
            string accountid = "";
            try { 

                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            var iam = new AmazonIdentityManagementServiceClient(credential);


                var myUserList = iam.ListUsers().Users;

                try
                {
                    accountid = myUserList[0].Arn.Split(':')[4];//Get the ARN and extract the AccountID ID
                }
                catch
                {
                    accountid = "?";
                }
                var createcredreport = iam.GenerateCredentialReport();
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
                    var arow = myStringRow.Split(",".ToCharArray()[0]);

                        //Letsa dumpa da data...
                    
                        mydata.Add("AccountID", accountid);
                        mydata.Add("Profile", aprofile);
                        string userID = UserNameIdMap[arow[0]];
                        mydata.Add("UserID", userID);
                        for (int x = 0; x < headers.Length; x++)
                        {
                            mydata.Add(headers[x], arow[x]);
                        }

                        ToReturn.Add(userID, mydata);

                    myStringRow = sr.ReadLine();
                }
                sr.Close();
                sr.Dispose();



            }
            catch (Exception ex)
            {
                string test = "";
                //Deal with this later if necessary.
            }

               //Done stream, now to fill in the blanks...


        }
            catch//The final catch
            {

            }
            return ToReturn;
        }//EndIamUserScan

        /// <summary>
        /// Given a profile and user, collect additional information.
        /// </summary>
        /// <param name="aprofile">An AWS Profile name stored in Windows Credential Store</param>
        /// <param name="auser">The Name of a User</param>
        /// <returns>Dictionary containing keys for each type of data[AcessKeys], [Groups], [Policies]</returns>
            public Dictionary<string,string> GetUserDetails(string aprofile, string auser)
        {
            Dictionary<string, string> ToReturn = new Dictionary<string, string>();




            return ToReturn;
        }


        public DataTable GetEC2Instances(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetEC2DetailsTable();
            

            Amazon.Runtime.AWSCredentials credential;
            string accountid = "";
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;
            Dictionary<string, Dictionary<String,String>> OldReturn = new Dictionary<string, Dictionary<String,String>>();
            credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            
            
            //Convert the Region2Scan to an AWS Endpoint.
            foreach(var aregion in RegionEndpoint.EnumerableAllRegions)
            {
                if(aregion.DisplayName.Equals(Region2Scan))
                {
                    Endpoint2scan = aregion;
                    continue;
                }
            }

            //var ec2 = AWSClientFactory.CreateAmazonEC2Client(credential, Endpoint2scan);
            var ec2 = new Amazon.EC2.AmazonEC2Client(credential);
            //These steps just get the account ID
            var iam = new AmazonIdentityManagementServiceClient(credential);
            var myUserList = iam.ListUsers().Users;
            try
            {
                accountid = myUserList[0].Arn.Split(':')[4];//Get the ARN and extract the AccountID ID
            }
            catch
            {
                accountid = "?";
            }

            var request = new DescribeInstanceStatusRequest();
            request.IncludeAllInstances = true;
            var instatresponse = ec2.DescribeInstanceStatus(request);
            var indatarequest = new DescribeInstancesRequest();


            //Get a list of the InstanceIDs.
            foreach (var instat in instatresponse.InstanceStatuses)
            {
                indatarequest.InstanceIds.Add(instat.InstanceId);
                indatarequest.InstanceIds.Sort();
            }


            //DescribeInstancesResult DescResult = ec2.DescribeInstances(indatarequest);
            
            DescribeInstancesResponse DescResult = ec2.DescribeInstances();

            int count = instatresponse.InstanceStatuses.Count();

            //Build data dictionary of instances

            Dictionary<String, Instance> Bunchadata = new Dictionary<string, Instance>();
            foreach (var urtburgle in DescResult.Reservations)
            {
                foreach (var instancedata in urtburgle.Instances)
                {
                    if (instancedata.InstanceId.ToString().Equals("i-3448ade7"))//This if is for debugging purposes.
                    {
                        var checkker = instancedata;
                    }
                    Bunchadata.Add(instancedata.InstanceId, instancedata);
                }
            }



            //Go through list of instances...
            foreach (var instat in instatresponse.InstanceStatuses)
            {
                string instanceid = instat.InstanceId;
                Instance thisinstance = Bunchadata[instanceid];
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

                var myinstance = new  Reservation();

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
                foreach(var atag in loadtags)
                {
                    tags.Add(atag.Key + ": " + atag.Value);
                    if (atag.Key.Equals( "Name")) instancename = atag.Value;
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
                        eventlist.Add(anevent.Description);
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
                string AMIDesc = "";
                try { AMI = thisinstance.ImageId; }
                catch { }
                if (string.IsNullOrEmpty(AMI))  AMI = "";
                else
                {
                    DescribeImagesRequest DIR = new DescribeImagesRequest();
                    DIR.ImageIds.Add(AMI);
                    var imresp = ec2.DescribeImages(DIR);
                    var idata = imresp.Images;
                    if (idata.Count > 0)
                    {
                        AMIDesc = idata[0].Description;
                    }
                    if (String.IsNullOrEmpty(AMIDesc)) AMIDesc = "AMI Image not accessible!!";
                }

                //
                var SGs = thisinstance.SecurityGroups;
                List<string> SGids = new List<string>();
                List<String> SGNames = new List<string>();
                foreach(var wabbit in SGs)
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
                thisinstancedatarow["Profile"]= profile ;
                thisinstancedatarow["Region"] = myregion ;
                 thisinstancedatarow["InstanceName"] = instancename;
                 thisinstancedatarow["InstanceID"] = instanceid;
                 thisinstancedatarow["AMI"] = AMI;
                 thisinstancedatarow["AMIDescription"] = AMIDesc;
                 thisinstancedatarow["AvailabilityZone"] = AZ;
                 thisinstancedatarow["Status"] = status;
                 thisinstancedatarow["Events"] = eventnumber.ToString();
                 thisinstancedatarow["EventList"] = eventlist;
                 thisinstancedatarow["Tags"] = tags;
                 thisinstancedatarow["PrivateIP"] = Priv_IP;
                 thisinstancedatarow["PublicIP"] = publicIP;
                 thisinstancedatarow["PublicDNS"] = publicDNS;
                 thisinstancedatarow["PublicDNS"] = publicDNS;
                 thisinstancedatarow["VPC"] = myvpcid;
                 thisinstancedatarow["SubnetID"] = mysubnetid;
                 thisinstancedatarow["InstanceState"] = istate.Value;
                 thisinstancedatarow["VirtualizationType"] = ivirtType;
                 thisinstancedatarow["InstanceType"] = instancetype;
                 thisinstancedatarow["SecurityGroups"] = SGids;
                 thisinstancedatarow["SGNames"] = SGNames;
                //Add this instance to the data returned.
                ToReturn.Rows.Add(thisinstancedatarow);


            }//End for of instances



            return ToReturn;
        }//EndGetEC2

    }//EndScanAWS

    public class AWSTables
    {
        public static DataTable GetEC2DetailsTable()
        {
            DataTable table = new DataTable();
            // Here we create a DataTable .
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("InstanceName", typeof(string));
            table.Columns.Add("InstanceID", typeof(string));
            table.Columns.Add("AMI", typeof(string));
            table.Columns.Add("AMIDescription", typeof(string));
            table.Columns.Add("AvailabilityZone", typeof(string));
            table.Columns.Add("Platform", typeof(string));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Events", typeof(string));
            table.Columns.Add("EventList", typeof(List<string>));
            table.Columns.Add("Tags", typeof(string));
            table.Columns.Add("PrivateIP", typeof(string));
            table.Columns.Add("PublicIP", typeof(string));
            table.Columns.Add("PublicDNS", typeof(string));
            table.Columns.Add("VPC", typeof(string));
            table.Columns.Add("SubnetID", typeof(string));
            table.Columns.Add("InstanceState", typeof(string));
            table.Columns.Add("VirtualizationType", typeof(string));
            table.Columns.Add("InstanceType", typeof(string));
            table.Columns.Add("SecurityGroups", typeof(List<string>));
            table.Columns.Add("SGNames", typeof(List<string>));
            
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

            UniqueConstraint makeUserIDUnique =
                 new UniqueConstraint(new DataColumn[] { table.Columns["UserID"] ,
                                                          table.Columns["ARN"]}); 
                 table.Constraints.Add(makeUserIDUnique);

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


            return table;
        }
        public static DataTable GetS3DetailsTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("AccountID", typeof(string));
            table.Columns.Add("Profile", typeof(string));
            table.Columns.Add("Bucket", typeof(string));
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("CreationDate", typeof(string));
            table.Columns.Add("LastAccess", typeof(string));// This works, but data returned is bogus.
            table.Columns.Add("Owner", typeof(string));
            table.Columns.Add("Grants", typeof(string));


            table.Columns.Add("WebsiteHosting", typeof(string));
            table.Columns.Add("Logging", typeof(string));
            table.Columns.Add("Events", typeof(string));
            table.Columns.Add("Versioning", typeof(string));
            table.Columns.Add("LifeCycle", typeof(string));
            table.Columns.Add("Replication", typeof(string));
            table.Columns.Add("Tags", typeof(string));
            table.Columns.Add("RequesterPays", typeof(string));

            return table;
        }

    }
}//End AWSFunctions

        //Da end
  

       

 


