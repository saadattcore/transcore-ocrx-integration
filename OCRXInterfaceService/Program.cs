using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRXInterfaceService
{
    class Program
    {
        static void Main(string[] args)
        {
            Process process = Process.Start(System.Configuration.ConfigurationManager.AppSettings["ProcessPath"] + "\\OCRXInvoker.exe", "OCRXInterface OCRXLog");
            int processID = process.Id; //CR_859: Storing the processID
        }
    }
}
