using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class OnDisconnected
    {
        [TestMethod]
        public async Task Test()
        {
            string reason = null;
            var client = new SocketIO(ConnectAsyncTest.URL, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("sever disconnect", false);
            };
            client.OnDisconnected += (sender, e) => reason = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("io server disconnect", reason);
        }

        [TestMethod]
        public async Task NspTest()
        {
            string reason = null;
            var client = new SocketIO(ConnectAsyncTest.NSP_URL, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("sever disconnect", false);
            };
            client.OnDisconnected += (sender, e) => reason = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("io server disconnect", reason);
        }
    }
}
