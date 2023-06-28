using System;
using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Serializer.Core
{
    public interface ISerializer
    {
        // string Serialize(object data);
        List<SerializedItem> Serialize(string eventName, int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(string eventName, string ns, object[] data);
        T Deserialize<T>(IMessage2 message, int index);
        IMessage2 Deserialize(EngineIO eio, string text);
        IMessage2 Deserialize(EngineIO eio, byte[] bytes);
        string MessageToJson(IMessage2 message);
        IMessage2 NewMessage(MessageType type);

        SerializedItem SerializeConnectedMessage(
            string ns, 
            EngineIO eio, 
            object auth,
            IEnumerable<KeyValuePair<string, string>> queries);
        
        SerializedItem SerializePingMessage();
        SerializedItem SerializePongMessage();
    }
}