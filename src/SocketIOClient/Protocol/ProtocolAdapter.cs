using System.Collections.Generic;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Observers;

namespace SocketIOClient.Protocol;

public abstract class ProtocolAdapter : IProtocolAdapter
{
    private readonly List<IMyObserver<ProtocolMessage>> _observers = [];

    public void Subscribe(IMyObserver<ProtocolMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    public async Task OnNextAsync(ProtocolMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public abstract void SetDefaultHeader(string name, string value);
}