using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcore.OCRXInvoker.Shared
{
    public interface IRestClient
    {
        /// <summary>
        /// Get data from rest api.
        /// </summary>
        /// <returns></returns>
        Task<string> Get(string queryString);
    }
}
