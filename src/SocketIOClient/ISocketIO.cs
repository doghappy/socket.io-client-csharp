using System.Threading.Tasks;

namespace SocketIOClient;

public interface ISocketIO
{
    SocketIOOptions Options { get; }
    Task ConnectAsync();
}