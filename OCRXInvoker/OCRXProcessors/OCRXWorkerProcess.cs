using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXInvoker.Core
{
    /// <summary>
    /// Specific implementation for worker process
    /// </summary>
    public class OCRXWorkerProcess : BaseProcess
    {
        public OCRXWorkerProcess(string origProcessName, string processId, string processArgument)
            : base(origProcessName, processId, processArgument)
        {
        }

        public override bool StartProcess()
        {
            bool result = false;
            _logger.Info($"Going to start worker process _isRunning value is = {_isRunning}");

            if (!_isRunning)
            {
                try
                {
                    _process = new Process();
                    _logger.Info($"Going to start worker process with id = {_processId}");

                    _process.StartInfo.FileName = _origProcessName;
                    _process.StartInfo.WorkingDirectory = _origProcessName.Substring(0, _origProcessName.LastIndexOf('\\'));
                    _process.EnableRaisingEvents = true;
                    _process.Exited += new EventHandler(Process_Exited);
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.CreateNoWindow = false;
                    _process.StartInfo.Arguments = $"{Environment.MachineName} {_processArgument} {_processId}";
                    if (_process.Start())
                    {
                        _isRunning = !_process.HasExited;
                        _logger.Info($"{_processId} worker process started successfully with osid = {_process.Id}._isRunning value is = {_isRunning}");
                        _osProcId = _process.Id;
                        result = true;
                    }
                    else
                        _logger.Warn($"Failed to start Worker process id = {_processId}");

                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception while starting worker process id = {_processId}");
                    _logger.Error(ex.Message, ex);
                }
            }
            else
            {
                _logger.Info($"Worker process = {_processId} with osid = {_process.Id} is already running");
                result = true;
            }

            return result;
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            _isRunning = !_process.HasExited;
            _logger.Info($"Worker process exited id = {_processId} and has exited = {_process.HasExited} with exit code = {_process.ExitCode} and _isRunning value = {_isRunning}");
            _process.Dispose();
        }
    }
}
