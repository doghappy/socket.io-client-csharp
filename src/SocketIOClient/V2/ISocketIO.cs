using System.Threading.Tasks;

namespace SocketIOClient.V2;

public interface ISocketIO
{
    SocketIOOptions Options { get; }
    Task ConnectAsync();
}