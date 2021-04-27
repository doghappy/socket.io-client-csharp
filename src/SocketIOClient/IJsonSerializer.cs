using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIOClient
{
    public interface IJsonSerializer
    {
        void Initialize(SocketIO client);

        // Serialize data object into json string, binary data can be added to outgoingBytes chunks.
        string SerializeObject<T>(T data, IList<byte[]> outgoingBytes);

        // Deserialize json string into data object, binary data can be found in incomingBytes chunks.
        T DeserializeObject<T>(string jsonData, IList<byte[]> incomingBytes);
    }
}
