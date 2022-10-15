using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLDC;
using System.Web.Script.Serialization;
using System.Configuration;
using log4net;
using System.Threading;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Net.Http;
using Transcore.OCRXWorker.Models;
using Transcore.OCRXWorker.Enums;
using Transcore.OCRXWorker.DAL;
using System.Net;
using System.Net.Sockets;
using Transcore.OCRXWorker.Shared;
using Transcore.OCRXWorker.Helpers;

namespace Transcore.OCRXWorker.Core
{
    public abstract class BaseWorker
    {
        #region Members

        protected readonly IRestClient _restClient;
        protected static readonly DataSet _lookups;
        protected static MultipleImageFileManager _mifManager = new MultipleImageFileManager();
        protected static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected short _port;
        protected List<BatchInfo> _batch;
        protected int _totalPlatesFound = 0;
        protected int _batchFailedCount = 0;
        protected bool _keepWorking;
        protected string _nodeName;

        #endregion

        #region CTOR
        public BaseWorker(short port, string nodeName)
        {
            string baseAddress = $"http://{Helpers.Helpers.HostIP}:{port}/";
            _restClient = new RestClient(baseAddress, int.Parse(ConfigurationManager.AppSettings["ApiTimeOut"]));
            _port = port;
            _nodeName = nodeName;
        }

        static BaseWorker()
        {
            _lookups = DataAccess.GetLookups();
        }


        #endregion

        #region Public Methods
        public abstract void ProcessNexteBatch();

        #endregion

        #region Private Methods
        protected abstract void RetrievePlatesDetailsFromServer();

        protected List<PlateInfo> SendRequest()
        {
            WrapperResult<List<PlateInfo>> response = null;
            List<PlateInfo> responseOCR = new List<PlateInfo>();

            try
            {
                string imageExt = ConfigurationManager.AppSettings["ImageExtension"];
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue;
                List<ApiRequestModel> requestList = new List<ApiRequestModel>();

                foreach (BatchInfo batch in _batch)
                {
                    var tmpReqList = batch.LicensePlates.Select(x => new ApiRequestModel { TransactionID = x.TransactionID, Ext = imageExt, ImageData = Convert.ToBase64String(x.ImageBytes) }).ToList();
                    requestList.AddRange(tmpReqList);
                }

                int totalRequestObjects = requestList.Count;
                int iterations = (totalRequestObjects / Helpers.Helpers.ChunkSize);

                for (int i = 0; i < iterations; i++)
                {
                    int startIndex = (i * Helpers.Helpers.ChunkSize);

                    var requestChunk = requestList.GetRange(startIndex, Helpers.Helpers.ChunkSize);

                    string json = js.Serialize(requestChunk);

                    response = _restClient.Post<List<PlateInfo>>("process-plates", json).Result;


                    if (response.Status != System.Net.HttpStatusCode.OK)
                    {
                        throw new OCRException(response.Message, response.Status);
                    }

                    responseOCR.AddRange(response.Data);
                }



            }
            catch (AggregateException ex)
            {
                var innerException = ex.InnerException;

                while (innerException != null)
                {
                    if (innerException is HttpRequestException || innerException.InnerException == null)
                    {
                        throw innerException;
                    }

                    innerException = innerException.InnerException;
                }
            }

            return responseOCR;

        }

        protected void ResetAndCleanup()
        {
            foreach (BatchInfo batch in _batch)
            {
                if (batch != null && batch.LicensePlates.Count > 0)
                {
                    foreach (PlateInfo plate in batch.LicensePlates)
                    {
                        Array.Clear(plate.ImageBytes, 0, plate.ImageBytesRead);
                    }
                    batch.LicensePlates.Clear();
                    batch.BatchID = 0;
                    batch.Status = BatchStatus.Init;
                }              
            }

            _batch.Clear();
            _batch = null;

            this._totalPlatesFound = 0;
            this._batchFailedCount = 0;
        }

        public void Terminate()
        {
            _keepWorking = false;
        }

        #endregion


    }
}
