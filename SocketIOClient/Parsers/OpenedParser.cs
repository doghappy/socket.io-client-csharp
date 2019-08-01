using Newtonsoft.Json.Linq;

namespace SocketIOClient.Parsers
{
    class OpenedParser : IParser
    {
        public bool Check(string text) => text.StartsWith("0{\"sid\":\"");

        public JObject Parse(string text)
        {
            string message = text.TrimStart('0');
            return JObject.Parse(message);
        }
    }
}
