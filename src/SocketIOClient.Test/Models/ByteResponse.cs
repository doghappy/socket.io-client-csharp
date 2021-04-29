using System.Text.Json.Serialization;

namespace SocketIOClient.Test.Models
{
    class ByteResponse
    {
        [JsonPropertyName("clientSource")]
        public string ClientSource { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("bytes")]
        public byte[] Buffer { get; set; }
    }
}
