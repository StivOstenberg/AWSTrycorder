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
        uint startIP, endIP;
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


        public DataTable GetSubnets (string aprofile, string Region2Scan)
        {
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
            DataTable ToReturn = AWSTables.GetSubnetDetailsTable();

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential,Endpoint2scan);
                var subbies = ec2.DescribeSubnets().Subnets;
                
                foreach(var asubnet in subbies)
                {
                    DataRow disone = ToReturn.NewRow();
                    disone["Profile"] = aprofile;
                    disone["AvailabilityZone"] = asubnet.AvailabilityZone;
                    disone["AvailableIPCount"] = asubnet.AvailableIpAddressCount.ToString();
                    disone["Cidr"] = asubnet.CidrBlock;
                    //Trickybits.  Cidr to IP
                    var dater = Network2IpRange(asubnet.CidrBlock);


                    ///
                    disone["DefaultForAZ"] = asubnet.DefaultForAz.ToString();
                    disone["MapPubIPonLaunch"] = asubnet.MapPublicIpOnLaunch.ToString();
                    disone["State"] = asubnet.State;
                    disone["SubnetID"] = asubnet.SubnetId;
                    var tagger = asubnet.Tags;
                    List<string> taglist = new List<string>();
                    foreach(var atag in tagger)
                    {
                        taglist.Add(atag.Key + ": " + atag.Value);
                        if(atag.Key.Equals("Name")) disone["SubnetName"] = atag.Value;
                    }

                    disone["Tags"] = List2String(taglist);
                    disone["VpcID"] = asubnet.VpcId;

                    ToReturn.Rows.Add(disone);
                }


            }
            catch(Exception ex)
                {
                string rabbit = "";
            }






            return ToReturn;
        }

        public DataTable GetS3Buckets(string aprofile)
        {
            Amazon.Runtime.AWSCredentials credential;
            DataTable ToReturn = AWSTables.GetS3DetailsTable();
            string accountid = "";

            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                
                AmazonS3Client S3Client = new AmazonS3Client(credential, Amazon.RegionEndpoint.USEast1);
                ListBucketsResponse response = S3Client.ListBuckets();
                foreach (S3Bucket abucket in response.Buckets)
                {
                    DataRow abucketrow = ToReturn.NewRow();
                    var name = abucket.BucketName;
                    try
                    {
                        GetBucketLocationRequest gbr = new GetBucketLocationRequest();
                        gbr.BucketName = name;
                        GetBucketLocationResponse location = S3Client.GetBucketLocation(gbr);
                        
                        var region = location.Location.Value;
                        if (region.Equals("")) region = "us-east-1";
                        var pointy = RegionEndpoint.GetBySystemName(region);

                        //Build a config that references the buckets region.
                        AmazonS3Config S3C = new AmazonS3Config();
                        S3C.RegionEndpoint = pointy;
                        AmazonS3Client BS3Client = new AmazonS3Client(credential, S3C);

                        var authregion = "";
                        var EP = BS3Client.Config.RegionEndpoint.DisplayName;
                        if (String.IsNullOrEmpty(BS3Client.Config.RegionEndpoint.DisplayName)) authregion = "";
                        else {
                             authregion = BS3Client.Config.AuthenticationRegion; }

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
                        ToReturn.Rows.Add(abucketrow.ItemArray);
                    }
                }




                }
            catch
            {
                //Croak
            }


            return ToReturn;
        }
        public DataTable GetIAMUsers(string aprofile)
        {
            DataTable IAMTable = AWSTables.GetUsersDetailsTable(); //Blank table to fill out.

            Dictionary<string, string> UserNameIdMap = new Dictionary<string, string>();//Usernames to UserIDs to fill in row later.
            Amazon.Runtime.AWSCredentials credential;
            string accountid = "";
            try { 

                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var iam = new AmazonIdentityManagementServiceClient(credential);
                Dictionary<string, string> unamelookup = new Dictionary<string, string>();

                var myUserList = iam.ListUsers().Users;

                
                foreach(var rabbit in myUserList)
                {
                    unamelookup.Add(rabbit.UserId, rabbit.UserName);
                }

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
                        DataRow auserdata = IAMTable.NewRow();
                    var arow = myStringRow.Split(",".ToCharArray()[0]);

                        //Letsa dumpa da data...
                        auserdata["AccountID"] = accountid;
                        auserdata["Profile"] = aprofile;

                        string thisid = "";
                        string username = "";
                        try {
                            thisid = UserNameIdMap[arow[0]];
                            auserdata["UserID"] = thisid;
                            auserdata["UserName"] = unamelookup[thisid];
                            username= unamelookup[thisid];
                        }
                        catch
                        {
                            auserdata["UserID"] = "";
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
                string atest = "";
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
            public Dictionary<string,string> GetUserDetails(string aprofile, string username)
        {
            var credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
            var iam = new AmazonIdentityManagementServiceClient(credential);
            Dictionary<string, string> ToReturn = new Dictionary<string, string>();
            string policylist = "";
            string aklist = "";
            string groups = "";
            try {
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

            try {
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

            try {
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

        public string List2String(List<string>stringlist)
        {
            string toreturn = "";
            foreach(string astring in stringlist)
            {
                if (toreturn.Length > 2) toreturn += "\n";
                toreturn += astring;
            }
            return toreturn;
        }


        public DataTable GetEC2Instances(string aprofile, string Region2Scan)
        {
            DataTable ToReturn = AWSTables.GetEC2DetailsTable();
            RegionEndpoint Endpoint2scan = RegionEndpoint.USEast1;



            Amazon.Runtime.AWSCredentials credential;
            string accountid = "";
            
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
            var ec2 = new Amazon.EC2.AmazonEC2Client(credential,Endpoint2scan);

            
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
                 thisinstancedatarow["EventList"] = List2String(eventlist);
                 thisinstancedatarow["Tags"] = List2String (tags);
                 thisinstancedatarow["PrivateIP"] = Priv_IP;
                 thisinstancedatarow["PublicIP"] = publicIP;
                 thisinstancedatarow["PublicDNS"] = publicDNS;
                 thisinstancedatarow["PublicDNS"] = publicDNS;
                 thisinstancedatarow["VPC"] = myvpcid;
                 thisinstancedatarow["SubnetID"] = mysubnetid;
                 thisinstancedatarow["InstanceState"] = istate.Value;
                 thisinstancedatarow["VirtualizationType"] = ivirtType;
                 thisinstancedatarow["InstanceType"] = instancetype;
                 thisinstancedatarow["SecurityGroups"] = List2String( SGids);
                 thisinstancedatarow["SGNames"] = List2String( SGNames);
                //Add this instance to the data returned.
                ToReturn.Rows.Add(thisinstancedatarow);


            }//End for of instances



            return ToReturn;
        }//EndGetEC2


        static Dictionary<string,string> Network2IpRange(string sNetwork)
        {
            uint startIP;
            uint endIP;
            Dictionary<string, string> ToReturn = new Dictionary<string, string>();
            uint ip,                /* ip address */
                    mask,           /* subnet mask */
                    broadcast,      /* Broadcast address */
                    network;        /* Network address */

            int bits;

            string[] elements = sNetwork.Split(new Char[] { '/' });

            ip = IP2Int(elements[0]);
            bits = Convert.ToInt32(elements[1]);

            mask = ~(0xffffffff >> bits);

            network = ip & mask;
            broadcast = network + ~mask;

            var usableIps = (bits > 30) ? 0 : (broadcast - network - 1);

            if (usableIps <= 0)
            {
                startIP = endIP = 0;
            }
            else
            {
                startIP = network + 1;
                endIP = broadcast - 1;



            }

            var a = Convert.ToDecimal(startIP);



            ToReturn.Add("StartIP", startIP.ToString());
            
            ToReturn.Add("EndIP", endIP.ToString());
            ToReturn.Add("Broadcast", broadcast.ToString());
            ToReturn.Add("Mask", mask.ToString());
            ToReturn.Add("Network", network.ToString());
            ToReturn.Add("UsableIP", usableIps.ToString());


            return ToReturn;
        }

        public static uint IP2Int(string IPNumber)
        {
            uint ip = 0;
            string[] elements = IPNumber.Split(new Char[] { '.' });
            if (elements.Length == 4)
            {
                ip = Convert.ToUInt32(elements[0]) << 24;
                ip += Convert.ToUInt32(elements[1]) << 16;
                ip += Convert.ToUInt32(elements[2]) << 8;
                ip += Convert.ToUInt32(elements[3]);
            }
            return ip;
        }

        public DataTable GetVPCList (String aprofile)
        {
            DataTable ToReturn = AWSTables.GetVPCDetailsTable();
            Amazon.Runtime.AWSCredentials credential;
            try
            {
                credential = new Amazon.Runtime.StoredProfileAWSCredentials(aprofile);
                var ec2 = new Amazon.EC2.AmazonEC2Client(credential);
                var vippies = ec2.DescribeVpcs().Vpcs;
                foreach(var avpc in vippies)
                {
                    DataRow thisvpc = ToReturn.NewRow();
                    thisvpc["CidrBlock"] = avpc. CidrBlock;
                    thisvpc["VpcID"] = avpc.VpcId;
                    thisvpc["IsDefault"] = avpc.IsDefault.ToString();
                    thisvpc["DHCPOptionsID"] = avpc.DhcpOptionsId;
                    thisvpc["InstanceTenancy"] = avpc.InstanceTenancy;
                    thisvpc["State"] = avpc.State;
                    var tagger = avpc.Tags;
                    List<string> tlist = new List<string>();
                    foreach(var atag in tagger)
                    {
                        tlist.Add(atag.Key + ": " + atag.Value);
                    }
                    thisvpc["Tags"] = List2String(tlist);

                    ToReturn.Rows.Add(thisvpc);
                }


            }//End of the big Try
            catch
            {

            }

            return ToReturn;
        }

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
            UniqueConstraint makeUserIDUnique = new UniqueConstraint(new DataColumn[] { table.Columns["UserID"] , table.Columns["ARN"]});
            table.Constraints.Add(makeUserIDUnique);

            return table;
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
            ToReturn.Columns.Add("DefaultForAZ", typeof(string));
            ToReturn.Columns.Add("MapPubIPonLaunch", typeof(string));
            ToReturn.Columns.Add("State", typeof(string));
            ToReturn.Columns.Add("Tags", typeof(string));
            return ToReturn;
        }

        public static DataTable GetVPCDetailsTable()
        {
            DataTable ToReturn = new DataTable();

            ToReturn.Columns.Add("CidrBlock", typeof(string)); 
            ToReturn.Columns.Add("VpcID", typeof(string)); 
            ToReturn.Columns.Add("IsDefault", typeof(string)); 
            ToReturn.Columns.Add("DHCPOptionsID", typeof(string)); 
            ToReturn.Columns.Add("InstanceTenancy", typeof(string)); 
            ToReturn.Columns.Add("State", typeof(string)); 
            ToReturn.Columns.Add("Tags", typeof(string));
            


            return ToReturn;
        }

        public static string Shrug = "¯\\_(ツ)_/¯";
    }


    public class ScannerSettings
    {

        public Boolean doScanEC2 { get; set; } = true;
        public String State { get; set; } = "Idle";


    }
}//End AWSFunctions

        //Da end
  

       

 


