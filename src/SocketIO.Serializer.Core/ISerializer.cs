using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Serializer.Core
{
    public interface ISerializer
    {
        List<SerializedItem> Serialize(object data);
        List<SerializedItem> Serialize(long packetId, string ns, EngineIO eio, object[] data);
        List<SerializedItem> Serialize(string eventName, string ns, object[] data);
        T Deserialize<T>(string json);
        object Deserialize(string json, Type type);
        T Deserialize<T>(string json, IEnumerable<byte[]> incomingBytes);
        object Deserialize(string json, Type type, IEnumerable<byte[]> incomingBytes);
        Message Deserialize(EngineIO eio, string text);
        string GetEventName(Message message);
        string MessageToJson(Message message);
    }
}