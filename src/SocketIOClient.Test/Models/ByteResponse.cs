using Newtonsoft.Json;

namespace SocketIOClient.Test.Models
{
    class ByteResponse
    {
        public string ClientSource { get; set; }

        public string Source { get; set; }

        [JsonProperty("bytes")]
        public byte[] Buffer { get; set; }
    }
}
