using System.Threading.Tasks;
using SocketIOClient.Transport.Http;
using SocketIOClient.V2.Http;
using SocketIOClient.V2.Session;

namespace SocketIOClient.V2;

public interface ISocketIO
{
    IHttpClient HttpClient { get; set; }
    ISession Session { get; set; }
    Task ConnectAsync();
    Task EmitAsync(string eventName);
}