using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient.Test.Models;

namespace SocketIOClient.Test
{
    [TestClass]
    public abstract class SocketIOTestBase
    {
        protected abstract string Uri { get; }

        [TestMethod]
        [Timeout(1000)]
        public async Task OnConnectedTest()
        {
            bool result = false;
            var client = new SocketIO(Uri);
            client.OnConnected += (sender, e) => result = true;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();
            Assert.IsTrue(result);
        }

        public abstract Task EventHiTest();

        [TestMethod]
        [Timeout(1000)]
        public async Task EventAckTest()
        {
            JToken result = null;
            var client = new SocketIO(Uri);
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("ack", response =>
                {
                    result = response.GetValue();
                }, ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(result.Value<bool>("result"));
            Assert.AreEqual("ack(.net core)", result.Value<string>("message"));
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task BinaryEventTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri);
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

        [TestMethod]
        [Timeout(1000)]
        public async Task ServerDisconectTest()
        {
            string reason = null;
            var client = new SocketIO(Uri);

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
        [Timeout(1000)]
        public async Task BinaryAckTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri);

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("binary ack", response =>
                {
                    result = response.GetValue<ByteResponse>();
                }, name, new
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
