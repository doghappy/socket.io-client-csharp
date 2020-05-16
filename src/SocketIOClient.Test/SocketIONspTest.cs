using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EventArguments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIONspTest : SocketIOTestBase
    {
        protected override string Uri => "http://localhost:11000/nsp";

        [TestMethod]
        public override async Task EventHiTest()
        {
            string result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi .net core, You are connected to the server - nsp", result);
        }

        [TestMethod]
        public async Task OnReceivedEventTest()
        {
            ReceivedEventArgs args = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
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
