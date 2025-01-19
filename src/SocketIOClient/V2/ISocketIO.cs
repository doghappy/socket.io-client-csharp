using System.Threading.Tasks;
using SocketIOClient.V2.Http;

namespace SocketIOClient.V2;

public interface ISocketIO
{
    IHttpClient HttpClient { get; set; }
    Task ConnectAsync();
    Task EmitAsync(string eventName);
}