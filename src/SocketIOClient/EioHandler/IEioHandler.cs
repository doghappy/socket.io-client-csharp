using System.Threading.Tasks;

namespace SocketIOClient.EioHandler
{
    interface IEioHandler
    {
        Task IOConnectAsync(SocketIO io);
        void Unpack(SocketIO io, string text);
    }
}
