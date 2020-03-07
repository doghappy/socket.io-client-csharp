using SocketIOClient.Arguments;
using System.Text.RegularExpressions;

namespace SocketIOClient.Parsers
{
    class ErrorParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            var regex = new Regex($@"^44{rtp.Namespace}([\s\S]*)$");
            if (regex.IsMatch(rtp.Text))
            {
                var groups = regex.Match(rtp.Text).Groups;
                rtp.ErrorHandler(new ResponseArgs
                {
                    Text = groups[1].Value,
                    RawText = rtp.Text
                });
            }
        }
    }
}
