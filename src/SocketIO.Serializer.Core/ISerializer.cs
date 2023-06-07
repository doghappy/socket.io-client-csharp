using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Serializer.Core
{
    public interface ISerializer
    {
        List<SerializedItem> Serialize(object data);
        List<SerializedItem> Serialize(string eventName, int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(string eventName, string ns, object[] data);
        T Deserialize<T>(IMessage2 message, int index);
        object Deserialize(string json, Type type);
        T Deserialize<T>(string json, IEnumerable<byte[]> incomingBytes);
        object Deserialize(string json, Type type, IEnumerable<byte[]> incomingBytes);
        IMessage2 Deserialize(EngineIO eio, string text);
        string MessageToJson(IMessage2 message);
        IMessage2 NewMessage(MessageType type);

        SerializedItem SerializeConnectedMessage(
            string ns, 
            EngineIO eio, 
            string auth,
            IEnumerable<KeyValuePair<string, string>> queries);
    }
}