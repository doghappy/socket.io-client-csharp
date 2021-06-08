using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V2)]
    public class ReconnectionV2Test : ReconnectionTest
    {
        protected override string Url => GetConstant("URL");

        protected override string Prefix => "V2: ";

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
