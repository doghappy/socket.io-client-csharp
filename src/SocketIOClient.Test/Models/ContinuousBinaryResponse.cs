using System.Text.Json.Serialization;

namespace SocketIOClient.Test.Models
{
    class ContinuousBinaryResponse
    {
        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }
    }
}
