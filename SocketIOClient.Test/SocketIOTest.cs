using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIOTest
    {
        //[TestMethod]
        //public async Task ConnectTest()
        //{
        //    var client = new SocketIO("http://localhost:3000");
        //    client.OnOpened += async arg =>
        //    {
        //        Assert.IsNotNull(arg);
        //        Assert.IsFalse(string.IsNullOrEmpty(arg.Sid));
        //        Assert.IsTrue(arg.PingInterval != 0);
        //        await client.CloseAsync();
        //    };
        //    await client.ConnectAsync();
        //}

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
            client.OnClosed += reason =>
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
            client.OnClosed += reason =>
            {
                result = true;
            };
            await client.ConnectAsync();
            await client.EmitAsync("close", "close");
            await Task.Delay(1000);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RoomTest()
        {
            var client = new SocketIO("http://localhost:3000");
            await client.ConnectAsync();
            string room = Guid.NewGuid().ToString();

            string roomMsg = string.Empty;
            client.On(room, res =>
            {
                roomMsg = res.Text;
            });

            await client.EmitAsync("create room", room);
            await Task.Delay(1000);
            Assert.AreEqual("\"I joined the room: " + room + "\"", roomMsg);
        }

        [TestMethod]
        public async Task RoomMessageTest()
        {
            string room = "ROOM";
            string client1Msg = string.Empty;
            string client2Msg = string.Empty;

            var client1 = new SocketIO("http://localhost:3000");
            client1.On(room, res => client1Msg = res.Text);
            await client1.ConnectAsync();
            await client1.EmitAsync("create room", room);

            var client2 = new SocketIO("http://localhost:3000");
            client2.On(room, res => client2Msg = res.Text);
            await client2.ConnectAsync();
            await client2.EmitAsync("create room", room);

            //需要添加 EmitAsync("event",roomName,data);

            await Task.Delay(1000);
            Assert.AreEqual(client1Msg, client2Msg);
        }

        [TestMethod]
        public async Task EventNameTest()
        {
            string text = string.Empty;
            var client = new SocketIO("http://localhost:3000/path");
            client.On("ws_message -new", res =>
            {
                text = res.Text;
            });
            await client.ConnectAsync();
            await client.EmitAsync("ws_message -new", "ws_message-new");
            await Task.Delay(1000);
            Assert.AreEqual(text, "\"message from server\"");
        }

        //[TestMethod]
        //public async Task ReConnectTest()
        //{
        //    string text = string.Empty;
        //    var client = new SocketIO("http://localhost:3000");
        //    await client.ConnectAsync();
        //    await client.CloseAsync()
        //    await client.EmitAsync("ws_message -new", "ws_message-new");
        //    await Task.Delay(1000);
        //    Assert.AreEqual(text, "\"message from server\"");
        //}

        [TestMethod]
        public async Task CallbackTest()
        {
            string text = string.Empty;
            string guid = Guid.NewGuid().ToString();
            var client = new SocketIO("http://localhost:3000");
            await client.ConnectAsync();
            await client.EmitAsync("callback", guid, async res =>
            {
                text = res.Text;
                await client.CloseAsync();
            });
            await Task.Delay(1000);
            Assert.AreEqual($"\"{guid} - server\"", text);
        }

        [TestMethod]
        public async Task NonCallbackTest()
        {
            string guid = Guid.NewGuid().ToString();
            var client = new SocketIO("http://localhost:3000");
            await client.ConnectAsync();
            await client.EmitAsync("callback", guid);
            await Task.Delay(1000);
            Assert.AreEqual(SocketIOState.Connected, client.State);
        }

        [TestMethod]
        public async Task UnhandleEventTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string text = string.Empty;
            string en = string.Empty;
            string guid = Guid.NewGuid().ToString();
            client.UnhandledEvent += (eventName, args) =>
            {
                en = eventName;
                text = args.Text;
            };
            await client.ConnectAsync();
            await client.EmitAsync("UnhandledEvent", guid);
            await Task.Delay(1000);
            Assert.AreEqual("UnhandledEvent-Server", en);
            Assert.AreEqual($"\"{guid} - server\"", text);
        }
    }
}
