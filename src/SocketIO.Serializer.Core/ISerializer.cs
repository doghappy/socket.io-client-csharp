using System.Collections.Generic;
using SocketIO.Core;

namespace SocketIO.Serializer.Core
{
    public interface ISerializer
    {
        // string Serialize(object data);
        List<SerializedItem> Serialize(EngineIO eio, string eventName, int packetId, string ns, object[] data);
        List<SerializedItem> Serialize(EngineIO eio, int packetId, string nsp, object[] data);
        List<SerializedItem> Serialize(EngineIO eio, string eventName, string nsp, object[] data);
        T Deserialize<T>(IMessage message, int index);
        IMessage Deserialize(EngineIO eio, string text);
        IMessage Deserialize(EngineIO eio, byte[] bytes);
        string MessageToJson(IMessage message);
        IMessage NewMessage(MessageType type);

        SerializedItem SerializeConnectedMessage(EngineIO eio, string ns, object auth, IEnumerable<KeyValuePair<string, string>> queries);

        SerializedItem SerializePingMessage();
        SerializedItem SerializePingProbeMessage();
        SerializedItem SerializePongMessage();
        SerializedItem SerializeUpgradeMessage();
    }
}