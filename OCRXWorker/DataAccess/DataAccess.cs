using log4net;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Transcore.OCRXWorker.Models;
using Transcore.OCRXWorker.DAL;
using Transcore.OCRXWorker.Helpers;

namespace Transcore.OCRXWorker.DAL
{
    public class DataAccess
    {
        private static readonly string _dbLaneConnString;
        private static readonly string _dbDTSConnString;
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Database _dbLaneMsg = null;

        public DataAccess()
        {
        }

        static DataAccess()
        {
            _dbLaneConnString = "LaneMsg";
            _dbDTSConnString = "DTS";
        }


        public static DataSet GetLookups()
        {
            _logger.Info("Loading lookups");

            DataSet lookupDS = new DataSet();

            DbCommand cmdLookups = null;

            Database dbLaneDb = DatabaseFactory.CreateDatabase(_dbLaneConnString);

            cmdLookups = dbLaneDb.GetStoredProcCommand("uspGetCarmenPlateInformationFromDTS");

            lookupDS = dbLaneDb.ExecuteDataSet(cmdLookups);

            if (lookupDS == null || lookupDS.Tables.Count == 0)
            {
                throw new Exception("Failed to retrieve Plate Information from dbDTS");
            }


            Database dbDTS = DatabaseFactory.CreateDatabase(_dbDTSConnString);

            cmdLookups = dbDTS.GetStoredProcCommand("uspSTBPlateCategAllGet");

            var tmpDS = dbDTS.ExecuteDataSet(cmdLookups);

            if (tmpDS == null || tmpDS.Tables.Count == 0)
            {
                throw new Exception("Failed to retrieve Plate Categories from dbDTS");
            }

            DataTable categoryTable = tmpDS.Tables[0].Copy();

            categoryTable.TableName = "CategoryTable";

            lookupDS.Tables.Add(categoryTable);

            _logger.Info("Complete loading lookups");

            return lookupDS;
        }

        public static List<BatchInfo> GetBatch(string nodeName, out int plateCount)
        {
            plateCount = 0;

            _logger.Info("Fetching next available batch for processing");

            _dbLaneMsg = DatabaseFactory.CreateDatabase(_dbLaneConnString);

            DataSet ds = new DataSet();

            DbCommand cmd = null;

            cmd = _dbLaneMsg.GetStoredProcCommand("uspOCRXImageGet");

            _dbLaneMsg.AddInParameter(cmd, "@ipv_vcNodeName", DbType.String, nodeName);

            try
            {
                ds = _dbLaneMsg.ExecuteDataSet(cmd);
            }
            catch (Exception ex)
            {

                _logger.Error(ex.Message);
            }
            

            if (ds.Tables.Count == 0)
            {
                _logger.Warn("No records found");
                return null;
            }

            if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0)
            {
                _logger.Warn("Plates and batch association not found");
                return null;
            }

            List<BatchInfo> batch = ds.ToBatchInfo(out plateCount);
            return batch;
        }

      

