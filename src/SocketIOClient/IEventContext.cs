using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;

namespace SocketIOClient;

public interface IEventContext
{
    T GetDataValue<T>(int index);
    object GetDataValue(Type type, int index);
    Task SendAckDataAsync(IEnumerable<object> data);
    Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken);
}

public class EventContext(IDataMessage message, IInternalSocketIO io) : IEventContext
{
    public async Task SendAckDataAsync(IEnumerable<object> data, CancellationToken cancellationToken)
    {
        await io.SendAckDataAsync(message.Id, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAckDataAsync(IEnumerable<object> data)
    {
        await SendAckDataAsync(data, CancellationToken.None).ConfigureAwait(false);
    }

    public T GetDataValue<T>(int index)
    {
        return message.GetValue<T>(index);
    }

    public object GetDataValue(Type type, int index)
    {
        return message.GetValue(type, index);
    }
}