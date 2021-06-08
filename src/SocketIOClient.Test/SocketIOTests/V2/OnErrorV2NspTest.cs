using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V2)]
    public class OnErrorV2NspTest : OnErrorTest
    {
        protected override string Url => GetConstant("NSP_URL");

        protected override string Prefix => "/nsp,V2: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
