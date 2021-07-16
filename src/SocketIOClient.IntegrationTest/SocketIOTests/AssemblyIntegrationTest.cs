using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.IntegrationTest.Configuration;
using SocketIOClient.IntegrationTest.SocketIOTests.V4;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SocketIOClient.IntegrationTest.SocketIOTests
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

        readonly static Preference Preference = PreferenceManager.Get();

        private static bool IsRunningOnAzureDevOps => Environment.GetEnvironmentVariable("SYSTEM_DEFINITIONID") != null;

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            if (!IsRunningOnAzureDevOps && Preference.RunServers)
            {
                foreach (var server in Servers)
                {
                    server.Create();
                }

                // Give some time to be sure that servers are running.
                Thread.Sleep(5000);
            }
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            if (!IsRunningOnAzureDevOps && Preference.RunServers && Preference.StopServersAfterRun)
            {
                foreach (var server in Servers)
                {
                    server.Destroy();
                }
            }
        }
    }
}
