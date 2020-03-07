using Newtonsoft.Json;
using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    class OpenedParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            if (rtp.Text.StartsWith("0{\"sid\":\""))
            {
                string message = rtp.Text.TrimStart('0');
                var args = JsonConvert.DeserializeObject<OpenedArgs>(message);
                rtp.OpenHandler(args);
            }
            else
            {
                rtp.Parser = new ConnectedParser();
                rtp.Parse();
            }
        }
    }
}
