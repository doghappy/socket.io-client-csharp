using System.Text;
using System.Text.Json;
using System.Collections.Generic;
namespace SocketIOClient.EioHandler
{
    public class Eio4Handler : IEioHandler
    {
        public string CreateConnectionMessage(string @namespace, Dictionary<string, string> query)
        {
            var builder = new StringBuilder();
            builder.Append("40");

            if (@namespace != null)
            {
                builder.Append(@namespace).Append(',');
            }

            return builder.ToString();
        }

        public ConnectionResult CheckConnection(string @namespace, string text)
        {
            if (!string.IsNullOrEmpty(@namespace))
            {
                text = text.Substring(@namespace.Length + 1);
            }
            return new ConnectionResult
            {
                Result = true,
                Id = JsonDocument.Parse(text).RootElement.GetProperty("sid").GetString()
            };
        }

        public string GetErrorMessage(string text)
        {
            var doc = JsonDocument.Parse(text);
            return doc.RootElement.GetProperty("message").GetString();
        }
    }
}
