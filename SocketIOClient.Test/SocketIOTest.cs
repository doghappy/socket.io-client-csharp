using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIOTest
    {
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

        [TestMethod]
        public async Task EmitStringTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string guid = Guid.NewGuid().ToString();
            string result = null;
            client.On("test", async res =>
            {
                result = JsonConvert.DeserializeObject<string>(res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("test", guid);
            await Task.Delay(1000);

            Assert.AreEqual(guid + " - server", result);
        }

        [TestMethod]
        public async Task Emit3StringTest()
        {
            var client = new SocketIO("http://localhost:3000");

            var dic = new Dictionary<int, bool>();
            for (int i = 0; i < 3; i++)
            {
                dic.Add(i, false);
            }

            client.On("test", res =>
            {
                string text = JsonConvert.DeserializeObject<string>(res.Text);
                int id = int.Parse(text[0].ToString());
                dic[id] = true;
            });

            await client.ConnectAsync();
            await Task.Delay(1000);
            foreach (var item in dic)
            {
                await client.EmitAsync("test", item.Key.ToString());
            }
            await Task.Delay(1000);
            await client.CloseAsync();

            Assert.IsTrue(dic.All(i => i.Value));
        }

        [TestMethod]
        public async Task EmitObjectTest()
        {
            var client = new SocketIO("http://localhost:3000");
            JObject obj = null;
            client.On("test", async res =>
            {
                obj = JObject.Parse(res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await client.EmitAsync("test", new
            {
                code = 200,
                message = "\"ok"
            });
            await Task.Delay(1000);

            Assert.AreEqual(200, obj.Value<int>("code"));
            Assert.AreEqual("\"ok", obj.Value<string>("message"));
            Assert.AreEqual("server", obj.Value<string>("source"));
        }

        [TestMethod]
        public async Task EmitArrayTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string result = null;
            client.On("test", async res =>
            {
                result = res.Text;
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("test", new[] { 0, 1, 2 });
            await Task.Delay(1000);
            Assert.AreEqual("[0,1,2]", result);
        }

        [TestMethod]
        public async Task CloseByServerTest()
        {
            var client = new SocketIO("http://localhost:3000");
            bool result = false;
            client.OnClosed += reason =>
            {
                result = true;
            };
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("close", "close");
            await Task.Delay(1000);
            Assert.IsTrue(result);
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
            await Task.Delay(1000);
            await client.EmitAsync("close", "close");
            await Task.Delay(1000);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task CloseByClientTest()
        {
            bool result = false;
            var client = new SocketIO("http://localhost:3000");
            client.OnClosed += reason =>
            {
                if (reason == ServerCloseReason.ClosedByClient)
                {
                    result = true;
                }
                else
                {
                    Assert.Fail();
                }
            };
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.CloseAsync();
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

            await Task.Delay(1000);
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
            await Task.Delay(1000);
            await client1.EmitAsync("create room", room);

            var client2 = new SocketIO("http://localhost:3000");
            client2.On(room, res => client2Msg = res.Text);
            await client2.ConnectAsync();
            await Task.Delay(1000);
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
            await Task.Delay(1000);
            await client.EmitAsync("ws_message -new", "ws_message-new");
            await Task.Delay(1000);
            Assert.AreEqual(text, "\"message from server\"");
        }

        [TestMethod]
        public async Task CallbackTest()
        {
            string text = string.Empty;
            string guid = Guid.NewGuid().ToString();
            var client = new SocketIO("http://localhost:3000");
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("callback", guid, async res =>
            {
                text = res.Text;
                await client.CloseAsync();
            });
            await Task.Delay(1000);
            Assert.AreEqual($"\"{guid} - server\"", text);
        }

        [TestMethod]
        public async Task CallbackWithRoomTest()
        {
            string text = string.Empty;
            string guid = Guid.NewGuid().ToString();
            var client = new SocketIO("http://localhost:3000/path");
            await client.ConnectAsync();
            await Task.Delay(1000);
            bool callbackCalled = false;
            await client.EmitAsync("callback", guid, async res =>
            {
                text = res.Text;
                callbackCalled = true;
                await client.CloseAsync();
            });
            await Task.Delay(1000);
            Assert.IsTrue(callbackCalled, "Callback was not called");
            Assert.AreEqual($"\"{guid} - server/path\"", text);
        }

        [TestMethod]
        public async Task NonCallbackTest()
        {
            string guid = Guid.NewGuid().ToString();
            var client = new SocketIO("http://localhost:3000");
            await client.ConnectAsync();
            await Task.Delay(1000);
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
            await Task.Delay(1000);
            await client.EmitAsync("UnhandledEvent", guid);
            await Task.Delay(1000);
            Assert.AreEqual("UnhandledEvent-Server", en);
            Assert.AreEqual($"\"{guid} - server\"", text);
            await client.ConnectAsync();
        }

        [TestMethod]
        public async Task ReceivedEventTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string en1 = string.Empty;
            string text1 = string.Empty;
            string text2 = string.Empty;
            string guid = Guid.NewGuid().ToString();
            client.OnReceivedEvent += (eventName, args) =>
            {
                en1 = eventName;
                text1 = args.Text;
            };
            client.On("test", args =>
            {
                text2 = args.Text;
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("test", guid);
            await Task.Delay(1000);
            Assert.AreEqual("test", en1);
            Assert.AreEqual($"\"{guid} - server\"", text1);
            Assert.AreEqual(text1, text2);
            await client.ConnectAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task TimeoutTest()
        {
            var client = new SocketIO("http://localhost:3000")
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(10)
            };
            await client.ConnectAsync();
        }

        [TestMethod]
        public async Task ErrorTest()
        {
            bool result = false;
            var client = new SocketIO("http://localhost:3000")
            {
                Parameters = new Dictionary<string, string>
                {
                    { "throw", "true" }
                }
            };
            string resText = null;
            client.OnError += args =>
            {
                result = true;
                resText = JsonConvert.DeserializeObject<string>(args.Text);
            };
            await client.ConnectAsync();
            await Task.Delay(1000);
            Assert.IsTrue(result);
            Assert.AreEqual("Authentication error", resText);
        }

        [TestMethod]
        public async Task NsErrorTest()
        {
            bool result = false;
            var client = new SocketIO("http://localhost:3000/path")
            {
                Parameters = new Dictionary<string, string>
                {
                    { "throw", "true" }
                }
            };
            string resText = null;
            client.OnError += args =>
            {
                result = true;
                resText = JsonConvert.DeserializeObject<string>(args.Text);
            };
            await client.ConnectAsync();
            await Task.Delay(1000);
            Assert.IsTrue(result);
            Assert.AreEqual("Authentication error -- Ns", resText);
        }

        [TestMethod]
        public async Task EmitStringWithPathTest()
        {
            var client = new SocketIO("http://localhost:3001")
            {
                Path = "/test"
            };
            string guid = Guid.NewGuid().ToString();
            string result = null;
            client.On("test", async res =>
            {
                result = JsonConvert.DeserializeObject<string>(res.Text);
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("test", guid);
            await Task.Delay(1000);

            Assert.AreEqual(guid + " - server", result);
        }

        [TestMethod]
        public async Task EmitNotingTest()
        {
            var client = new SocketIO("http://localhost:3000");
            bool result = false;
            client.On("emit-noting", async res =>
            {
                result = true;
                await client.CloseAsync();
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("emit-noting", null);
            await Task.Delay(1000);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task EmitMultipleArgsTest()
        {
            var client = new SocketIO("http://localhost:3000");
            string result1 = null;
            string result2 = null;
            string result3 = null;
            client.On("emit\\args\"", res => result1 = res.Text, res => result2 = res.Text, res => result3 = res.Text);
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("emit\\args\"");
            await Task.Delay(1000);
            await client.CloseAsync();

            Assert.AreEqual("\"channel\"", result1);
            Assert.AreEqual("\"emit-args-server\"", result2);
            Assert.IsNull(result3);
        }
    }
}
