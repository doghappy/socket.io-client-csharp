using Newtonsoft.Json;
using SocketIOClient.Arguments;
using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    class OpenedParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            if (rtp.Text.StartsWith("0{\"sid\":\""))
            {
                string message = rtp.Text.TrimStart('0');
                var args = JsonConvert.DeserializeObject<OpenedArgs>(message);
                return rtp.Socket.InvokeOpenedAsync(args);
            }
            else
            {
                rtp.Parser = new ConnectedParser();
                return rtp.ParseAsync();
            }
        }
    }
}
