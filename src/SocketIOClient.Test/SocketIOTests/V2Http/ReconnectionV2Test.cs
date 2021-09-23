using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2Http
{
    [TestClass]
    public class ReconnectionV2Test : ReconnectionHttpTest
    {
        public ReconnectionV2Test()
        {
            SocketIOCreator = new SocketIOV2Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        // NOTE: This test case is wrong, because the client will not automatically reconnect after the server closes the connection.
        //[TestMethod]
        //public override async Task ReconnectionTrueTest()
        //{
        //    await base.ReconnectionTrueTest();
        //}

        // NOTE: This test case is wrong, because the client will not automatically reconnect after the server closes the connection.
        //[TestMethod]
        //public override async Task ReconnectionAttemptsExceededTest()
        //{
        //    await base.ReconnectionAttemptsExceededTest();
        //}

        [TestMethod]
        public override async Task ReconnectionFalseTest()
        {
            await base.ReconnectionFalseTest();
        }

        // NOTE: This test case is wrong, because the client will not automatically reconnect after the server closes the connection.
        //[TestMethod]
        //public override async Task ReconnectingTest()
        //{
        //    await base.ReconnectingTest();
        //}

        // NOTE: This test takes a long time, disable it.
        //[TestMethod]
        //public override async Task ManuallyReconnectionTest()
        //{
        //    await base.ManuallyReconnectionTest();
        //}
    }
}
