using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXWorker.Enums
{
    public enum RecognitionType : int
    {
        Init = -1,
        PlateNotFound = 0,
        Missed = 5
        
    }

    public enum BatchStatus : byte
    {
        Init = 0,
        InProgress = 1,
        Success = 2,
        Failed = 3
    }

    public enum PlateStatus
    {
        NoPlateFound,
        Missed
    }
}
