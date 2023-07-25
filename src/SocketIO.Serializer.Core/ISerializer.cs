using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Serializer.Core
{
    public interface ISerializer
    {
        // string Serialize(object data);
        List<SerializedItem> Serialize(string eventName, int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(int packetId, string nsp, object[] data);
        List<SerializedItem> Serialize(string eventName, string nsp, object[] data);
        T Deserialize<T>(IMessage message, int index);
        IMessage Deserialize(string text);
        IMessage Deserialize(byte[] bytes);
        string MessageToJson(IMessage message);
        IMessage NewMessage(MessageType type);

        SerializedItem SerializeConnectedMessage(string ns, object auth, IEnumerable<KeyValuePair<string, string>> queries);
        
        SerializedItem SerializePingMessage();
        SerializedItem SerializePongMessage();
    }
}