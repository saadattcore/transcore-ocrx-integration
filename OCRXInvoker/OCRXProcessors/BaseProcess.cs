using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransCore.ORT.CSC.HostProcesses.Workers;
using TransCore.ORT.CSC.IPC.SharedLibrary;

namespace Transcore.OCRXInvoker.Core
{
    /// <summary>
    /// Base class for ocr & worker process. Common members goes here.
    /// </summary>
    public abstract class BaseProcess
    {
        protected Process _process;
        protected string _origProcessName;
        protected string _processArgument;
        protected string _processId;
        protected bool _isRunning;
        protected int _osProcId;

        protected static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BaseProcess(string origProcessName, string processId, string processArgument)
        {
            _origProcessName = origProcessName;
            _processId = processId;
            _processArgument = processArgument;
            _isRunning = false;
            _osProcId = -1;
            //_process = new Process();
        }

        public abstract bool StartProcess();

        public virtual void Kill()
        {
            if (_isRunning)
            {
                _logger.Warn($"Going to kill {this.GetType().Name} with  process id  = {_processId}");
                _process.Kill();                
            }
            else
                _logger.Info($"{this.GetType().Name} with  process id  = {_processId} does not exist");
        }

        public virtual bool Terminate(int timeout)  // CR_770: Allow timeout to be specified
        {
            bool terminated = false;
            ITerminateMessage termMsg;

            if (this._process.HasExited == false)
            {
                // send IPC message to terminate 
               // _restart = false;

                try
                {
                    termMsg = (ITerminateMessage)Activator.GetObject(typeof(ITerminateMessage),
                        string.Format("ipc://OCRX{0}/TerminateMessage", _processId));
                    if (termMsg != null)
                    {
                        termMsg.Terminate();
                        terminated = _process.WaitForExit(timeout); // CR_770: Wait for specified timeout
                        //terminated = true;
                    }


                }
                catch (Exception ex)
                {
                    SATSLogging.LogError(String.Format("Error terminating process {0}: " + ex.Message, _processId), ex);
                }

                /* CR_770: Start */
                /* Don't kill processes which fail to terminate cleanly */
                //if (terminated == false)
                //    _process.Kill();
                /* CR_770: End */

            }

            return terminated;
        }

        public bool WaitForExit(int milliseconds)
        {
            return _process.WaitForExit(milliseconds);
        }


        public bool IsRunning { get { return this._isRunning; } }
        public string ProcessId { get { return _processId; } }
        public int OsProcId { get { return _osProcId; } }



    }

}
