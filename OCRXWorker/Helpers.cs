using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXWorker.Enums;
using Transcore.OCRXWorker.Models;

namespace Transcore.OCRXWorker.Helpers
{
    public class Helpers
    {
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SaveImage(byte[] data, string name)
        {
            File.WriteAllBytes($"E:\\{name}.jpeg", data);
        }


        public static void SendNotification(string subject, string body)
        {
            try
            {
                SmtpSection secObj = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                string reciepient = ConfigurationManager.AppSettings["ToEmail"];
                string emailServerDomain = ConfigurationManager.AppSettings["EmailServerDomain"];
                string emailServerUserName = ConfigurationManager.AppSettings["ExchangeNoReplyServerUser"];

                using (MailMessage mm = new MailMessage())
                {
                    mm.From = new MailAddress(secObj.From);
                    mm.To.Add(reciepient);
                    mm.Subject = subject;
                    mm.Body = body;
                    mm.IsBodyHtml = false;

                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = secObj.Network.Host;
                    smtp.EnableSsl = secObj.Network.EnableSsl;
                    NetworkCredential NetworkCred = new NetworkCredential(emailServerUserName, secObj.Network.Password, emailServerDomain);

                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = NetworkCred;
                    smtp.Port = secObj.Network.Port;
                    smtp.Send(mm);
                }


            }
            catch (Exception ex)
            {
                _logger.Error("Error while sending email to IT support team");
                _logger.Error(ex.Message, ex);
            }
        }

        #region Properties
        public static short BatchSize
        {
            get
            {
                return short.Parse(ConfigurationManager.AppSettings["BatchSize"]);
            }
        }

        public static short ConfThreshold
        {
            get
            {
                return short.Parse(ConfigurationManager.AppSettings["ConfThreshold"]);
            }
        }

        public static short BlankImageConfThreshold
        {
            get
            {
                return short.Parse(ConfigurationManager.AppSettings["ConfThreshold"]);
            }
        }

        public static short WorkerSleepInterval
        {
            get
            {
                return short.Parse(ConfigurationManager.AppSettings["WorkerSleepInterval"]);
            }
        }

        public static string ProcessingMode
        {
            get
            {
                return ConfigurationManager.AppSettings["ProcessingMode"];
            }
        }

        public static short ChunkSize
        {
            get { return 1; }
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

        #endregion
    }

    public static class Extensions
    {
        public static List<BatchInfo> ToBatchInfo(this DataSet ds, out int TotalPlates)
        {
            TotalPlates = 0;
            List<BatchInfo> batchList = new List<BatchInfo>();

            DataTable dtBatchInfo = ds.Tables[1];

            for (int i = 0; i < dtBatchInfo.Rows.Count; i++)
            {
                BatchInfo batch = new BatchInfo();

                batch.BatchID = Convert.ToInt64(dtBatchInfo.Rows[i]["biBatchId"]);
                byte status = Convert.ToByte(dtBatchInfo.Rows[i]["tiStatusId"]);
                batch.Status = (BatchStatus)Enum.Parse(typeof(BatchStatus), status.ToString());

                DataTable dtPlateInfo = ds.Tables[0];
                batch.LicensePlates = new List<PlateInfo>();
                for (int j = 0; j < dtPlateInfo.Rows.Count; j++)
                {
                    PlateInfo pInfo = new PlateInfo();
                    pInfo.BatchID = Convert.ToInt64(dtPlateInfo.Rows[j]["biBatchId"]);

                    if (pInfo.BatchID == batch.BatchID)
                    {
                        TotalPlates++;
                        pInfo.TransactionID = Convert.ToInt64(dtPlateInfo.Rows[j]["biImgProcessedID"]);

                        pInfo.ImageIndex = Convert.ToInt64(dtPlateInfo.Rows[j]["biImageIndex"]);
                        pInfo.ImageFile = dtPlateInfo.Rows[j]["vcImageLocation"].ToString();
                        pInfo.YYYYMM = Convert.ToInt32(dtPlateInfo.Rows[j]["iYYYYMM"]);

                        batch.LicensePlates.Add(pInfo);

                    }
                }

                batchList.Add(batch);
            }

            return batchList;
        }

        public static byte[] ToByteArray(this Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }
    }

    public class OCRException : Exception
    {
        public OCRException(string message, System.Net.HttpStatusCode httpStatusCode)
        {
            this.ErrorMessage = message;
            this.StatusCode = httpStatusCode;
        }

        public string ErrorMessage { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }



}
