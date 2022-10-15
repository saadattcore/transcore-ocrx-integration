using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Transcore.OCRXWorker.DAL;
using Transcore.OCRXWorker.Enums;
using Transcore.OCRXWorker.Helpers;
using Transcore.OCRXWorker.Models;

namespace Transcore.OCRXWorker.Core
{
    public class BlankImageRejectionWorker : BaseWorker
    {

        #region CTOR
        public BlankImageRejectionWorker(short port, string nodeName) : base(port, nodeName)
        {
        }

        #endregion

        #region Public Methods
        public override void ProcessNexteBatch()
        {

            _keepWorking = true;

            bool updateDb = false;

            System.Diagnostics.Stopwatch totalTimeWatch = new System.Diagnostics.Stopwatch();
            //DataAccess da = new DataAccess();

            #region Process The Batch

            try
            {
                if (_batch == null || _batch.Count == 0)
                {
                    _batch = DataAccess.GetBatch(_nodeName, out this._totalPlatesFound);

                }

                totalTimeWatch.Start();

                if (_batch != null && _batch.Count > 0)
                {
                    _logger.Info($"Batch size = {_totalPlatesFound} for worker = {_nodeName} at port = {_port}");

                    RetrievePlatesDetailsFromServer();

                    foreach (var item in _batch)
                    {
                        item.Status = BatchStatus.Success;
                    }

                    updateDb = true;

                    _logger.Info("Plates details retrieved successfully");
                }
                else
                {
                    totalTimeWatch.Stop();

                    totalTimeWatch.Reset();

                    int delayInterval = int.Parse(ConfigurationManager.AppSettings["SleepThreadInCaseNoData"]);

                    Thread.Sleep(delayInterval); // in case no data present in backlog then mark this process to sleep .
                }
            }
            catch (HttpRequestException ex) // sleep worker
            {
                _logger.Error(ex.Message, ex);

                _logger.Error($"OCR-X server is down at port = {this._port} with node = {_nodeName}. Going to make worker process sleep for {Helpers.Helpers.WorkerSleepInterval} minutes");

                totalTimeWatch.Stop();

                totalTimeWatch.Reset();

                _logger.Info("Worker process will going to sleep");

                Thread.Sleep(Helpers.Helpers.WorkerSleepInterval);

            }
            catch (OCRException ex)
            {
                _logger.Error($"OCR-X exception encounter for node = {_nodeName} with port = {_port}");

                _logger.Error($"{ex.ErrorMessage} & HttpStatus code = {ex.StatusCode.ToString()}", ex);

                foreach (var item in _batch)
                {
                    item.Status = BatchStatus.Failed;
                }

                updateDb = true;

                totalTimeWatch.Stop();

                totalTimeWatch.Reset();

            }
            catch (Exception ex)
            {
                totalTimeWatch.Stop();

                totalTimeWatch.Reset();

                _logger.Error(ex.Message, ex);

                _batchFailedCount++;

                int batchFailedCount = int.Parse(ConfigurationManager.AppSettings["BatchFailedCount"]);

                if (_batchFailedCount == batchFailedCount)
                {
                    _logger.Error($"{ex.Message} with port = {_port}");

                    Helpers.Helpers.SendNotification($"Exception Caught In OCRX Worker Process with port = {_port}", ex.Message);

                    _batchFailedCount = 0;

                    System.Environment.Exit(1); // terminate abnormally                   
                }
            }
            #endregion

            #region In Case OCR-X Server Is Down Then Sleep.

            if (updateDb)
            {
                totalTimeWatch.Stop();

                int totalTimeTakenByImageInMS = (int)(totalTimeWatch.ElapsedMilliseconds / _totalPlatesFound);

                totalTimeWatch.Reset();

                _logger.Info($"Going to update database for {_nodeName} and port = {_port}");

                //da.UpdateBatchBlankImageRejection(ref _batch, totalTimeTakenByImageInMS);

                DataAccess.UpdateBatchBlankImageRejection(ref _batch, totalTimeTakenByImageInMS, this._nodeName);

                ResetAndCleanup();

                _logger.Info($"Batch processing completed sucessfully for nodename = {_nodeName} and port = {_port}");
            }
            #endregion
        }
        #endregion

        #region Private Methods
        protected override void RetrievePlatesDetailsFromServer()
        {
            #region 1 - Get Images For Each Plate

            foreach (BatchInfo batch in _batch)
            {
                foreach (PlateInfo plate in batch.LicensePlates)
                {
                    byte[] originalImageBytes = new byte[3100000];
                    string fileName = string.Empty;
                    plate.ImageBytesRead = _mifManager.GetImage(ref originalImageBytes, plate.ImageFile, plate.ImageIndex, out fileName);

                    using (var imgStream = new MemoryStream(originalImageBytes, 0, plate.ImageBytesRead))
                    {
                        var image = new Bitmap(imgStream);
                        plate.ImageBytes = image.ToByteArray(ImageFormat.Jpeg);
                    }
                }
            }

            #endregion

            #region 2 - Get Plates Details From OCR-X Server

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            var processedPlates = SendRequest();

            watch.Stop();

            int averageTimeTakenByImage = (int)(watch.ElapsedMilliseconds / _totalPlatesFound);

            watch.Reset();

            #endregion

            #region 3 - After Getting Response Join The Request And Response And Contruct Plates Details            

            if (processedPlates != null && processedPlates.Count > 0)
            {
                foreach (BatchInfo batch in _batch)
                {

                    var fResult = processedPlates.Join(
                      batch.LicensePlates,
                      processItem => processItem.TransactionID,
                      unProcessItem => unProcessItem.TransactionID,
                      (processItem, unProcessItem) => new { Process = processItem, UnProcess = unProcessItem });

                    foreach (var item in fResult)
                    {
                        int conf = (int)Math.Floor(item.Process.ConfidenceLevel * 100);

                        item.UnProcess.RecognitionTypeID = (RecognitionType)Enum.Parse(typeof(RecognitionType), item.Process.Status.Trim());


                        //item.Process.Status.Trim() == PlateStatus.NoPlateFound.ToString() 
                        //   ? RecognitionType.NoPlateFound : RecognitionType.Missed;



                        //if (item.Process.Status == PlateStatus.PlateNotFound.ToString())
                        //{


                        //item.UnProcess.PlateDetail.RecognitionTypeID = conf >= Helpers.Helpers.BlankImageConfThreshold ? RecognitionType.NoPlateFound : RecognitionType.PlateFound;

                        //}
                        //else
                        //{
                        //   item.UnProcess.PlateDetail.RecognitionTypeID = RecognitionType.PlateFound;
                        //}

                        item.UnProcess.ConfidenceLevel = conf;

                        item.UnProcess.ProcessImageTime = averageTimeTakenByImage;
                        _logger.Info($"Item processed by {_nodeName} at port = {_port}: " +
                            $"biImgProcessedId = {item.UnProcess.TransactionID}, iYYYYMM = {item.UnProcess.YYYYMM}, " +
                            $"biBatchID = {item.UnProcess.BatchID}, tiRecognition = {item.UnProcess.RecognitionTypeID}, iPlateConfidence = {item.UnProcess}," +
                            $"ProcessingTime = {item.UnProcess.ProcessImageTime} ");
                    }
                }
            }
            else
            {
                _logger.Debug($"No plates found for processing for node = {_nodeName} and port = {_port}");
            }

            #endregion            
        }

        #endregion


    }
}
