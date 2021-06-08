using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V3)]
    public class DisconnectionV3Test : DisconnectionTest
    {
        protected override string Url => GetConstant("URL");

        protected override string Prefix => "V3: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
