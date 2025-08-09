using System;

namespace SocketIOClient.Core.Messages;

public interface IDataMessage : IMessage
{
    string Namespace { get; set; }
    public int Id { get; set; }

    T GetDataValue<T>(int index);
    object GetDataValue(Type type, int index);
}