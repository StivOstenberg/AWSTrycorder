using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;

using System;
using System.Collections.Generic;
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
            foreach(var EP in Regions)
            {
                RegionNames.Add(EP.DisplayName);
            }
            return RegionNames;

        }



        public Dictionary<string,object> GetEC2Instances(string account, string Region2Scan)
        {
            Dictionary<string, object> ToReturn = new Dictionary<string, object>();




            return ToReturn;
        }

        /// <summary>
        /// Collects the information of IAM users in a particular profile.
        /// </summary>
        /// <param name="Profile2Scan"></param>
        /// <returns>Returns a dictionary, keyed to UserID with KVPs for each field and value</returns>
        public Dictionary<string,List<KeyValuePair<string,string>>> GetIAMUsers (string Profile2Scan)
        {
            Dictionary<string, List<KeyValuePair<string, string>>> ToReturn = new Dictionary<string, List<KeyValuePair<string, string>>>();


            return ToReturn;
        }

    }

}
