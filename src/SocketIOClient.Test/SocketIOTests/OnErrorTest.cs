using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class OnErrorTest
    {
        [TestMethod]
        public async Task NspTest()
        {
            bool connected = false;
            string error = null;
            var client = new SocketIO(ConnectAsyncTest.NSP_URL, new SocketIOOptions
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
