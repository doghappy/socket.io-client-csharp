using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTests
{
    public abstract class SocketIOBaseTests
    {
        protected abstract string ServerUrl { get; }
        protected abstract string ServerTokenUrl { get; }
        protected abstract EngineIO EIO { get; }

        protected abstract SocketIOOptions CreateOptions();
        protected abstract SocketIO CreateSocketIO();

        protected SocketIO CreateSocketIO(SocketIOOptions options)
        {
            return new SocketIO(ServerUrl, options);
        }

        protected SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            return new SocketIO(ServerTokenUrl, options);
        }

        [ClassInitialize]
        public void InitailizeCheck()
        {
            var uri = new Uri(ServerUrl);
            using var tcp = new TcpClient();
            tcp.Connect(uri.Host, uri.Port);
            tcp.Close();
        }

        #region Id and Connected

        [TestMethod]
        public void Properties_Value_Should_Be_Correct_Before_Connected()
        {
            using var io = CreateSocketIO();

            io.Connected.Should().BeFalse();
            io.Id.Should().BeNull();
        }

        [TestMethod]
        public async Task Properties_Value_Should_Be_Changed_After_Connected()
        {
            using var io = CreateSocketIO();

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
            io.Id.Should().NotBeNull();
        }

        [TestMethod]
        public async Task Connect_Disconnect_2Times_Should_Be_Work()
        {
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
            });
            int times = 0;
            io.OnConnected += (s, e) => times++;

            await io.ConnectAsync();
            await Task.Delay(200);
            await io.DisconnectAsync();
            await io.ConnectAsync();
            await Task.Delay(200);

            times.Should().Be(2);
        }

        #endregion

        #region Emit

        [TestMethod]
        [DynamicData(nameof(Emit1ParameterCases), DynamicDataSourceType.Method)]
        public async Task Should_Be_Able_To_Emit_1_Parameters_And_Emit_Back(string eventName,
            object data,
            string expectedJson,
            IEnumerable<byte[]> expectedBytes)
        {
            using var io = CreateSocketIO();
            SocketIOResponse response = null;
            io.On(eventName, res => response = res);

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data);
            await Task.Delay(100);

            response.Should().NotBeNull();
            response.ToString().Should().Be(expectedJson);
            response.InComingBytes.Should().BeEquivalentTo(expectedBytes, options => options.WithStrictOrdering());
        }

        public static IEnumerable<object[]> Emit1ParameterCases()
        {
            return PrivateEmit1PrameterCase().Select(x => new[] { x.EventName, x.Data, x.ExpectedJson, x.ExpectedBytes });
        }

        private static IEnumerable<(string EventName, object Data, string ExpectedJson, IEnumerable<byte[]> ExpectedBytes)> PrivateEmit1PrameterCase()
        {
            return new (string EventName, object Data, string ExpectedJson, IEnumerable<byte[]> ExpectedBytes)[]
            {
                ("1:emit", null, "[null]", Array.Empty<byte[]>()),
                ("1:emit", true, "[true]", Array.Empty<byte[]>()),
                ("1:emit", false, "[false]", Array.Empty<byte[]>()),
                ("1:emit", -1234567890, "[-1234567890]", Array.Empty<byte[]>()),
                ("1:emit", 1234567890, "[1234567890]", Array.Empty<byte[]>()),
                ("1:emit", -1.234567890, "[-1.23456789]", Array.Empty<byte[]>()),
                ("1:emit", 1.234567890, "[1.23456789]", Array.Empty<byte[]>()),
                ("1:emit", "hello\n世界\n🌍🌎🌏", "[\"hello\\n世界\\n🌍🌎🌏\"]", Array.Empty<byte[]>()),
                ("1:emit", new { User = "abc", Password = "123" }, "[{\"User\":\"abc\",\"Password\":\"123\"}]", Array.Empty<byte[]>()),
                ("1:emit", new { Result = true, Data = new { User = "abc", Password = "123" } }, "[{\"Result\":true,\"Data\":{\"User\":\"abc\",\"Password\":\"123\"}}]", Array.Empty<byte[]>()),
                ("1:emit", new { Result = true, Data = new[] { "a", "b" } }, "[{\"Result\":true,\"Data\":[\"a\",\"b\"]}]", Array.Empty<byte[]>()),
                ("1:emit",
                    new { Result = true, Data = Encoding.UTF8.GetBytes("🦊🐶🐱") },
                    "[{\"Result\":true,\"Data\":{\"_placeholder\":true,\"num\":0}}]",
                    new[] { new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 } }),
                ("1:emit",
                    new { Result = Encoding.UTF8.GetBytes("test"), Data = Encoding.UTF8.GetBytes("🦊🐶🐱") },
                    "[{\"Result\":{\"_placeholder\":true,\"num\":0},\"Data\":{\"_placeholder\":true,\"num\":1}}]",
                    new[] { new byte[] { 0x74, 0x65, 0x73, 0x74 }, new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 } }),
            };
        }

        [TestMethod]
        [DataRow("2:emit", true, false, "[true,false]")]
        [DataRow("2:emit", -1234567890, 1234567890, "[-1234567890,1234567890]")]
        [DataRow("2:emit", -1.234567890, 1.234567890, "[-1.23456789,1.23456789]")]
        [DataRow("2:emit", "hello", "世界", "[\"hello\",\"世界\"]")]
        [DynamicData(nameof(Emit2ParameterCases), DynamicDataSourceType.Method)]
        public async Task Should_Be_Able_To_Emit_2_Parameters_And_Emit_Back(string eventName, object data1, object data2, string excepted)
        {
            using var io = CreateSocketIO();
            string json = null;
            io.On(eventName, res => json = res.ToString());

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data1, data2);
            await Task.Delay(100);

            json.Should().Be(excepted);
        }

        public static IEnumerable<object[]> Emit2ParameterCases()
        {
            return PrivateEmit2PrameterCase().Select(x => new[] { x.EventName, x.Data1, x.Data2, x.Expected });
        }

        private static IEnumerable<(string EventName, object Data1, object Data2, string Expected)> PrivateEmit2PrameterCase()
        {
            return new (string EventName, object Data1, object Data2, string Expected)[]
            {
                ("2:emit", new { User = "abc", Password = "123" }, "ok", "[{\"User\":\"abc\",\"Password\":\"123\"},\"ok\"]"),
                ("2:emit", new { Result = true, Data = new { User = "abc", Password = "123" } }, 789, "[{\"Result\":true,\"Data\":{\"User\":\"abc\",\"Password\":\"123\"}},789]"),
                ("2:emit", new { Result = true, Data = new[] { "a", "b" } }, new[] { 1, 2 }, "[{\"Result\":true,\"Data\":[\"a\",\"b\"]},[1,2]]"),
            };
        }

        [TestMethod]
        [DataRow("1:ack", "ack", "[\"ack\"]")]
        public async Task Should_Be_Able_To_Emit_1_Parameters_And_Ack(string eventName, object data, string excepted)
        {
            using var io = CreateSocketIO();
            string? json = null;

            await io.ConnectAsync();
            await io.EmitAsync(eventName, res => json = res.ToString(), data);
            await Task.Delay(100);

            json.Should().Be(excepted);
        }

        #endregion

        #region Callback

        [TestMethod]
        public async Task Callback_Should_Be_Work()
        {
            string guid = Guid.NewGuid().ToString();
            string resText = null;
            using var io = CreateSocketIO();
            io.On("callback_step2", async res => await res.CallbackAsync(guid));
            io.On("callback_step3", res => resText = res.GetValue<string>());

            await io.ConnectAsync();
            await io.EmitAsync("callback_step1", async res => { await res.CallbackAsync(guid); });
            await Task.Delay(100);

            resText.Should().Be(guid + "-server");
        }

        #endregion

        #region Query

        [TestMethod]
        public async Task Should_Connect_Successfully_If_Token_Is_Correct()
        {
            int times = 0;
            using var io = CreateTokenSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
                Query = new Dictionary<string, string>
                {
                    { "token", "abc" }
                }
            });
            io.OnConnected += (s, e) => times++;

            await io.ConnectAsync();
            await Task.Delay(100);

            times.Should().Be(1);
        }

        [TestMethod]
        public async Task Should_Connect_Failed_If_Token_Is_Incorrect()
        {
            int times = 0;
            string error = null;
            using var io = CreateTokenSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
                Query = new Dictionary<string, string>
                {
                    { "token", "abc123" }
                }
            });
            io.OnConnected += (s, e) => times++;
            io.OnError += (s, e) => error = e;

            await io.Invoking(i => i.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();

            times.Should().Be(0);
            error.Should().Be("Authentication error");
        }

        #endregion

        #region Auth

        [TestMethod]
        public async Task Should_Be_Able_To_Get_Auth()
        {
            if (EIO == EngineIO.V3)
            {
                return;
            }
            UserDTO auth = null;
            string guid = Guid.NewGuid().ToString();
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
                Auth = new UserDTO
                {
                    Name = "test",
                    Password = guid
                }
            });

            await io.ConnectAsync();
            await io.EmitAsync("get_auth", res => auth = res.GetValue<UserDTO>());
            await Task.Delay(100);

            auth.Name.Should().Be("test");
            auth.Password.Should().Be(guid);
        }

        class UserDTO
        {
            public string Name { get; set; }
            public string Password { get; set; }
        }

        #endregion

        #region Disconnect

        [TestMethod]
        public async Task Disconnect_Should_Be_Work_Even_If_Not_Connected()
        {
            using var io = CreateSocketIO();
            await io.DisconnectAsync();
        }

        [TestMethod]
        public async Task Should_Can_Disconnect_From_Client()
        {
            int times = 0;
            string reason = null;
            using var io = CreateSocketIO();
            io.OnDisconnected += (s, e) =>
            {
                times++;
                reason = e;
            };
            await io.ConnectAsync();
            await io.DisconnectAsync();

            times.Should().Be(1);
            reason.Should().Be(DisconnectReason.IOClientDisconnect);
            io.Id.Should().BeNull();
            io.Connected.Should().BeFalse();
        }

        [TestMethod]
        public async Task Should_Can_Disconnect_From_Server()
        {
            int times = 0;
            string reason = null;
            using var io = CreateSocketIO();
            io.OnDisconnected += (s, e) =>
            {
                times++;
                reason = e;
            };
            await io.ConnectAsync();
            await io.EmitAsync("disconnect", false);
            await Task.Delay(100);

            times.Should().Be(1);
            reason.Should().Be(DisconnectReason.IOServerDisconnect);
            io.Id.Should().BeNull();
            io.Connected.Should().BeFalse();
        }

        #endregion

        #region Reconnect

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public async Task Manually_Reconnect_Should_Be_Work(int times)
        {
            int connectTimes = 0;
            int disconnectTimes = 0;
            using var io = CreateSocketIO();
            io.OnConnected += (s, e) => connectTimes++;
            io.OnDisconnected += (s, e) => disconnectTimes++;

            for (int i = 0; i < times; i++)
            {
                await io.ConnectAsync();
                await io.DisconnectAsync();
            }

            connectTimes.Should().Be(times);
            disconnectTimes.Should().Be(times);
        }

        #endregion

        [TestMethod]
        [DataRow("CustomHeader", "CustomHeader-Value")]
        [DataRow("User-Agent", "dotnet-socketio[client]/socket")]
        [DataRow("user-agent", "dotnet-socketio[client]/socket")]
        public async Task ExtraHeaders(string key, string value)
        {
            string actual = null;
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { key, value },
                },
            });

            await io.ConnectAsync();
            await io.EmitAsync("get_header", 
                res => actual = res.GetValue<string>(), 
                key.ToLower());
            await Task.Delay(100);

            actual.Should().Be(value);
        }

        #region OnAny OffAny Off

        [TestMethod]
        public async Task OnAny_Should_Be_Work()
        {
            string eventName = null;
            string text = null;
            using var io = CreateSocketIO();
            io.OnAny((e, r) =>
            {
                eventName = e;
                text = r.GetValue<string>();
            });

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", "OnAny");
            await Task.Delay(100);

            eventName.Should().Be("1:emit");
            text.Should().Be("OnAny");
        }

        [TestMethod]
        public async Task OffAny_Should_Be_Work()
        {
            bool onAnyCalled = false;
            bool onCalled = false;
            using var io = CreateSocketIO();
            io.OnAny((e, r) => onAnyCalled = true);
            io.On("1:emit", res => onCalled = true);
            io.OffAny(io.ListenersAny()[0]);

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", "test");
            await Task.Delay(100);

            onAnyCalled.Should().BeFalse();
            onCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task Off_Should_Be_Work()
        {
            bool onAnyCalled = false;
            bool onCalled = false;
            using var io = CreateSocketIO();
            io.OnAny((e, r) => onAnyCalled = true);
            io.On("1:emit", res => onCalled = true);
            io.Off("1:emit");

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", "test");
            await Task.Delay(100);

            onAnyCalled.Should().BeTrue();
            onCalled.Should().BeFalse();
        }

        #endregion
    }
}