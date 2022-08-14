using Microsoft.Extensions.Logging;

namespace SocketIOClient.IntegrationTests
{
    public abstract class HttpBaseTests : SocketIOBaseTests
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
            var options = CreateOptions();
            options.Reconnection = false;
            return CreateSocketIO(options);
        }
    }
}