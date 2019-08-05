using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIOTest
    {
        [TestMethod]
        public async Task ConnectTest()
        {
            var client = new SocketIO("http://localhost:3000");
            client.OnOpened += async arg =>
            {
                Assert.IsNotNull(arg);
                Assert.IsFalse(string.IsNullOrEmpty(arg.Sid));
                Assert.IsTrue(arg.PingInterval != 0);
                await client.CloseAsync();
            };
            await client.ConnectAsync();
        }

        [TestMethod]
        public async Task OnConnectedTest()
        {
            bool result = false;
            var client = new SocketIO("http://localhost:3000");
            client.OnConnected += () =>
            {
                result = true;
            };
            await client.ConnectAsync();
            await Task.Delay(1000);
            Assert.IsTrue(result);
        }

        //[TestMethod]
        //public async Task OnClosedTest()
        //{
        //    bool result = false;
        //    var client = new SocketIO("http://localhost:3000");
        //    client.OnClosed += () =>
        //    {
        //        result = true;
        //    };
        //    await client.ConnectAsync();
        //    await client.CloseAsync();
        //    await Task.Delay(1000);
        //    Assert.IsTrue(result);
        //}

        [TestMethod]
        public async Task MessageTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string guid = Guid.NewGuid().ToString();
            client.On("message", async res =>
             {
                 Assert.AreEqual("42[\"message\",\"connected - server\"]", res.RawText);
                 Assert.AreEqual("\"connected - server\"", res.Text);
                 await client.CloseAsync();
             });
            await client.ConnectAsync();
        }

        [TestMethod]
        public async Task EmitStringTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string guid = Guid.NewGuid().ToString();
            client.On("test", async res =>
            {
                Assert.AreEqual(guid + " - server", res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await client.EmitAsync("test", guid);
        }

        [TestMethod]
        public async Task EmitObjectTest()
        {
            var client = new SocketIO("http://localhost:3000");
            client.On("test", async res =>
            {
                Assert.AreEqual("{\"code\":200,\"message\":\"\\\"ok\",\"source\":\"server\"}", res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await client.EmitAsync("test", new
            {
                code = 200,
                message = "\"ok"
            });
        }

        [TestMethod]
        public async Task EmitArrayTest()
        {
            var client = new SocketIO("http://localhost:3000");
            client.On("test", async res =>
            {
                Assert.AreEqual("[0,1,2]", res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await client.EmitAsync("test", new[] { 0, 1, 2 });
        }

        [TestMethod]
        public async Task PathMessageTest()
        {
            var client = new SocketIO("http://localhost:3000/path");
            string guid = Guid.NewGuid().ToString();
            client.On("message", async res =>
            {
                Assert.AreEqual("42[\"message\",\"connected - server/path\"]", res.RawText);
                Assert.AreEqual("\"connected - server/path\"", res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
        }

        [TestMethod]
        public async Task CloseByServerTest()
        {
            var client = new SocketIO("http://localhost:3000");
            client.OnClosed += () =>
            {
                Assert.IsTrue(true);
            };
            await client.ConnectAsync();
            await client.EmitAsync("close", "close");
        }

        [TestMethod]
        public async Task CloseByServerWithPathTest()
        {
            bool result = false;
            var client = new SocketIO("http://localhost:3000/path");
            client.OnClosed += () =>
            {
                result = true;
            };
            await client.ConnectAsync();
            await client.EmitAsync("close", "close");
            await Task.Delay(1000);
            Assert.IsTrue(result);
        }
    }
}
