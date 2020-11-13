using SocketIOClient.Packgers;
using System.Threading.Tasks;

namespace SocketIOClient.EioHandler
{
    interface IEioHandler : IUnpackable
    {
        Task IOConnectAsync(SocketIO io);
    }
}
