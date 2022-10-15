using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcore.OCRXWorker.Enums;

namespace Transcore.OCRXWorker.Models
{
    public class BatchInfo
    {
        public long BatchID { get; set; }
        public BatchStatus Status { get; set; }
        public List<PlateInfo> LicensePlates { get; set; }
    }
}
