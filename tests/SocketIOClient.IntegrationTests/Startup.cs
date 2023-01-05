using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
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