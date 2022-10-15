using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Transcore.OCRXInvoker.Core;

namespace Transcore.OCRXInvoker.Helpers
{
    public class Helper
    {
        public static string RootPath
        {
            get
            {
                string fullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(fullPath);
            }
        }

        public static string HostIP
        {
            get
            {
                string configIP = ConfigurationManager.AppSettings["HostIp"];

                if (string.IsNullOrEmpty(configIP))
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            configIP = ip.ToString();
                            break;
                        }
                    }

                }
                return configIP;
            }
        }

        public static string ParseException(Exception ex)
        {            
            StringBuilder error = new StringBuilder();
            error.Append(ex.Message);
            Exception e = ex.InnerException;

            while (e != null)
            {                
                error.Append(e.Message);
                e = e.InnerException;
            }

            return error.ToString();
        }
    }

   
}
