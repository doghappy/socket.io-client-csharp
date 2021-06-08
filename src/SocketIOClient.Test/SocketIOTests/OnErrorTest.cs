using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OnErrorTest : SocketIOTest
    {
        public virtual async Task Test()
        {
            bool connected = false;
            string error = null;
            var client = new SocketIO(Url, new SocketIOOptions
            {
                Reconnection = false
            });
            client.OnConnected += (sender, e) => connected = true;
            client.OnError += (sender, e) => error = e;
            await client.ConnectAsync();
            await Task.Delay(200);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);

            await client.DisconnectAsync();

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.IsFalse(connected);
            Assert.AreEqual("Authentication error", error);
        }
    }
}
