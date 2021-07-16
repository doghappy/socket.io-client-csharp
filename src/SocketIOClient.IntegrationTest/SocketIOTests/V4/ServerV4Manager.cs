namespace SocketIOClient.IntegrationTest.SocketIOTests.V4
{
    public class ServerV4Manager : BaseServerManager, IServerManager
    {
        public ServerV4Manager() 
            : base(@"..\..\..\..\socket.io-server-v4")
        {
        }
    }
}
