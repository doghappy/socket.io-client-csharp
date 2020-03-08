using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SocketIOClient.Arguments;

namespace SocketIOClient.Parsers
{
    class MessageEventParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^42{rtp.Namespace}\d*\[(.+)\]$");
            if (regex.IsMatch(rtp.Text))
            {
                var groups = regex.Match(rtp.Text).Groups;
                string text = groups[1].Value;
                var formatter = new DataFormatter();
                var data = formatter.Format(text);
                var eventHandlerArg = new ResponseArgs { RawText = rtp.Text };
                string eventName = JsonConvert.DeserializeObject<string>(data[0]);
                if (data.Count > 1)
                    eventHandlerArg.Text = data[1];
                if (rtp.Socket.EventHandlers.ContainsKey(data[0]))
                {
                    var handlerBox = rtp.Socket.EventHandlers[data[0]];
                    handlerBox.EventHandler?.Invoke(eventHandlerArg);
                    if (handlerBox.EventHandlers != null)
                    {
                        for (int i = 0; i < handlerBox.EventHandlers.Count; i++)
                        {
                            var arg = new ResponseArgs { RawText = rtp.Text };
                            if (i + 2 <= data.Count - 1)
                                arg.Text = data[i + 2];
                            handlerBox.EventHandlers[i](arg);
                        }
                    }
                }
                else
                {
                    rtp.UncaughtHandler(eventName, eventHandlerArg);
                }
                rtp.ReceiveHandler(eventName, eventHandlerArg);
            }
            else
            {
                rtp.Parser = new MessageAckParser();
                rtp.Parse();
            }
        }
    }
}
