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
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ScannerInterfaceDefinition
    {
        /// <summary>
        /// Pulls a Dataset with all der datatables from scans.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        DataSet ScanResults();

        [OperationContract]
        void ScanAll();

        [OperationContract]
        string GetData(int value);


        [OperationContract]
        string Initialize();


        [OperationContract]
        string LastScan();

        [OperationContract]
        DataTable GetEC2Table();

        [OperationContract]
        DataTable GetIAMTable();

        [OperationContract]
        DataTable GetS3Table();

        [OperationContract]
        DataTable GetRDSTable();

        [OperationContract]
        DataTable GetSubnetsTable();

        [OperationContract]
        DataTable GetVPCTable();


        /// <summary>
        /// Gets a string with the status of the scanner.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        String GetStatus();




        [OperationContract]
        void SetRegionStatus(string region,bool state);

        [OperationContract]
        void setProfileStatus(string aprofile, bool state);


        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        [OperationContract]
        Dictionary<string, bool> GetProfiles();

        [OperationContract]
        Dictionary<string, bool> GetRegions();

        [OperationContract]
        Dictionary<string, bool> GetComponents();

        [OperationContract]
        string GetDetailedStatus();

        [OperationContract]
        void SetComponentScanBit(string component, bool state);

        void PayPalDonate(string youremail, string description, string country, string currency);
    }
    [ServiceContract]


    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        
        string stringValue = "Hello ";
        bool boolValue = true;

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
