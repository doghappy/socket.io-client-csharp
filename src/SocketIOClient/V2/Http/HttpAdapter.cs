using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Http;

public class HttpAdapter(IHttpClient httpClient) : IHttpAdapter
{
    private readonly List<IMessageObserver> _observers = [];

    public async Task SendAsync(IMessage message)
    {
        // TODO
        // 1. Serialize
        // 2. Send
        // 3. Deserialize
        // 4. Notify observers
        await httpClient.SendAsync(new HttpRequest());
        foreach (var observer in _observers)
        {
            observer.OnNext(new OpenedMessage());
        }
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    // public void OnNext(IHttpResponse response)
    // {
    //     throw new NotImplementedException();
    // }

    public void Subscribe(IMessageObserver observer)
    {
        _observers.Add(observer);
    }
}