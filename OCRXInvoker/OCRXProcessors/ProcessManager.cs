using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXInvoker.Shared;
using Transcore.OCRXInvoker.Helpers;
using Transcore.OCRXInvoker.Core;
using Transcore.OCRXInvoker.Models;
using Trancore.OCRXInvoker.Models;
using Transcore.OCRXWorker.DAL;
using Transcore.OCRXWorker.Models;

namespace Transcore.OCRXInvoker.Core
{
    public class ProcessManager
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ProcessManager() { }

        public static void StartProcesses()
        {
            _logger.Info($"Going to start worker & ocr process");

            var nodes = new List<OCRXWorker.Models.OCRXNode>();
            OCRXNode node = null;

            foreach (ProcessDetail process in ProcessConfiguration.ProcessList)
            {
                node = new OCRXNode();
                node.NodeName = $"{Environment.MachineName}_{process.WorkerProcess.ProcessId}";
                short port;
                short.TryParse(process.ServerProcess.Argument, out port);
                node.Port = port;

                try
                {

                    if (process.ServerProcess.StartProcess() && process.ServerProcess.IsRunning)
                    {

                        if (PingServer(process.ServerProcess.Argument))
                        {
                            if (process.WorkerProcess.StartProcess() && process.WorkerProcess.IsRunning)
                            {
                                nodes.Clear();

                                node.ServerID = process.ServerProcess.OsProcId;
                                node.WorkerID = process.WorkerProcess.OsProcId;
                                node.IsServerActive = true;
                                nodes.Add(node);
                                DataAccess.PingNodes(nodes);

                            }

                        }
                        else
                        {
                            _logger.Warn($"Ocr server at port = {process.ServerProcess.Argument} failed to response. Skipping invocation of worker process {process.WorkerProcess.ProcessId}");

                            Transcore.OCRXWorker.Helpers.Helpers.SendNotification($"Failed to recieve response from OCR server {process.ServerProcess.ProcessId}",
                                $"Failed to recieve response from OCR server = {process.ServerProcess.ProcessId} at port = {process.ServerProcess.Argument}");
                        }

                    }

                }
                catch (AggregateException ex)
                {
                    var innerException = ex.InnerException;
                    string error = string.Empty;

                    while (innerException != null)
                    {
                        if (innerException is HttpRequestException || innerException.InnerException == null)
                        {
                            error = $"OCR-X server is down at port {process.ServerProcess.Argument}";

                            _logger.Error(error);
                            _logger.Error(ex.Message, ex);

                        }
                        innerException = innerException.InnerException;
                    }

                    if (string.IsNullOrEmpty(error))
                    {
                        error = Helper.ParseException(ex);
                    }

                    Transcore.OCRXWorker.Helpers.Helpers.SendNotification($"Error while pinging OCR-X server {process.ServerProcess.ProcessId}", error);

                    _logger.Error(ex.Message, ex);

                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    Transcore.OCRXWorker.Helpers.Helpers.SendNotification($"Error while starting OCR-X server {process.ServerProcess.ProcessId}", Helper.ParseException(ex));
                }


                //nodes.Add(node);

            }

            //if (nodes.Count > 0)
             //   DataAccess.PingNodes(nodes);

        }

        public static bool PingServer(string port)
        {
            string baseAddress = $"http://{Helper.HostIP}:{port}/";

            //string baseAddress = $"http://10.212.134.202:{port}";

            int timeOut = int.Parse(ConfigurationManager.AppSettings["ServiceTimeOut"]);

            IRestClient client = new RestClient(baseAddress, timeOut);
            _logger.Info($"Going to ping ocr server at = {baseAddress}");
            var response = client.Get("ping-server").Result;
            _logger.Info($"Receieved response from server with message ={response}");
            return response.ToLower().Contains("flask server");

        }

    }
}
