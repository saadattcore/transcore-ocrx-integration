using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXInvoker.Core;

namespace Transcore.OCRXInvoker.Models
{
    public class ProcessDetail
    {
        /// <summary>
        /// This class hold pair of worker & ocr process
        /// </summary>
        public ProcessDetail(string processName, string processId, string argument)
        {
            ServerProcess = new OCRXServerProcess(processId, argument);
            WorkerProcess = new OCRXWorkerProcess(processName, processId, argument);
        }

        public OCRXServerProcess ServerProcess { get; set; }

        public OCRXWorkerProcess WorkerProcess { get; set; }

    }
}
