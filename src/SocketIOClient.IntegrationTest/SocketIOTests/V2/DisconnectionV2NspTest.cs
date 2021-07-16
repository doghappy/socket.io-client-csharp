using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTest.SocketIOTests.V2
{
    [TestClass]
    public class DisconnectionV2NspTest : DisconnectionTest
    {
        public DisconnectionV2NspTest()
        {
            SocketIOCreator = new SocketIOV2NspCreator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
