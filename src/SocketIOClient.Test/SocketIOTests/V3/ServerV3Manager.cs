namespace SocketIOClient.Test.SocketIOTests.V4
{
    public class ServerV3Manager : BaseServerManager, IServerManager
    {
        public ServerV3Manager() 
            : base(@"..\..\..\..\socket.io-server-v3")
        {
        }
    }
}
