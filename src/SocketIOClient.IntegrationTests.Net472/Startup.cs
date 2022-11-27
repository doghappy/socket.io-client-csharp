using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests.Net472
{
    [TestClass]
    internal class Startup
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext testContext)
        {
           Common.Startup.Initialize();
        }
    }
}
