using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2Http
{
    [TestClass]
    public class DisconnectionV2Test : DisconnectionHttpTest
    {
        public DisconnectionV2Test()
        {
            SocketIOCreator = new SocketIOV2Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task ClientDisconnect()
        {
            await base.ClientDisconnect();
        }

        [TestMethod]
        public override async Task ServerDisconnect()
        {
            await base.ServerDisconnect();
        }
    }
}
