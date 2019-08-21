using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    class ConnectedParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            if (rtp.Text == "40" + rtp.Namespace)
            {
                return rtp.Socket.InvokeConnectedAsync();
            }
            else if (rtp.Text == "40")
            {
                return Task.CompletedTask;
            }
            else
            {
                rtp.Parser = new DisconnectedParser();
                return rtp.ParseAsync();
            }
        }
    }
}
