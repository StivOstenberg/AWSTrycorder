using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



        public Dictionary<string, object> GetEC2Instances(string account, string Region2Scan)
        {
            Dictionary<string, object> ToReturn = new Dictionary<string, object>();




            return ToReturn;
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
        /// Given 
        /// </summary>
        /// <param name="aprofile">An AWS Profile name stored in Windows Credential Store</param>
        /// <param name="auser">The Name of a User</param>
        /// <returns>Dictionary containing keys for each type of data[AcessKeys], [Groups], [Policies]</returns>
    
        public Dictionary<string,string> GetUserDetails(string aprofile, string auser)
        {
            Dictionary<string, string> ToReturn = new Dictionary<string, string>();




            return ToReturn;
        }

    }//EndScanAWS
}//End AWSFunctions

        //Da end
  

       

 


