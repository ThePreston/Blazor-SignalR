using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorSignalRApp.Server.Model
{
    public class BlobProcess
    {
        public BlobProcess()
        {

        }
        public BlobProcess(string partitionKey, string rowKey, string eventName, string data, string timestamp)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            Data = data;
            EventName = eventName;
            Timestamp = timestamp;
        }

        public string Data { get; set; }
        
        public string EventName { get; set; }
        
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string Timestamp { get; set; }

    }
}
