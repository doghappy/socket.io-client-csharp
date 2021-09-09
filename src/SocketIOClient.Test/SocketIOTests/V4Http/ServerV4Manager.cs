namespace SocketIOClient.Test.SocketIOTests.V4Http
{
    public class ServerV4Manager : BaseServerManager, IServerManager
    {
        public ServerV4Manager() 
            : base(@"..\..\..\..\socket.io-server-v4")
        {
        }
    }
}
