using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EventArguments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class OnReceivedEventTest
    {
        [TestMethod]
        public async Task Test()
        {
            ReceivedEventArgs args = null;
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
                await client.EmitAsync("hi", "unit test");
            };
            client.OnReceivedEvent += (sender, e) => args = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi", args.Event);
            Assert.AreEqual("hi unit test, You are connected to the server", args.Response.GetValue<string>());
        }

        [TestMethod]
        public async Task NspTest()
        {
            ReceivedEventArgs args = null;
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
                await client.EmitAsync("hi", "unit test");
            };
            client.OnReceivedEvent += (sender, e) => args = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi", args.Event);
            Assert.AreEqual("hi unit test, You are connected to the server - nsp", args.Response.GetValue<string>());
        }
    }
}
