using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    class MessageEventParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^42{rtp.Namespace}\d*\[""([*\s\w-]+)"",([\s\S]*)\]$");
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
                    rtp.Socket.InvokeUnhandledEvent(eventName, args);
                }
                rtp.Socket.InvokeReceivedEvent(eventName, args);
                return Task.CompletedTask;
            }
            else
            {
                rtp.Parser = new MessageAckParser();
                return rtp.ParseAsync();
            }
        }
    }
}
