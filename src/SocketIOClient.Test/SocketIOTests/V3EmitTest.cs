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
        public async Task BinaryTest()
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
            client.On("binary", response =>
            {
                var bytes = response.GetValue<byte[]>();
                result = Encoding.UTF8.GetString(bytes);
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("binary", "return all the characters");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("return all the characters", result);
        }

        [TestMethod]
        public async Task BinaryObjTest()
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
            client.On("binary-obj", response =>
            {
                var data = response.GetValue<BinaryObjectResponse>();
                result = Encoding.UTF8.GetString(data.Data);
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("binary-obj", "return all the characters");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("return all the characters", result);
        }
    }
}
