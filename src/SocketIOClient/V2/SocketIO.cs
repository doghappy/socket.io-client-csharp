using System.Threading.Tasks;
using SocketIOClient.Transport.Http;
using SocketIOClient.V2.Session;

namespace SocketIOClient.V2;

public class SocketIO : ISocketIO
{
    public IHttpClient HttpClient { get; set; }
    public ISession Session { get; set; }

    public Task ConnectAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task EmitAsync(string eventName)
    {
        throw new System.NotImplementedException();
    }
}