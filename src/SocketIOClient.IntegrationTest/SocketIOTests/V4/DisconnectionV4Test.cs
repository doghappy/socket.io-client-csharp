using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V4
{
    [TestClass]
    public class DisconnectionV4Test : DisconnectionTest
    {
        public DisconnectionV4Test()
        {
            SocketIOCreator = new SocketIOV4Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
