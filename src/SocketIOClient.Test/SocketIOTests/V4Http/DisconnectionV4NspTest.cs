using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4Http
{
    [TestClass]
    public class DisconnectionV4NspTest : DisconnectionHttpTest
    {
        public DisconnectionV4NspTest()
        {
            SocketIOCreator = new SocketIOV4NspCreator();
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
