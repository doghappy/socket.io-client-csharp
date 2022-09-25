using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        [AssemblyInitialize]
        public static void Initialize(TestContext testContext)
        {
            Configuration = new ConfigurationBuilder()
                .AddYamlFile("appsettings.yml")
                .AddYamlFile("appsettings.user.yml")
                .AddEnvironmentVariables()
                // .AddCommandLine(args)
                .Build();
        }

        //[AssemblyCleanup]
        //public static void Cleanup()
        //{

        //}
    }
}