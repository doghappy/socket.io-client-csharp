using System.Threading.Tasks;
using SocketIOClient.V2.Http;
using SocketIOClient.V2.WebSocket;

namespace SocketIOClient.V2;

public class SocketIO:ISocketIO
{
    public IHttpClient HttpClient { get; set; }

    public Task ConnectAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task EmitAsync(string eventName)
    {
        throw new System.NotImplementedException();
    }
}