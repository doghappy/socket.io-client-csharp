using System.Text.Json.Serialization;

namespace SocketIOClient.Test.Models
{
    class BinaryObjectResponse
    {
        [JsonPropertyName("data")]
        public byte[] Data { get; set; }
    }
}
