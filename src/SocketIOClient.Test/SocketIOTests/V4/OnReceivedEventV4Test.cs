using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Attributes;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V4
{
    [TestClass]
    [SocketIOVersion(SocketIOVersion.V4)]
    public class OnReceivedEventV4Test : OnReceivedEventTest
    {
        protected override string Url => GetConstant("URL");

        protected override string Prefix => "V4: ";

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }
    }
}
