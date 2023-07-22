using System;
using SocketIOClient.IntegrationTests.Utils;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    public abstract class HttpTests : SocketIOTests
    {
        protected override TransportProtocol Transport => TransportProtocol.Polling;
        
        protected SocketIOOptions CreateOptions()
        {
            return new SocketIOOptions
            {
                EIO = EIO,
                AutoUpgrade = false,
            };
        }

        protected override SocketIO CreateSocketIO()
        {
            var options = CreateOptions();
            options.Reconnection = false;
            options.EIO = EIO;
            options.ConnectionTimeout = TimeSpan.FromSeconds(2);
            var io = new SocketIO(ServerUrl, options);
            io.SetMessagePackSerializer();
            return io;
        }
    }
}