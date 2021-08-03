using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    public class DisconnectionV3Test : DisconnectionTest
    {
        public DisconnectionV3Test()
        {
            SocketIOCreator = new SocketIOV3Creator();
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
