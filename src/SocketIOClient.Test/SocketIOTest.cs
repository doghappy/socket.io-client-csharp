using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SocketIOClient.EventArguments;
using SocketIOClient.Test.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIOTest
    {
        const string Uri = "http://localhost:11000/";

        [TestMethod]
        public async Task EventHiTest()
        {
            string result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi .net core, You are connected to the server", result);
        }

        [TestMethod]
        public async Task OffTest()
        {
            string result = null;
            int hiCount = 0;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                hiCount++;
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            Assert.AreEqual("hi .net core, You are connected to the server", result);
            Assert.AreEqual(1, hiCount);

            client.Off("hi");
            await client.EmitAsync("hi", ".net core");
            await client.DisconnectAsync();
            Assert.AreEqual(1, hiCount);
        }

        [TestMethod]
        public async Task OnReceivedEventTest()
        {
            ReceivedEventArgs args = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "unit test");
            };
            client.OnReceivedEvent += (sender, e) => args = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi", args.Event);
            Assert.AreEqual("hi unit test, You are connected to the server", args.Response.GetValue<string>());
        }

        [TestMethod]
        public async Task OnConnectedTest()
        {
            bool result = false;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += (sender, e) => result = true;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task EventAckTest()
        {
            JToken result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("ack", response =>
                {
                    result = response.GetValue();
                }, ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(result.Value<bool>("result"));
            Assert.AreEqual("ack(.net core)", result.Value<string>("message"));
        }

        [TestMethod]
        public async Task BinaryEventTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("bytes", response => result = response.GetValue<ByteResponse>());

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("bytes", name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }

        [TestMethod]
        public async Task ServerDisconectTest()
        {
            string reason = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("sever disconnect", false);
            };
            client.OnDisconnected += (sender, e) => reason = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("io server disconnect", reason);
        }

        [TestMethod]
        public async Task BinaryAckTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("binary ack", response =>
                {
                    result = response.GetValue<ByteResponse>();
                }, name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }

        [TestMethod]
        public async Task EventChangeTest()
        {
            string resVal1 = null;
            ChangeResponse resVal2 = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("change", new
                {
                    code = 200,
                    message = "val1"
                }, "val2");
            };
            client.On("change", response =>
            {
                resVal1 = response.GetValue<string>();
                resVal2 = response.GetValue<ChangeResponse>(1);
            });
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("val2", resVal1);
            Assert.AreEqual(200, resVal2.Code);
            Assert.AreEqual("val1", resVal2.Message);
        }

        [TestMethod]
        public async Task OnReceivedBinaryEventTest()
        {
            ReceivedEventArgs args = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnReceivedEvent += (sender, e) => args = e;

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("bytes", name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("bytes", args.Event);

            var result = args.Response.GetValue<ByteResponse>();
            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }

        [TestMethod]
        public async Task ClientMessageCallbackTest()
        {
            SocketIOResponse res = null;
            bool called = false;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("client message callback", "SocketIOClient.Test");
            };
            client.On("client message callback", async response =>
            {
                res = response;
                await response.CallbackAsync();
            });
            client.On("server message callback called", response => called = true);
            await client.ConnectAsync();
            await Task.Delay(400);
            await client.DisconnectAsync();

            Assert.IsTrue(called);
            Assert.AreEqual("SocketIOClient.Test - server", res.GetValue<string>());
        }

        [TestMethod]
        public async Task ClientBinaryCallbackTest()
        {
            SocketIOResponse res = null;
            bool called = false;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                byte[] bytes = Encoding.UTF8.GetBytes("SocketIOClient.Test");
                await client.EmitAsync("client binary callback", bytes);
            };
            client.On("client binary callback", async response =>
            {
                res = response;
                await response.CallbackAsync();
            });
            client.On("server binary callback called", response => called = true);
            await client.ConnectAsync();
            await Task.Delay(400);
            await client.DisconnectAsync();

            Assert.IsTrue(called);
            byte[] resBytes = res.GetValue<byte[]>();
            Assert.AreEqual("SocketIOClient.Test - server", Encoding.UTF8.GetString(resBytes));
        }

        [TestMethod]
        public async Task ReconnectionTrueTest()
        {
            int hiCount = 0;
            string res = null;
            int disconnectionCount = 0;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                res = response.GetValue<string>();
                hiCount++;
            });

            client.OnDisconnected += (sender, e) =>
            {
                disconnectionCount++;
            };

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "SocketIOClient.Test");
                await Task.Delay(10);
                if (hiCount >= 2)
                {
                    await client.DisconnectAsync();
                }
                else
                {
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(2400);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.AreEqual(2, hiCount);
            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual("hi SocketIOClient.Test, You are connected to the server", res);
        }

        [TestMethod]
        public async Task ReconnectionFalseTest()
        {
            int hiCount = 0;
            string res = null;
            int disconnectionCount = 0;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("hi", response =>
            {
                res = response.GetValue<string>();
                hiCount++;
            });

            client.OnDisconnected += (sender, e) =>
            {
                disconnectionCount++;
            };

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", "SocketIOClient.Test");
                await Task.Delay(10);
                if (hiCount >= 2)
                {
                    await client.DisconnectAsync();
                }
                else
                {
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(1000);

            Assert.IsFalse(client.Connected);
            Assert.IsTrue(client.Disconnected);
            Assert.AreEqual(1, hiCount);
            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual("hi SocketIOClient.Test, You are connected to the server", res);
        }

        [TestMethod]
        public async Task ReconnectingTest()
        {
            int disconnectionCount = 0;
            int reconnectingCount = 0;
            int attempt = 0;
            bool connectedFlag = false;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnDisconnected += (sender, e) => disconnectionCount++;

            client.OnReconnecting += (sender, e) =>
            {
                reconnectingCount++;
                attempt = e;
            };

            client.OnConnected += async (sender, e) =>
            {
                if (!connectedFlag)
                {
                    await Task.Delay(200);
                    connectedFlag = true;
                    await client.EmitAsync("sever disconnect", true);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(2400);
            await client.DisconnectAsync();

            Assert.AreEqual(1, disconnectionCount);
            Assert.AreEqual(1, reconnectingCount);
            Assert.AreEqual(1, attempt);
        }

        [TestMethod]
        public async Task ConcurrencySendTest()
        {
            int endIndex = -1;
            int bytesCallbackCount = 0;
            var client = new SocketIO("https://socket-io.doghappy.wang", new SocketIOOptions
            {
                Reconnection = false,
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("bytes", response => bytesCallbackCount++);

            client.OnConnected += async (sender, e) =>
            {
                string data = File.ReadAllText("Files/data.txt");
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await Task.Factory.StartNew(async () =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        await client.EmitAsync("bytes", "c#", new
                        {
                            source = "client007",
                            bytes = buffer
                        });
                        //await Task.Delay(20);
                    }
                });

                for (int i = 0; i < 100; i++)
                {
                    await client.EmitAsync("hi", i);
                    endIndex = i;
                    //await Task.Delay(20);
                }
            };
            await client.ConnectAsync();
            await Task.Delay(10000);

            Assert.AreEqual(99, endIndex);
            Assert.AreEqual(100, bytesCallbackCount);
            Assert.IsTrue(client.Connected);
            await client.DisconnectAsync();
        }
    }
}
