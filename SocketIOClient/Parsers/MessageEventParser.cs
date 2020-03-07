using System.Text.RegularExpressions;
using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    class MessageEventParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^42{rtp.Namespace}\d*\[""([*\s\w-]+)"",?([\s\S]*)\]$");
            if (regex.IsMatch(rtp.Text))
            {
                var groups = regex.Match(rtp.Text).Groups;
                string eventName = groups[1].Value;
                var args = new ResponseArgs
                {
                    Text = groups[2].Value,
                    RawText = rtp.Text
                };
                if (rtp.Socket.EventHandlers.ContainsKey(eventName))
                {
                    var handler = rtp.Socket.EventHandlers[eventName];
                    handler(args);
                }
                else
                {
                    rtp.UncaughtHandler(eventName, args);
                }
                rtp.ReceiveHandler(eventName, args);
            }
            else
            {
                rtp.Parser = new MessageAckParser();
                rtp.Parse();
            }
        }
    }
}
