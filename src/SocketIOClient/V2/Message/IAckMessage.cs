using System;

namespace SocketIOClient.V2.Message;

public interface IAckMessage : IMessage
{
    string Namespace { get; set; }
    public int Id { get; set; }

    T GetDataValue<T>(int index);
    object GetDataValue(Type type, int index);
}