using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TransCore.ORT.CSC.IPC.SharedLibrary;
using TransCore.ORT.CSC.HostProcesses.Workers;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels;

namespace OCRXInterfaceService
{
    partial class OCRXInterfaceService : ServiceBase
    {
        private int processID;
        public OCRXInterfaceService()
        {
            SATSLogging.Init("OCRXInterfaceService", "OCRXLog");

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.

            Process process = Process.Start(System.Configuration.ConfigurationManager.AppSettings["ProcessPath"] + "\\OCRXInvoker.exe", "OCRXInterface OCRXLog");
            processID = process.Id; //CR_859: Storing the processID
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.

            // Add code here to perform any tear-down necessary to stop your service.
            IpcChannel ipc = new IpcChannel("OCRXInterfaceService");
            ChannelServices.RegisterChannel(ipc, false);

            ITerminateMessage msg = (ITerminateMessage)Activator.GetObject(typeof(ITerminateMessage),
                "ipc://OCRXInterface/ProcessControl");
            if (msg == null)
            {
                SATSLogging.LogError("Failed to communicate with CSCRTDCInterface.");
                return;
            }

            msg.Terminate();

            Process proc = Process.GetProcessById(processID);//CR_859: We have 2 services running CSCRTDCSys, therefore; we need to kill by ID not name
            bool terminated = proc.WaitForExit(30000);
            if (terminated == false)
                proc.Kill();

        }
    }
}
