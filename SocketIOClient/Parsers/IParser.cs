using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.Parsers
{
    public interface IParser
    {
        JObject Parse(string text);

        bool Check(string text);
    }
}
