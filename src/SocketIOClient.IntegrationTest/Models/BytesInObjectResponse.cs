using System.Text.Json.Serialization;

namespace SocketIOClient.IntegrationTest.Models
{
    class BytesInObjectResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public byte[] Message { get; set; }
    }
}
