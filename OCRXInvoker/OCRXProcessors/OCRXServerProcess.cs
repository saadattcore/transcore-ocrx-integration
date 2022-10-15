using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transcore.OCRXInvoker.Helpers;

namespace Transcore.OCRXInvoker.Core
{
    /// <summary>
    /// Specific implementation for ocr
    /// </summary>
    public class OCRXServerProcess : BaseProcess
    {
        public OCRXServerProcess(string processId, string processArgument)
            : base(Path.Combine(Helper.RootPath, "OCRServer", "server.exe"), processId, processArgument)
        {

        }

        public override bool StartProcess()
        {
            bool result = false;
            _logger.Info($"Going to start ocr process. _isRunning value is = {_isRunning}");

            if (!_isRunning)
            {
                try
                {
                    _process = new Process();
                    _logger.Info($"Going to start ocr server with id = {_processId} at port = {_processArgument}");
                    _process.StartInfo.FileName = _origProcessName;
                    _process.StartInfo.WorkingDirectory = _origProcessName.Substring(0, _origProcessName.LastIndexOf('\\'));
                    _process.EnableRaisingEvents = true;
                    _process.Exited += new EventHandler(Process_Exited);
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.CreateNoWindow = false;
                    _process.StartInfo.Arguments = $"--port {_processArgument}";
                    if (_process.Start())
                    {
                        _isRunning = !_process.HasExited;
                        int delay = int.Parse(ConfigurationManager.AppSettings["OCRServerUpTimeDelay"]);
                        Thread.Sleep(delay);

                        _logger.Info($"{_processId} ocr server started successfully._isRunning value is {_isRunning} and osid = {_process.Id}");
                        _osProcId = _process.Id;
                        result = true;
                    }
                    else
                        _logger.Warn($"Failed to start ocr server id = {_processId} at port = {_processArgument}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception while starting ocr server id = {_processId} at port = {_processArgument}");
                    _logger.Error(ex.Message, ex);
                    throw;
                }
            }
            else
            {
                _logger.Info($"Ocr server  process = {_processId} at port = {_processArgument} with osid = {_process.Id} is already running");
                result = true;
            }

            return result;

        }

        public string Argument { get { return _processArgument; } }

        private void Process_Exited(object sender, EventArgs e)
        {
            _isRunning = !_process.HasExited;
            _logger.Info($"OCR process exited id = {_processId} and has exited = {_process.HasExited} with exit code = {_process.ExitCode} and _isRunning value = {_isRunning}");
            _process.Dispose();
        }

        /// <summary>
        /// Kill process tree of ocr
        /// </summary>
        public override void Kill()
        {
            _logger.Warn($"Going to kill OCR worker process tree with  process id  = {_processId}");

            // Cannot close 'system idle process'.
            if (_osProcId == 0)
            {
                return;
            }
            // kill entire graph of process parent + child.

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + _osProcId);
            ManagementObjectCollection moc = searcher.Get();

            foreach (ManagementObject mo in moc)
            {
                var pId = Convert.ToInt32(mo["ProcessID"]);
                try
                {
                    Process proc = Process.GetProcessById(pId);
                    proc.Kill();
                }
                catch (ArgumentException ex)
                {
                    // Process already exited.
                    _logger.Error(ex.Message, ex);
                }
                catch (Win32Exception ex)
                {
                    // Access denied
                    _logger.Error(ex.Message, ex);

                }
            }
        }
    }

}
