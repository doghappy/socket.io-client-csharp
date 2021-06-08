using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V3)]
    public class ReconnectionV3NspTest : ReconnectionTest
    {
        protected override string Url => GetConstant("NSP_URL");

        protected override string Prefix => "/nsp,V3: ";

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
