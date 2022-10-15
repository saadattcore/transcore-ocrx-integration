using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading.Tasks;
using Trancore.OCRXInvoker.Models;
using Transcore.OCRXInvoker.Core;
using Transcore.OCRXWorker.DAL;
using TransCore.ORT.CSC.HostProcesses.Workers;

using TransCore.ORT.CSC.IPC.SharedLibrary;


namespace OCRXInvoker
{
    class Program
    {
        private static CommandProcessor sm;
        private static StatusMessage _statusMessage;
        private static DateTime _startDateTime;
        private static IpcChannel ipc;

        private static bool _terminateProgram = false;
        private const int MILLISECONDS_IN_MINUTE = 60000;

        static void Main(string[] args)
        {
            args = new string[2];


            args[0] = "OCRXInterface";
            args[1] = "OCRXLog";

            if (args.Length < 2)
            {
                SATSLogging.LogError("Invalid number of arguments");
            }
            else
            {
                //CR_859: Accepting 2 arguments by CSCRTDCSys process:
                //Event log name.
                //Event Source name (it will also be used as the name that will be part of the URI of the channel inquired by CSCRTDCIF application).

                SATSLogging.Init(args[0], args[1]);

                try
                {
                    _startDateTime = DateTime.Now;

                    Start(args[0]);

                    while (_terminateProgram == false)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                catch (Exception exc)
                {
                    SATSLogging.LogError("Error Starting CSCRTDCSys", exc);
                }
            }
        }

        static void Start(String portName)//CR_859: Adding parameter 
        {
            int count = 0;

            try
            {
                // start ipc listener for command line process

                // need this or we can't use delegates and remoting.
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();

                System.Collections.Hashtable properties = new System.Collections.Hashtable();
                properties["portName"] = portName;
                properties["authorizedGroup"] = "Administrators";

                ipc = new IpcChannel(properties, clientProv, serverProv);
                ipc.IsSecured = false;
                ChannelServices.RegisterChannel(ipc, false);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(CommandProcessor), "ProcessControl", WellKnownObjectMode.Singleton);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(StatusMessage), "ProcessStatusMessage", WellKnownObjectMode.Singleton);

                sm = (CommandProcessor)Activator.GetObject(typeof(CommandProcessor),
                    "ipc://" + portName + "/ProcessControl");

                _statusMessage = (StatusMessage)Activator.GetObject(typeof(StatusMessage),
                    "ipc://" + portName + "/ProcessStatusMessage");

                sm.OnCommandReceived += new OnCommandReceivedEventHandler(sm_OnCommandReceived);
                _statusMessage.OnStatusMessageReceived += new OnStatusMessageReceivedEventHandler(_statusMessage_OnStatusMessageReceived);

                //Worker worker;
                //string id = "";


                int pingMinutes = DataAccess.GetOCRXPingMinues();

                if (ProcessConfiguration.ProcessList.Count > 0)
                {
                    //_logger.Info($"Found items in list. Count = {ProcessConfiguration.ProcessList.Count}");

                    Task.Run(() => {

                        ProcessManager.StartProcesses();

                    });

                    
                }

                
                System.Timers.Timer timer = new System.Timers.Timer(pingMinutes * MILLISECONDS_IN_MINUTE);
                timer.AutoReset = true;
                timer.Elapsed += new System.Timers.ElapsedEventHandler((object sender, System.Timers.ElapsedEventArgs e) =>
                {
                    if (ProcessConfiguration.ProcessList.Count > 0)
                    {
                        //_logger.Info($"Found items in list. Count = {ProcessConfiguration.ProcessList.Count}");

                        ProcessManager.StartProcesses();
                    }
                });

                timer.Start();
                

            }
            catch (Exception exc)
            {
                SATSLogging.LogError("Error Initializing Workers", exc);
            }
        }

        static string sm_OnCommandReceived(CommandsEnum command, string processId)
        {
            string retval = "";
            switch (command)
            {
                case CommandsEnum.TerminateProgram:
                    TearDown();
                    break;
            }

            return retval;
        }

        static void _statusMessage_OnStatusMessageReceived(string processId, string message)
        {
            //set status for worker process
            /* if (_processes.ContainsKey(processId))
             {
                 _processes[processId].Status = message;
                 _processes[processId].LastStatusUpdate = DateTime.Now;
             }
         }
         */
        }

        private static void TearDown()
        {
            foreach (var item in ProcessConfiguration.ProcessList)
            {
                if (item.WorkerProcess != null)
                {
                    try
                    {
                        item.WorkerProcess.Terminate(0);
                        item.ServerProcess.Kill();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            foreach (var item in ProcessConfiguration.ProcessList)
            {
                if (item.WorkerProcess.WaitForExit(20000) == false)
                {
                    SATSLogging.LogWarning(String.Format("Forcibly killing process {0}", item.WorkerProcess.ProcessId));
                    item.WorkerProcess.Kill();
                }
                item.ServerProcess.Kill();
            }


            _terminateProgram = true;
        }


    }

    public class CommandProcessor : MarshalByRefObject,
          TransCore.ORT.CSC.IPC.SharedLibrary.IServiceControlMessage,
          TransCore.ORT.CSC.IPC.SharedLibrary.ITerminateMessage
    {
        public event OnCommandReceivedEventHandler OnCommandReceived;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region ICommandProcessing Member

        public string StartProcess(string processID)
        {
            SATSLogging.LogInfo(String.Format("Starting Process : ProcessId {0}", processID));

            if ((OnCommandReceived != null) &&
                             (OnCommandReceived.GetInvocationList().Length > 0))
                return OnCommandReceived(CommandsEnum.StartProcess, processID);

            return "Event not handled in command processor.";
        }

        public string StopProcess(string processID)
        {
            SATSLogging.LogInfo(String.Format("Stopping Process : ProcessId {0}", processID));

            if ((OnCommandReceived != null) &&
                             (OnCommandReceived.GetInvocationList().Length > 0))
                return OnCommandReceived(CommandsEnum.StopProcess, processID);

            return "Event not handled in command processor.";
        }

        public string GetStatus()
        {
            string status = "";
            if ((OnCommandReceived != null) &&
                             (OnCommandReceived.GetInvocationList().Length > 0))
                status = OnCommandReceived(CommandsEnum.Status, "");


            return status;
        }

        #endregion

        #region ITerminate Members

        public void Terminate()
        {
            if ((OnCommandReceived != null) &&
                             (OnCommandReceived.GetInvocationList().Length > 0))
                OnCommandReceived(CommandsEnum.TerminateProgram, "");

        }
        #endregion
    }


    public class StatusMessage : MarshalByRefObject, TransCore.ORT.CSC.IPC.SharedLibrary.IStatusMessage
    {
        public event OnStatusMessageReceivedEventHandler OnStatusMessageReceived;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region IStatusMessage Members

        public void SendStatusMessage(string processID, string message)
        {
            if ((OnStatusMessageReceived != null) &&
                             (OnStatusMessageReceived.GetInvocationList().Length > 0))
                OnStatusMessageReceived(processID, message);
        }

        #endregion
    }

    public delegate string OnCommandReceivedEventHandler(CommandsEnum command, string processId);

    public delegate void OnStatusMessageReceivedEventHandler(string processId, string message);

}