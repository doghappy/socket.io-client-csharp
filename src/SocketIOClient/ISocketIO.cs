using System;
using System.Threading.Tasks;

namespace SocketIOClient;

public interface ISocketIO : IDisposable
{
    SocketIOOptions Options { get; }
    Task ConnectAsync();
}