        public static void UpdateBatchBlankImageRejection(ref List<BatchInfo> batchList, int totalAverageTimeTakenByImage, string nodeName)
        {
            #region Prepare Data Tables
            DataTable dtBatch = new DataTable();
            dtBatch.Columns.Add(new DataColumn("biBatchId", typeof(Int64)));
            dtBatch.Columns.Add(new DataColumn("tiStatusId", typeof(byte)));

            DataTable dtPlateInfo = new DataTable();
            dtPlateInfo.Columns.Add(new DataColumn("biImgProcessedID", typeof(Int64)));
            dtPlateInfo.Columns.Add(new DataColumn("tiRecognitionTypeId", typeof(byte)));
            dtPlateInfo.Columns.Add(new DataColumn("vcRecognitionResult", typeof(string)));            
            dtPlateInfo.Columns.Add(new DataColumn("iProcessTimeImage", typeof(Int32)));
            dtPlateInfo.Columns.Add(new DataColumn("iProcessTimeTotal", typeof(Int32)));
            dtPlateInfo.Columns.Add(new DataColumn("iPlateConfidence", typeof(Int32)));
            dtPlateInfo.Columns.Add(new DataColumn("biBatchId", typeof(Int64)));
            dtPlateInfo.Columns.Add(new DataColumn("iYYYYMM", typeof(Int32)));


            foreach (BatchInfo batch in batchList)
            {

                DataRow row = dtBatch.NewRow();
                row["biBatchId"] = batch.BatchID;
                row["tiStatusId"] = (byte)Enum.Parse(typeof(Enums.BatchStatus), batch.Status.ToString());
                dtBatch.Rows.Add(row);

                foreach (PlateInfo plate in batch.LicensePlates)
                {
                    DataRow pRow = dtPlateInfo.NewRow();

                    pRow["biImgProcessedID"] = plate.TransactionID;
                    pRow["tiRecognitionTypeId"] = Convert.ToByte((int)(Enum.Parse(typeof(Transcore.OCRXWorker.Enums.RecognitionType), plate.RecognitionTypeID.ToString())));
                    pRow["vcRecognitionResult"] = plate.RecognitionTypeID == Enums.RecognitionType.PlateNotFound ? "No Plate Found" : "Missed";
                    pRow["iProcessTimeImage"] = plate.ProcessImageTime;
                    pRow["iProcessTimeTotal"] = totalAverageTimeTakenByImage;
                    pRow["iPlateConfidence"] = plate.ConfidenceLevel;
                    pRow["biBatchId"] = plate.BatchID;
                    pRow["iYYYYMM"] = plate.YYYYMM;

                    dtPlateInfo.Rows.Add(pRow);
                }
            }
            #endregion


            DbCommand cmd = null;
            cmd = _dbLaneMsg.GetStoredProcCommand("uspOCRXResultIns");

            SqlParameter paramPlates = new SqlParameter();
            paramPlates.ParameterName = "@ipv_ttOCRXImgProcessed";
            paramPlates.SqlDbType = SqlDbType.Structured;
            paramPlates.Value = dtPlateInfo;

            SqlParameter paramBatch = new SqlParameter();
            paramBatch.ParameterName = "@ipv_ttOCRXBatch";
            paramBatch.SqlDbType = SqlDbType.Structured;
            paramBatch.Value = dtBatch;

            SqlParameter paramNodeName = new SqlParameter();
            paramNodeName.ParameterName = "@ipv_vcNodeName";
            paramNodeName.SqlDbType = SqlDbType.VarChar;
            paramNodeName.Value = nodeName;

            cmd.Parameters.Add(paramPlates);
            cmd.Parameters.Add(paramBatch);
            cmd.Parameters.Add(paramNodeName);
            int r = _dbLaneMsg.ExecuteNonQuery(cmd);
        }

        public static void PingNodes(List<Transcore.OCRXWorker.Models.OCRXNode> nodes)
        {
            Database dbLaneDb = DatabaseFactory.CreateDatabase(_dbLaneConnString);           

            DataTable dtNode = new DataTable();
            //dtNode.Columns.Add(new DataColumn("biNodeId", typeof(Int16)));
            dtNode.Columns.Add(new DataColumn("vcNodeName", typeof(string)));
            dtNode.Columns.Add(new DataColumn("tiWorkerOsId", typeof(Int16)));
            dtNode.Columns.Add(new DataColumn("tiServerOsId", typeof(Int16)));
            dtNode.Columns.Add(new DataColumn("bIsActive", typeof(bool)));
            dtNode.Columns.Add(new DataColumn("tiServerPort", typeof(short)));

            foreach (var node in nodes)
            {
                DataRow dataRow = dtNode.NewRow();

                dataRow["vcNodeName"] = node.NodeName;
                dataRow["tiWorkerOsId"] = node.WorkerID;
                dataRow["tiServerOsId"] = node.ServerID;
                dataRow["bIsActive"] = node.IsServerActive;
                dataRow["tiServerPort"] = node.Port;

                dtNode.Rows.Add(dataRow);
            }

            var command = dbLaneDb.GetStoredProcCommand("uspPingOCRXNode");

            SqlParameter paramNodes = new SqlParameter();
            paramNodes.ParameterName = "@ipv_ttOCRXNodes";
            paramNodes.SqlDbType = SqlDbType.Structured;
            paramNodes.Value = dtNode;

            command.Parameters.Add(paramNodes);

            int result = _dbLaneMsg.ExecuteNonQuery(command);

        }

        public static int GetOCRXPingMinues()
        {
            _dbLaneMsg = DatabaseFactory.CreateDatabase(_dbLaneConnString);
            DbCommand _cmdGetApplParamValue = _dbLaneMsg.GetSqlStringCommand("SELECT dbo.fnGetApplParamValue(@ipv_vcParamName)");

            _dbLaneMsg.AddInParameter(_cmdGetApplParamValue, "@ipv_vcParamName", DbType.String, "OCRXInstancesPingMinutes");


            string applParamValue = _dbLaneMsg.ExecuteScalar(_cmdGetApplParamValue)?.ToString();
            int result = 0;

            return int.TryParse(applParamValue, out result) ? result : 0;

        }

    }
}
