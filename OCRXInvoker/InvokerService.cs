using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Trancore.OCRXInvoker.Models;
using Transcore.OCRXInvoker.Core;
using Transcore.OCRXInvoker.Helpers;

namespace Transcore.OCRXInvoker
{
    partial class InvokerService : ServiceBase
    {
        #region Member
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        public InvokerService()
        {
            _logger.Info("Invoker service constructor invoked");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Service start Trigger");

            Thread t = new Thread(new ThreadStart(StartProcessesAsync));
            t.IsBackground = true;
            t.Start();

            double interval = 0;
            bool result = Double.TryParse(ConfigurationManager.AppSettings["Interval"], out interval);

            if (!result)
                _logger.Info("Interval value is invalid please review in app.config");

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = interval;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Enabled = true;
            timer.Start();

        }

        protected override void OnStop()
        {
            _logger.Info("Windows service going to stop");

            try
            {
                TearDown();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
            }
        }

        public void OnTimer(object sender, ElapsedEventArgs e)
        {
            _logger.Info("Timer ticker event ");

            try
            {
                StartProcesses();
            }
            catch (Exception ex)
            {

                _logger.Error(ex.Message, ex);
            }

        }


        private void StartProcessesAsync()
        {
            _logger.Info("Background thread started");

            try
            {
                _logger.Info("Staring processes configured in app.config");
                StartProcesses();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
            }
        }

        private void TearDown()
        {             

            _logger.Info($"Number of process OCR & Worker pairs to be killed are {ProcessConfiguration.ProcessList.Count}");

            foreach (var item in ProcessConfiguration.ProcessList)
            {
                item.ServerProcess.Kill();
                item.WorkerProcess.Kill();
            }

            ProcessConfiguration.ProcessList.Clear();
        }

        private void StartProcesses()
        {
            if (ProcessConfiguration.ProcessList.Count > 0)
            {
                _logger.Info($"Found items in list. Count = {ProcessConfiguration.ProcessList.Count}");

                ProcessManager.StartProcesses();
            }
        }

    }
}
