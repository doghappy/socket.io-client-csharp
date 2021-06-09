using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class DisconnectionTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public virtual async Task Test()
        {
            var client = new SocketIO(SocketIOCreator.Url, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", SocketIOCreator.Token }
                }
            });

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);

            client.OnConnected += async (sender, e) =>
            {
                Assert.IsTrue(client.Connected);
                Assert.IsFalse(client.Disconnected);
                await client.EmitAsync("sever disconnect");
            };
            await client.ConnectAsync();

            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
        }
    }
}
