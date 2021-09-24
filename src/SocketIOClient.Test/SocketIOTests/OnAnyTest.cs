using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OnAnyTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public virtual async Task Test()
        {
            SocketIOResponse result = null;
            string name = string.Empty;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "onAny");
            };
            client.OnAny((eventName, response) =>
            {
                result = response;
                name += eventName;
            });
            client.On("hi", response =>
            {
                name += "[on('hi')]";
            });
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi[on('hi')]", name);
            Assert.AreEqual($"{SocketIOCreator.Prefix}onAny", result.GetValue<string>());
            client.Dispose();
        }
    }
}
