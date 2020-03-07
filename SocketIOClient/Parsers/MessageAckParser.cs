using SocketIOClient.Arguments;
using System.Text.RegularExpressions;

namespace SocketIOClient.Parsers
{
    class MessageAckParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
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
            }
            else
            {
                rtp.Parser = new ErrorParser();
                rtp.Parse();
            }
        }
    }
}
