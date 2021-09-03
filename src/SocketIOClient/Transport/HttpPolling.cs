using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    public class HttpPolling : IDisposable
    {
        public HttpPolling()
        {
            _httpClient = new HttpClient();
        }

        readonly HttpClient _httpClient;


        public async Task ConnectAsync(Uri uri)
        {
            //http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling&t=Nkfn3sk
            string text = await _httpClient.GetStringAsync(uri.ToString());
        }

        public async Task SendAsync(string text)
        {

        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
