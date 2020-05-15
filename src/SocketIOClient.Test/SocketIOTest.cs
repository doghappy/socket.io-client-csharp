using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIOTest : SocketIOTestBase
    {
        protected override string Uri => "http://localhost:11000/";

        [TestMethod]
        [Timeout(1000)]
        public override async Task EventHiTest()
        {
            string result = null;
            var client = new SocketIO(Uri);
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(400);
            await client.DisconnectAsync();

            Assert.AreEqual("hi .net core, You are connected to the server", result);
        }
    }
}
