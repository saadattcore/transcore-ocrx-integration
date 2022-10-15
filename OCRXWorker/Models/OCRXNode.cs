using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXWorker.Models
{
    public class OCRXNode
    {
        public short NodeID { get; set; }

        public string NodeName { get; set; }

        public int  WorkerID { get; set; }

        public int ServerID { get; set; }

        public bool IsServerActive { get; set; }

        public short Port { get; set; }

        public string FailureReason { get; set; }

        public OCRXNode()
        {
            WorkerID = -1;
            ServerID = -1;
            IsServerActive = false;

        }

        public OCRXNode(string name , int workerID , int serverID , bool isActive, string port , string failureReason = "")
        {
            NodeName = name;
            workerID = WorkerID;
            serverID = ServerID;
            IsServerActive = isActive;
            FailureReason = failureReason;

        }
    }
}
