using System.Text.Json.Serialization;

namespace SocketIOClient.Test.Models
{
    class BytesInObjectResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public byte[] Message { get; set; }
    }
}
