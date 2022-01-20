using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    public abstract class HeadersTest
    {
        protected abstract ISocketIOCreateable SocketIOCreator { get; }

        public async virtual Task CustomHeader()
        {
            string result = null;

            var client = SocketIOCreator.Create(false);
            client.Options.ExtraHeaders = new Dictionary<string, string>
            {
                { "CustomHeader", "CustomHeader-Value" }
            };

            client.OnConnected += async (sender, e) =>
            {
                var sio = sender as SocketIO;
                await sio.EmitAsync("headers", response =>
                {
                    var jobj = response.GetValue() as JObject;
                    result = jobj.Value<string>("customheader");
                });
            };

            await client.ConnectAsync();
            await Task.Delay(400);

            Assert.AreEqual(result, "CustomHeader-Value");
            client.Dispose();
        }
    }
}
