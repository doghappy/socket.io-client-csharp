using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V3HttpTests : HttpBaseTests
    {
        protected override string ServerUrl => V3_HTTP;
        protected override string ServerTokenUrl => V3_HTTP_TOKEN;
    }
}