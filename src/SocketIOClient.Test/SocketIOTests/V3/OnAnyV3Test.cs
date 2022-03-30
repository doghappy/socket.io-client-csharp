using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    [TestClass]
    public class OnAnyV3Test : OnAnyTest
    {
        public OnAnyV3Test()
        {
            SocketIOCreator = new SocketIOV3Creator();
        }

        protected override ISocketIOCreateable SocketIOCreator { get; }

        [TestMethod]
        public override async Task Test()
        {
            await base.Test();
        }

        [TestMethod]
        public async Task BinaryMessage_ShouldWork()
        {
            SocketIOResponse result = null;
            string name = string.Empty;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("1 params", Encoding.UTF8.GetBytes(nameof(BinaryMessage_ShouldWork)));
            };
            client.OnAny((eventName, response) =>
            {
                result = response;
                name += eventName;
            });
            await client.ConnectAsync();
            await Task.Delay(600);
            await client.DisconnectAsync();

            Assert.AreEqual("1 params", name);
            Assert.AreEqual(nameof(BinaryMessage_ShouldWork), Encoding.UTF8.GetString(result.GetValue<byte[]>()));
            client.Dispose();
        }
    }
}
