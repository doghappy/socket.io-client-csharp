using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    class MessageAckParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^43{rtp.Namespace}(\d+)\[([\s\S]*)\]$");
            if (regex.IsMatch(rtp.Text))
            {
                var groups = regex.Match(rtp.Text).Groups;
                int packetId = int.Parse(groups[1].Value);
                if (rtp.Socket.Callbacks.ContainsKey(packetId))
                {
                    var handler = rtp.Socket.Callbacks[packetId];
                    handler(new ResponseArgs
                    {
                        Text = groups[2].Value,
                        RawText = rtp.Text
                    });
                    rtp.Socket.Callbacks.Remove(packetId);
                }
                return Task.CompletedTask;
            }
            else
            {
                rtp.Parser = new ErrorParser();
                return rtp.ParseAsync();
            }
        }
    }
}
