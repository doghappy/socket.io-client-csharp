using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    public class HeadersV3Test : HeadersTest
    {
        public HeadersV3Test()
        {
            SocketIOCreator = new SocketIOV3Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task CustomHeader()
        {
            await base.CustomHeader();
        }
    }
}
