using System;
using System.Text.Json;
using System.Collections.Generic;

namespace SocketIOClient.Converters
{
    public class OpenedMessage : ICvtMessage
    {
        public CvtMessageType Type => CvtMessageType.Opened;

        public string Sid { get; set; }

        public List<string> Upgrades { get; private set; }

        public int PingInterval { get; private set; }

        public int PingTimeout { get; private set; }

        public void Read(string msg)
        {
            var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;
            Sid = root.GetProperty("sid").GetString();
            PingInterval = root.GetProperty("pingInterval").GetInt32();
            PingTimeout = root.GetProperty("pingTimeout").GetInt32();
        }

        public string Write()
        {
            throw new NotImplementedException();
        }
    }
}
