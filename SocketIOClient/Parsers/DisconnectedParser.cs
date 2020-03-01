using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    class DisconnectedParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            if (rtp.Text == "41" + rtp.Namespace)
            {
                rtp.CloseHandler();
                return Task.CompletedTask;
            }
            else
            {
                rtp.Parser = new MessageEventParser();
                return rtp.ParseAsync();
            }
        }
    }
}
