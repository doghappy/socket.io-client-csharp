using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EventArguments;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OnReceivedEventTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public virtual async Task Test()
        {
            ReceivedEventArgs args = null;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "unit test");
            };
            client.OnReceivedEvent += (sender, e) => args = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi", args.Event);
            Assert.AreEqual($"{SocketIOCreator.Prefix}unit test", args.Response.GetValue<string>());
        }
    }
}
