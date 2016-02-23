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

        [OperationContract]
        string GetData(int value);


        [OperationContract]
        string Initialize();


        [OperationContract]
        DataTable GetEC2Table();

        /// <summary>
        /// Gets a dictionary with the status of the scanner.
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        String GetStatus();


        [OperationContract]
        string ScanAll();



        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        [OperationContract]
        Dictionary<string, bool> GetProfiles();

        [OperationContract]
        Dictionary<string, bool> GetRegions();

        // TODO: Add your service operations here
    }


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
