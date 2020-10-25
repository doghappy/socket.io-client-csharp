using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class OnReconnectFailedTest
    {
        [TestMethod]
        public async Task InvokedTest()
        {
            bool isInvoked = false;
            // Create a URL where the service does not exist
            var client = new SocketIO("http://localhost:11001", new SocketIOOptions
            {
                AllowedRetryFirstConnection = true,
                ReconnectionDelayMax = 1000,
            });
            client.OnReconnectFailed += (sender, ex) => isInvoked = true;
            await client.ConnectAsync();

            Assert.IsTrue(isInvoked);
        }
    }
}
