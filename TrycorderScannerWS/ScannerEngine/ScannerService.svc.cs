﻿using System;
using System.Collections.Generic;
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
        // This section will hold the private data the functions will interact with
        //Accounts List with Credentials and an Enable/Disable field  Last success +  Fail Count.
        //AWSRegionList(RegionEnglish, Region Name, Enable)
        //AWS Component Table [Enable, Last Complete Scan,  Last Incomplete,  ScanStatus]
        //Scanner Status (Running, Stopping, Ready)


        //Tables shall be Dictionary(String,Dictionary,(string,List)
        //EC2 Table  
        //S3 Table
        //IAM Table
        //VPC table
        //RDS Table

        //Settings Stuff
        //External MySQL (Endpoint, Port, User, Password, Certificate?)

        //This section will hold private functions to update our data objects and maybe to log out to external data collectors
        //ScanAll for each component type


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
    }
}
