using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V2)]
    public class DisconnectionV2Test : DisconnectionTest
    {
        protected override string Url => GetConstant("URL");

        protected override string Prefix => "V2: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
