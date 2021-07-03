using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.SocketIOTests.V4;
using System.Collections.Generic;
using System.Threading;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]

    public class AssemblyIntegrationTest
    {
        private static readonly IEnumerable<IServerManager> Servers = new List<IServerManager>()
        {
            new ServerV2Manager(),
            new ServerV3Manager(),
            new ServerV4Manager()
        };

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            foreach (var server in Servers) 
            {
                server.Create();
            }

            // Give some time to be sure that servers are running.
            Thread.Sleep(5000);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            foreach (var server in Servers)
            {
                server.Destroy();
            }
        }
    }
}
