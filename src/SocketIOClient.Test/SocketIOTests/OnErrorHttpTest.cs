using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OnErrorHttpTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public virtual async Task Test()
        {
            bool connected = false;
            string error = null;
            var client = new SocketIO(SocketIOCreator.Url, new SocketIOOptions
            {
                Transport = Transport.TransportProtocol.Polling,
                Reconnection = false,
                EIO = SocketIOCreator.EIO
            });
            client.OnConnected += (sender, e) => connected = true;
            client.OnError += (sender, e) => error = e;
            await client.ConnectAsync();
            await Task.Delay(600);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);

            await client.DisconnectAsync();

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.IsFalse(connected);
            Assert.AreEqual("Authentication error", error);
            client.Dispose();
        }
    }
}
