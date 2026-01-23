using System.Collections.Generic;

namespace SocketIOClient.Serializer
{
    public class SerializationResult
    {
        public string Json { get; set; } = null!;
        public ICollection<byte[]> Bytes { get; set; } = [];
    }
}