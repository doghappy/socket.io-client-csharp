using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SocketIOClient
{
    public class SocketIOMessage
    {
        public string Event { get; set; }
        public List<JsonElement> JsonElements { get; set; }

    }
}
