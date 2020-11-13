using System.Threading.Tasks;

namespace SocketIOClient.EioHandler
{
    class Eio4Handler : IEioHandler
    {
        public async Task IOConnectAsync(SocketIO io)
        {
            await io.Socket.SendMessageAsync("40" + io.Namespace);
        }

        public void Unpack(SocketIO io, string text)
        {
            if (string.IsNullOrEmpty(io.Namespace))
            {
                if (io.Options.EIO == 4)
                {
                    io.Id = text;
                }
            }
            else
            {
                if (io.Options.EIO == 4)
                {
                    io.Id = text.Substring(io.Namespace.Length);
                }
            }
            io.InvokeConnect();
        }
    }
}
