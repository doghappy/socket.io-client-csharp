using System;

namespace SocketIOClient.Common.Messages;

public interface IDataMessage : IMessage
{
    string? Namespace { get; set; }
    int Id { get; set; }
    string RawText { get; set; }

    T? GetValue<T>(int index);
    object? GetValue(Type type, int index);
}