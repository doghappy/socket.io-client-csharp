using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class ReconnectionTest
    {
        [TestMethod]
        public async Task ReconnectionTrueTest()
        {
            int hiCount = 0;
            string res = null;
            int disconnectionCount = 0;
            var client = new SocketIO(ConnectAsyncTest.URL, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                res = response.GetValue<string>();
                hiCount++;
            });

            client.OnDisconnected += (sender, e) =>
            {
                disconnectionCount++;
            };

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "SocketIOClient.Test");
                await Task.Delay(10);
                if (hiCount >= 2)
                {
                    await client.DisconnectAsync();
                }
                else
                {
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(2400);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.AreEqual(2, hiCount);
            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual("hi SocketIOClient.Test, You are connected to the server", res);
        }

        [TestMethod]
        public async Task ReconnectionFalseTest()
        {
            int hiCount = 0;
            string res = null;
            int disconnectionCount = 0;
            var client = new SocketIO(ConnectAsyncTest.URL, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                res = response.GetValue<string>();
                hiCount++;
            });

            client.OnDisconnected += (sender, e) =>
            {
                disconnectionCount++;
            };

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "SocketIOClient.Test");
                await Task.Delay(10);
                if (hiCount >= 2)
                {
                    await client.DisconnectAsync();
                }
                else
                {
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(1000);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.AreEqual(1, hiCount);
            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual("hi SocketIOClient.Test, You are connected to the server", res);
        }

        [TestMethod]
        public async Task ReconnectingTest()
        {
            int disconnectionCount = 0;
            int reconnectingCount = 0;
            int attempt = 0;
            bool connectedFlag = false;
            var client = new SocketIO(ConnectAsyncTest.URL, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnDisconnected += (sender, e) => disconnectionCount++;

            client.OnReconnecting += (sender, e) =>
            {
                reconnectingCount++;
                attempt = e;
            };

            client.OnConnected += async (sender, e) =>
            {
                if (!connectedFlag)
                {
                    await Task.Delay(200);
                    connectedFlag = true;
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(2400);
            await client.DisconnectAsync();

            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual(1, reconnectingCount);
            Assert.AreEqual(1, attempt);
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task ManuallyReconnectionTest()
        {
            var client = new SocketIO(ConnectAsyncTest.NSP_URL, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
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
