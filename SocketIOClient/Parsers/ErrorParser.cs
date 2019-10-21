using SocketIOClient.Arguments;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    class ErrorParser : IParser
    {
        public Task ParseAsync(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^44{rtp.Namespace}([\s\S]*)$");
            if (regex.IsMatch(rtp.Text))
            {
                var groups = regex.Match(rtp.Text).Groups;
                rtp.Socket.InvokeErrorEvent(new ResponseArgs
                {
                    Text = groups[1].Value,
                    RawText = rtp.Text
                });
            }
            return Task.CompletedTask;
        }
    }
}
