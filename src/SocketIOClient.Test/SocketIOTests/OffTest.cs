using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class OffTest
    {
        [TestMethod]
        public async Task Test()
        {
            string result = null;
            int hiCount = 0;
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
                hiCount++;
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            Assert.AreEqual("hi .net core, You are connected to the server", result);
            Assert.AreEqual(1, hiCount);

            client.Off("hi");
            await client.EmitAsync("hi", ".net core");
            await client.DisconnectAsync();
            Assert.AreEqual(1, hiCount);
        }
    }
}
