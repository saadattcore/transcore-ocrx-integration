using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXWorker.Enums;

namespace Transcore.OCRXWorker.Models
{
    public class PlateInfo
    {
        public PlateInfo()
        {
            this.RecognitionTypeID = RecognitionType.Init;
        }

        public long BatchID { get; set; }
        public int ProcessImageTime { get; set; }
        public long TransactionID { get; set; }
        public long ImageIndex { get; set; }
        public string ImageFile { get; set; }
        public int YYYYMM { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public string Status { get; set; }
        public RecognitionType RecognitionTypeID { get; set; }        
        public int ImageBytesRead { get; set; }
        public byte[] ImageBytes { get; set; }

    }

}
