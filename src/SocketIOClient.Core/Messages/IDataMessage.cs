using System;

namespace SocketIOClient.Core.Messages;

public interface IDataMessage : IMessage
{
    string? Namespace { get; set; }
    int Id { get; set; }
    string RawText { get; set; }

    T? GetValue<T>(int index);
    object? GetValue(Type type, int index);
}