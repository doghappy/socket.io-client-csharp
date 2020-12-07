using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class V3ReconnectionTest
    {
        [TestMethod]
        [Timeout(30000)]
        public async Task ManuallyReconnectionTest()
        {
            var client = new SocketIO(ConnectAsyncTest.V4_NSP_URL, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "v3" }
                },
                EIO = 4
            });

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);

            int connectedCount = 0;
            int disconnectedCount = 0;
            int pongCount = 0;

            client.OnConnected += (sender, e) =>
            {
                connectedCount++;
                Assert.IsTrue(client.Connected);
                Assert.IsFalse(client.Disconnected);
            };
            client.OnDisconnected += async (sender, e) =>
            {
                disconnectedCount++;
                Assert.IsFalse(client.Connected);
                Assert.IsTrue(client.Disconnected);
                if (disconnectedCount <= 1)
                {
                    await client.ConnectAsync();
                }
            };
            client.OnPong += async (sender, e) =>
            {
                pongCount++;
                Assert.IsTrue(client.Connected);
                Assert.IsFalse(client.Disconnected);
                await client.EmitAsync("sever disconnect");
            };
            await client.ConnectAsync();
            await Task.Delay(22000);
            await client.DisconnectAsync();

            Assert.AreEqual(2, connectedCount);
            Assert.AreEqual(2, disconnectedCount);
            Assert.AreEqual(2, pongCount);
            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
        }
    }
}
