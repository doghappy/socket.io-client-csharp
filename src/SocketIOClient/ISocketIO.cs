using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Common.Messages;

namespace SocketIOClient;

public interface ISocketIO : IDisposable
{
    SocketIOOptions Options { get; }
    string? Id { get; }
    bool Connected { get; }
    Task ConnectAsync();
    Task ConnectAsync(CancellationToken cancellationToken);
    Task EmitAsync(string eventName, IEnumerable<object> data, CancellationToken cancellationToken);
    Task EmitAsync(string eventName, IEnumerable<object> data);
    Task EmitAsync(string eventName, CancellationToken cancellationToken);
    Task EmitAsync(string eventName);

    Task EmitAsync(
        string eventName,
        IEnumerable<object> data,
        Func<IDataMessage, Task> ack,
        CancellationToken cancellationToken);

    Task EmitAsync(string eventName, IEnumerable<object> data, Func<IDataMessage, Task> ack);
    Task EmitAsync(string eventName, Func<IDataMessage, Task> ack, CancellationToken cancellationToken);
    Task EmitAsync(string eventName, Func<IDataMessage, Task> ack);
    Task DisconnectAsync();
    Task DisconnectAsync(CancellationToken cancellationToken);
    void On(string eventName, Func<IEventContext, Task> handler);
    void OnAny(Func<string, IEventContext, Task> handler);
    IEnumerable<Func<string, IEventContext, Task>> ListenersAny { get; }
    void OffAny(Func<string, IEventContext, Task> handler);
    void PrependAny(Func<string, IEventContext, Task> handler);
    void Once(string eventName, Func<IEventContext, Task> handler);
}