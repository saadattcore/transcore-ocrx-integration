using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace Transcore.OCRXInvoker.Shared
{
    public class RestClient : IRestClient
    {
        #region Members
        public string _baseAddress;
        public int _timeOut;

        #endregion        

        #region Ctor

        public RestClient(string baseAddress, int timeOut)
        {
            _baseAddress = baseAddress;
            _timeOut = timeOut;
        }

        public RestClient(string baseAddress)
        {
            _baseAddress = baseAddress;
            _timeOut = 30;
        }

        #endregion

        #region Http methods

        public async Task<string> Get(string url)
        {
            string result = string.Empty;

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(_baseAddress);
                client.Timeout = new TimeSpan(0, _timeOut, 0);
                var response = client.GetAsync(url).Result;
                result = await response.Content.ReadAsStringAsync();

            }
            return result;
        }

        #endregion

    }

    public class WrapperResult<T>
    {
        public WrapperResult()
        {
            Data = default(T);
        }

        public T Data { get; set; }
        public HttpStatusCode Status { get; set; }
        public string Message { get; set; }
    }
}
