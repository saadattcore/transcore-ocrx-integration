using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transcore.OCRXWorker.DAL;
using Transcore.OCRXWorker.Core;
using log4net;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using TransCore.ORT.CSC.IPC.SharedLibrary;
using TransCore.ORT.CSC.HostProcesses.Workers;

namespace Transcore.OCRXWorker
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string _processId;
        private static string _machineName;
        private static IpcChannel ipc;
        private static TerminateMessage _terminateMsg;
        private static Transcore.OCRXWorker.Core.BaseWorker _worker = null;

        static void Main(string[] args)
        {
            //while (true) { }

            //args = new string[3];
            //args[1] = "5001";
            //args[2] = "62795001";
            //args[0] = Environment.MachineName;
            _logger.Info("OCRXWorker invoked");

            try
            {

                if (args.Length == 0)
                {
                    _logger.Error("Port number is empty");
                }
                else
                {
                    short port = short.Parse(args[1]);
                    _processId = args[2];
                    _machineName = args[0];

                    _logger.Info($"port = {port}, process id = {_processId}, machine name = {_machineName}");


                    BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                    serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                    BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();

                    System.Collections.Hashtable properties = new System.Collections.Hashtable();
                    properties["portName"] = "OCRX" + _processId;
                    properties["exclusiveAddressUse"] = false;//CR_859: This property is used to fix the error: Could not register ipc channel: Access is denied

                    ipc = new IpcChannel(properties, clientProv, serverProv);

                    ChannelServices.RegisterChannel(ipc, false);

                    RemotingConfiguration.RegisterWellKnownServiceType(
                        typeof(TerminateMessage), "TerminateMessage", WellKnownObjectMode.Singleton);


                    _terminateMsg = (TerminateMessage)Activator.GetObject(typeof(TerminateMessage),
                        string.Format("ipc://OCRX{0}/TerminateMessage", _processId));

                    _terminateMsg.TerminateRequested += new EventHandler(_terminateMsg_TerminateRequested);

                    _worker = new BlankImageRejectionWorker(port, $"{_machineName}_{_processId}");

                    while (true)
                    {
                        _worker.ProcessNexteBatch();
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);

            }
        }

        static void _terminateMsg_TerminateRequested(object sender, EventArgs e)
        {


            //SATSLogging.LogInfo(string.Format("terminate requested for ProcessID:{0} Point:{1} Zone:{2}", _processId, _pointNumber, _zoneNumber));

            //shutdown the worker
            _worker.Terminate();

            //_endTask = true;
        }
    }
}
