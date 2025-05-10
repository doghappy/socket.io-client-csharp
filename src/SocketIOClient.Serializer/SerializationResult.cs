using System.Collections.Generic;

namespace SocketIOClient.Serializer
{
    public class SerializationResult
    {
        public string Json { get; set; }
        public ICollection<byte[]> Bytes { get; set; }
    }
}