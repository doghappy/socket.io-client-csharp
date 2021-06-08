using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class OffTest : SocketIOTest
    {
        public async virtual Task Test()
        {
            string result = null;
            int hiCount = 0;
            var client = new SocketIO(Url, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", Version }
                }
            });
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
            Assert.AreEqual($"{Prefix}.net core", result);
            Assert.AreEqual(1, hiCount);

            client.Off("hi");
            await client.EmitAsync("hi", ".net core 1");
            await Task.Delay(200);
            await client.DisconnectAsync();
            Assert.AreEqual($"{Prefix}.net core", result);
            Assert.AreEqual(1, hiCount);
        }
    }
}
