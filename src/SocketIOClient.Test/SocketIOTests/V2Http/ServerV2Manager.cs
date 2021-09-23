namespace SocketIOClient.Test.SocketIOTests.V2Http
{
    public class ServerV2Manager : BaseServerManager, IServerManager
    {
        public ServerV2Manager() 
            : base(@"..\..\..\..\socket.io-server-v2")
        {
        }
    }
}
