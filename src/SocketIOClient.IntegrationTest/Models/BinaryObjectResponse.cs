using System.Text.Json.Serialization;

namespace SocketIOClient.IntegrationTest.Models
{
    class BinaryObjectResponse
    {
        [JsonPropertyName("data")]
        public byte[] Data { get; set; }
    }
}
