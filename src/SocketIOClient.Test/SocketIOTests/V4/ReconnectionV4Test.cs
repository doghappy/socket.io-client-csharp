using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    [TestClass]
    public class ReconnectionV4Test : ReconnectionTest
    {
        public ReconnectionV4Test()
        {
            SocketIOCreator = new SocketIOV4Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task ReconnectionTrueTest()
        {
            await base.ReconnectionTrueTest();
        }

        [TestMethod]
        public override async Task ReconnectionFalseTest()
        {
            await base.ReconnectionFalseTest();
        }

        [TestMethod]
        public override async Task ReconnectingTest()
        {
            await base.ReconnectingTest();
        }

        [TestMethod]
        public override async Task ReconnectionAttemptsExceededTest()
        {
            await base.ReconnectionAttemptsExceededTest();
        }

        [TestMethod]
        public override async Task ManuallyReconnectionTest()
        {
            await base.ManuallyReconnectionTest();
        }
    }
}
