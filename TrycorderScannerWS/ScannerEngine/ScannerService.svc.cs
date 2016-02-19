using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;


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
         Settings.State = "Scanning..";
            if (Settings.doScanEC2) ScanEC2();



          Settings.State = "Idle";
           return "Ribbitflux";
        }

        public string ScanEC2()
        {


            return "Done EC2";
        }
    }


}
