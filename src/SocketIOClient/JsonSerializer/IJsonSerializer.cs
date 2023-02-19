using System;
using System.Collections.Generic;

namespace SocketIOClient.JsonSerializer
{
    public interface IJsonSerializer
    {
        JsonSerializeResult Serialize(object[] data);
        T Deserialize<T>(string json);
        object Deserialize(string json, Type type);
        T Deserialize<T>(string json, IList<byte[]> incomingBytes);
        object Deserialize(string json, Type type, IList<byte[]> incomingBytes);
    }
}