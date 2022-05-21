using Microsoft.Extensions.Logging;

namespace SocketIOClient.IntegrationTests
{
    public abstract class WebSocketBaseTests : SocketIOBaseTests
    {
        protected override SocketIOOptions CreateOptions()
        {
            return new SocketIOOptions
            {
                AutoUpgrade = false
            };
        }

        protected override SocketIO CreateSocketIO()
        {
            var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false
            });
            io.LoggerFactory = LoggerFactory.Create(options =>
            {
                options.AddConsole();
                options.AddFilter(nameof(SocketIO), LogLevel.Debug);
            });
            return io;
        }
    }
}