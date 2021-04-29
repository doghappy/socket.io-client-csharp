using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SocketIOClient.JsonConverters
{
    public class DefaultSerializer : IJsonSerializer
    {
        public string SerializeObject<T>(T data, IList<byte[]> outgoingBytes)
        {
            return JsonConvert.SerializeObject(data, new ByteArrayConverter
            {
                Client = _client,
                BinaryBytes = outgoingBytes
            });
        }

        public T DeserializeObject<T>(string jsonData, IList<byte[]> incomingBytes)
        {
            return JsonConvert.DeserializeObject<T>(jsonData, new ByteArrayConverter
            {
                Client = _client,
                BinaryBytes = incomingBytes
            });
        }

        public void Initialize(SocketIO client)
        {
            Debug.Assert(client != null);

            _client = client;
        }

        SocketIO _client;
    }
}
