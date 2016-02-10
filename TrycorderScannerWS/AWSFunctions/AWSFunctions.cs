using Amazon;

using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;


using System;
using System.Collections.Generic;
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


        public Dictionary<string, object> GetEC2Instances(string aprofile, string Region2Scan)
        {
            
            Amazon.Runtime.AWSCredentials credential;
            string accountid = "";
            Dictionary<string, object> ToReturn = new Dictionary<string, object>();
            credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            //Need to convert Region2Scan to endpoint
           
            var ec2 = AWSClientFactory.CreateAmazonEC2Client(credential, RegionEndpoint.USEast1);


            var request = new DescribeInstanceStatusRequest();
            request.IncludeAllInstances = true;
            var instatresponse = ec2.DescribeInstanceStatus(request);
            var indatarequest = new DescribeInstancesRequest();

            foreach (var instat in instatresponse.InstanceStatuses)
            {

                indatarequest.InstanceIds.Add(instat.InstanceId);
                indatarequest.InstanceIds.Sort();
            }


            //DescribeInstancesResult DescResult = ec2.DescribeInstances(indatarequest);
            
            DescribeInstancesResult DescResult = ec2.DescribeInstances();

            int count = instatresponse.InstanceStatuses.Count();
            int itindex = -1;
            foreach (var instat in instatresponse.InstanceStatuses)
            {
                itindex++;
                //Collect the datases
                string instanceid = instat.InstanceId;
                string instancename = "";


                var status = instat.Status.Status;
                string AZ = instat.AvailabilityZone;
                var istate = instat.InstanceState.Name;

                string profile = aprofile;
                string myregion = Region2Scan;
                int eventnumber = instat.Events.Count();

                string eventlist = "";
                var reservations = DescResult.Reservations;

                var myinstance = new  Reservation();
                if (instanceid.Contains("i-a8535657"))//Troubleshooting....
                {
                    var truebert = false;
                }
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



                string tags = ""; // Holds the list of tags to print out.

                var loadtags = (from t in DescResult.Reservations
                                where t.Instances[0].InstanceId.Equals(instanceid)
                                select t.Instances[0].Tags).AsEnumerable();

                Dictionary<string, string> taglist = new Dictionary<string, string>();
                foreach (var rekey in loadtags)
                {
                    foreach (var kvp in rekey)
                    {
                        taglist.Add(kvp.Key, kvp.Value);
                    }
                }

            

                if (eventnumber > 0)
                {
                    foreach (var anevent in instat.Events)
                    {
                        eventlist += anevent.Description + "\n";
                    }
                }


                var platform = (from t in reservations
                                where t.Instances[0].InstanceId.Equals(instanceid)
                                select t.Instances[0].Platform).FirstOrDefault();
                if (String.IsNullOrEmpty(platform)) platform = "Linux";



                var Priv_IP = (from t in DescResult.Reservations
                               where t.Instances[0].InstanceId.Equals(instanceid)
                               select t.Instances[0].PrivateIpAddress).FirstOrDefault();

                var disInstance = (from t in reservations
                                   where t.Instances[0].InstanceId.Equals(instanceid)
                                   select t).FirstOrDefault();

                if (String.IsNullOrEmpty(Priv_IP))
                {
                    Priv_IP = "?";
                }

                var publicIP = (from t in reservations
                                where t.Instances[0].InstanceId.Equals(instanceid)
                                select t.Instances[0].PublicIpAddress).FirstOrDefault();
                if (String.IsNullOrEmpty(publicIP)) publicIP = "";

                var publicDNS = (from t in reservations
                                 where t.Instances[0].InstanceId.Equals(instanceid)
                                 select t.Instances[0].PublicDnsName).FirstOrDefault();
                if (String.IsNullOrEmpty(publicDNS)) publicDNS = "";




                //Virtualization type (HVM, Paravirtual)
                var ivirtType = (from t in reservations
                                 where t.Instances[0].InstanceId.Equals(instanceid)
                                 select t.Instances[0].VirtualizationType).FirstOrDefault();
                if (String.IsNullOrEmpty(ivirtType)) ivirtType = "?";

                // InstanceType (m3/Large etc)
                var instancetype = (from t in reservations
                                    where t.Instances[0].InstanceId.Equals(instanceid)
                                    select t.Instances[0].InstanceType).FirstOrDefault();
                if (String.IsNullOrEmpty(instancetype)) instancetype = "?";


                //Test section to try to pull out AMI data
                string AMIDesc = "";
                var AMI = (from t in reservations
                           where t.Instances[0].InstanceId.Equals(instanceid)
                           select t.Instances[0].ImageId).FirstOrDefault();
                if (string.IsNullOrEmpty(AMI))
                {
                    AMI = "";
                }
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
                var SGs = (from t in reservations
                           where t.Instances[0].InstanceId.Equals(instanceid)
                           select t.Instances[0].SecurityGroups);

                string sglist = "";


                if (SGs.Count() > 0)
                {
                    foreach (var ansg in SGs.FirstOrDefault())
                    {
                        if (sglist.Length > 2) { sglist += "\n"; }
                        sglist += ansg.GroupName;
                    }
                }
                else
                {
                    sglist = "_NONE!_";
                }
                //Add to table
                if (String.IsNullOrEmpty(sglist)) sglist = "NullOrEmpty";

                if (String.IsNullOrEmpty(instancename)) instancename = "";
                string rabbit = accountid + profile + myregion + instancename + instanceid + AZ + status + eventnumber + eventlist + tags + Priv_IP + publicIP + publicDNS + istate + ivirtType + instancetype + sglist;

                if (instancename.Contains("p1-job"))
                {
                    string yup = "y";
                }


                //EC2DetailsTable.Rows.Add(accountid, profile, myregion, instancename, instanceid, AMI, AMIDesc, AZ, platform, status, eventnumber, eventlist, tags, Priv_IP, publicIP, publicDNS, istate, ivirtType, instancetype, sglist);


            }



            return ToReturn;
        }//EndGetEC2

    }//EndScanAWS
}//End AWSFunctions

        //Da end
  

       

 


