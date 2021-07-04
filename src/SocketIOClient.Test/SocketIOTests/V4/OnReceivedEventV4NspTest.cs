using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    [TestClass]
    public class OnReceivedEventV4NspTest : OnReceivedEventTest
    {
        public OnReceivedEventV4NspTest()
        {
            SocketIOCreator = new SocketIOV4NspCreator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
