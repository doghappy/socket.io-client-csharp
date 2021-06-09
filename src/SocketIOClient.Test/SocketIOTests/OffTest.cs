using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OffTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public async virtual Task Test()
        {
            string result = null;
            int hiCount = 0;
            var client = SocketIOCreator.Create();
            client.On("hi", response =>
            {
                hiCount++;
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", $".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            Assert.AreEqual($"{SocketIOCreator.Prefix}.net core", result);
            Assert.AreEqual(1, hiCount);

            client.Off("hi");
            await client.EmitAsync("hi", ".net core 1");
            await Task.Delay(200);
            await client.DisconnectAsync();
            Assert.AreEqual($"{SocketIOCreator.Prefix}.net core", result);
            Assert.AreEqual(1, hiCount);
        }
    }
}
