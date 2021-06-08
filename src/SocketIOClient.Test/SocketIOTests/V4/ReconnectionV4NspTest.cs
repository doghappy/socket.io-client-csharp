using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V4)]
    public class ReconnectionV4NspTest : ReconnectionTest
    {
        protected override string Url => GetConstant("NSP_URL");

        protected override string Prefix => "/nsp,V4: ";

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
        public override async Task ManuallyReconnectionTest()
        {
            await base.ManuallyReconnectionTest();
        }
    }
}
