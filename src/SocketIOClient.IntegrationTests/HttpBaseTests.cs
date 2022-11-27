namespace SocketIOClient.IntegrationTests
{
    public abstract class HttpBaseTests : SocketIOBaseTests
    {
        protected override SocketIOOptions CreateOptions()
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
            return CreateSocketIO(options);
        }
    }
}