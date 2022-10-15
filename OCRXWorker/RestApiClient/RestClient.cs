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


namespace Transcore.OCRXWorker.Shared
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
        public async Task<WrapperResult<T>> Post<T>(string url, string json)
        {
            WrapperResult<T> result = new WrapperResult<T>();

            using (HttpClient client = new HttpClient())
            {

                client.Timeout = new TimeSpan(0, _timeOut, 0); // set time out as max value , due to api time out
                client.BaseAddress = new Uri(_baseAddress);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var platesInfo = response.Content.ReadAsAsync<T>().Result; // read data from api
                    result.Data = platesInfo;
                    result.Status = response.StatusCode;
                    result.Message = response.ReasonPhrase;
                }
                else
                {
                    result.Data = default(T);
                    result.Message = response.Content.ReadAsStringAsync().Result;
                    result.Status = response.StatusCode;
                }
                return result;
            }
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
