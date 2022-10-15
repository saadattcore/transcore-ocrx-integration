using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXWorker.Shared
{
    public interface IRestClient
    {
        Task<WrapperResult<T>> Post<T>(string url, string data);
    }
}
