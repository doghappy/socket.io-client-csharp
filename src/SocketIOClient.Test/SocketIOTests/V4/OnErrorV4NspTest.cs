using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V4)]
    public class OnErrorV4NspTest : OnErrorTest
    {
        protected override string Url => GetConstant("NSP_URL");

        protected override string Prefix => "/nsp,V4: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
