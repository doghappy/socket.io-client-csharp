using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V3)]
    public class OnErrorV3NspTest : OnErrorTest
    {
        protected override string Url => GetConstant("NSP_URL");

        protected override string Prefix => "/nsp,V3: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
