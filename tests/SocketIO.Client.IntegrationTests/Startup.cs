using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 8, Scope = ExecutionScope.ClassLevel)]

namespace SocketIO.Client.IntegrationTests
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