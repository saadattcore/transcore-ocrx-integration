using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXWorker.Models
{
    public class ApiRequestModel
    {
        public long TransactionID { get; set; }
        public string Ext { get; set; }
        public string ImageData { get; set; }
    }
}
