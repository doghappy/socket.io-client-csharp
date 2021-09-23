using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V2
{
    [TestClass]
    public class HeadersV2Test : HeadersTest
    {
        public HeadersV2Test()
        {
            SocketIOCreator = new SocketIOV2Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task CustomHeader()
        {
            await base.CustomHeader();
        }
    }
}
