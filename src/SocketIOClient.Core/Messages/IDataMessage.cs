using System;

namespace SocketIOClient.Core.Messages;

public interface IDataMessage : IMessage
{
    string Namespace { get; set; }
    public int Id { get; set; }

    T GetValue<T>(int index);
    object GetValue(Type type, int index);
}