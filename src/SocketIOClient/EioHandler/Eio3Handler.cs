using System.Text;
using System.Collections.Generic;

namespace SocketIOClient.EioHandler
{
    public class Eio3Handler : IEioHandler
    {
        public string CreateConnectionMessage(string @namespace, Dictionary<string, string> query)
        {
            var builder = new StringBuilder();
            builder.Append("40");

            if (@namespace != null)
            {
                builder.Append(@namespace);
            }
            if (query != null && query.Count > 0)
            {
                builder.Append('?');
                int index = -1;
                foreach (var item in query)
                {
                    index++;
                    builder
                        .Append(item.Key)
                        .Append('=')
                        .Append(item.Value);
                    if (index < query.Count - 1)
                    {
                        builder.Append('&');
                    }
                }
            }
            if (@namespace != null)
            {
                builder.Append(',');
            }
            return builder.ToString();
        }

        public ConnectionResult CheckConnection(string @namespace, string text)
        {
            var result = new ConnectionResult();
            if (string.IsNullOrEmpty(@namespace))
            {
                result.Result = text == string.Empty;
            }
            else
            {
                result.Result = text.StartsWith(@namespace);
            }
            return result;
        }

        public string GetErrorMessage(string text)
        {
            return text.Trim('"');
        }
    }
}
