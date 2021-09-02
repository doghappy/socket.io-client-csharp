using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient
{
    public class Payload
    {
        public string Event { get; set; }
        public object[] Params { get; set; }
    }
}
