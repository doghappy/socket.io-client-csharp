using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OffAnyTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public virtual async Task Test()
        {
            SocketIOResponse result = null;
            string name = string.Empty;
            int hiCount = 0;
            var client = SocketIOCreator.Create();
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "onAny");
            };
            OnAnyHandler handler = (eventName, response) =>
            {
                result = response;
                name += eventName;
            };
            client.OnAny(handler);
            client.On("hi", response =>
            {
                hiCount++;
                name += "[on('hi')]";
            });
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi[on('hi')]", name);
            Assert.AreEqual($"{SocketIOCreator.Prefix}onAny", result.GetValue<string>());

            client.OffAny(handler);
            await client.EmitAsync("hi", "onAny2");

            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi[on('hi')]", name);
            Assert.AreEqual(2, hiCount);
            client.Dispose();
        }
    }
}
