using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class V3EmitTest
    {
        [TestMethod]
        public async Task HiTest()
        {
            string result = null;
            var client = new SocketIO(ConnectAsyncTest.V4_URL, new SocketIOOptions
            {
                Reconnection = false,
                EIO = 4,
                Query = new Dictionary<string, string>
                {
                    { "token", "v3" }
                }
            });
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "socket.io v3");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("io: socket.io v3", result);
        }

        [TestMethod]
        public async Task NspHiTest()
        {
            string result = null;
            var client = new SocketIO(ConnectAsyncTest.V4_NSP_URL, new SocketIOOptions
            {
                Reconnection = false,
                EIO = 4,
                Query = new Dictionary<string, string>
                {
                    { "token", "v4" }
                }
            });
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "socket.io v3");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("nsp: socket.io v3", result);
        }
    }
}
