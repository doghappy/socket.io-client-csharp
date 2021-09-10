using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SocketIOClient.Transport
{
    class HandshakeInfo
    {
        [JsonPropertyName("sid")]
        public string Sid { get; set; }

        [JsonPropertyName("upgrades")]
        public List<string> Upgrades { get; set; }

        [JsonPropertyName("pingInterval")]
        public int PingInterval { get; set; }

        [JsonPropertyName("pingTimeout")]
        public int PingTimeout { get; set; }
    }
}
