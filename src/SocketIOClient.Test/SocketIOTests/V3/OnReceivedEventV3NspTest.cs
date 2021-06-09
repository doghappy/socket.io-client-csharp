using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    public class OnReceivedEventV3NspTest : OnReceivedEventTest
    {
        public OnReceivedEventV3NspTest()
        {
            SocketIOCreator = new ScoketIOV3NspCreator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
