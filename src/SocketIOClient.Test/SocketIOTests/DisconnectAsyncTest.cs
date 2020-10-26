using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class DisconnectAsyncTest
    {
        [TestMethod]
        public async Task NspTest()
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

            client.OnConnected += async (sender, e) =>
            {
                Assert.IsTrue(client.Connected);
                Assert.IsFalse(client.Disconnected);
                await client.EmitAsync("sever disconnect");
            };
            await client.ConnectAsync();

            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
        }
    }
}
