using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Test.Models;
using System.Collections.Generic;
using System.Text;
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

            Assert.AreEqual("nsp: socket.io v3", result);
        }

        [TestMethod]
        public async Task BinaryEventTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(ConnectAsyncTest.V4_URL, new SocketIOOptions
            {
                Reconnection = false,
                EIO = 4,
                Query = new Dictionary<string, string>
                {
                    { "token", "v3" }
                }
            });
            client.On("bytes", response => result = response.GetValue<ByteResponse>());

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("bytes", name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }
    }
}
