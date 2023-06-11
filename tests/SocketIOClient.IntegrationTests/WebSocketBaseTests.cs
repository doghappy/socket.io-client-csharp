namespace SocketIOClient.IntegrationTests
{
    public abstract class WebSocketBaseTests : SocketIOBaseTests
    {
        protected virtual SocketIOOptions CreateOptions()
        {
            return new SocketIOOptions
            {
                AutoUpgrade = false,
                EIO = EIO,
            };
        }

        protected override SocketIO CreateSocketIO()
        {
            return CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
            });
        }
    }
}