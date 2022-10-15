using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXInvoker.Models;

namespace Trancore.OCRXInvoker.Models
{

    public static class ProcessConfiguration
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static List<ProcessDetail> _procList;
        private static object synLock = new object();
        public static List<ProcessDetail> ProcessList
        {
            get
            {
                lock (synLock)
                {
                    if (_procList == null)
                    {
                        _logger.Info("Process list is null. Going to load configs");
                        _procList = new List<ProcessDetail>();
                        ParseConfig();
                    }
                }
                return _procList;
            }
        }

        /// <summary>
        /// Contruct process details  from config
        /// </summary>
        /// <returns></returns>
        private static void ParseConfig()
        {
            _logger.Info("Config parsing started ");

            if (ProcessList.Count == 0)
            {
                _logger.Info("There is no config items. Going to parse config");

                short count = 1;
                while (true)
                {
                    if (ConfigurationManager.AppSettings["Process" + count.ToString() + "Name"] != null)
                    {
                        ProcessList.Add(new ProcessDetail(
                            ConfigurationManager.AppSettings["Process" + count.ToString() + "Name"],
                            ConfigurationManager.AppSettings["Process" + count.ToString() + "Id"],
                            ConfigurationManager.AppSettings["Process" + count.ToString() + "Arguments"]));
                    }
                    else
                    {
                        break;
                    }
                    count++;
                }
            }
            else
                _logger.Info("Config already parsed");

            _logger.Info($"Config parsing completed. Item found = {ProcessList.Count}");
        }

    }
}
