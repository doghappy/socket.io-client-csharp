using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTests
{
    public abstract class SocketIOBaseTests
    {
        public const string V4_WS = "http://localhost:11400";
        public const string V4_NSP_WS = "http://localhost:11400/nsp";
        public const string V4_WS_TOKEN = "http://localhost:11410";
        public const string V4_NSP_WS_TOKEN = "http://localhost:11410/nsp";

        public const string V4_HTTP = "http://localhost:11401";
        public const string V4_NSP_HTTP = "http://localhost:11401/nsp";
        public const string V4_HTTP_TOKEN = "http://localhost:11411";
        public const string V4_NSP_HTTP_TOKEN = "http://localhost:11411/nsp";

        public const string V3_WS = "http://localhost:11300";
        public const string V3_NSP_WS = "http://localhost:11300/nsp";
        public const string V3_WS_TOKEN = "http://localhost:11310";
        public const string V3_NSP_WS_TOKEN = "http://localhost:11310/nsp";

        public const string V3_HTTP = "http://localhost:11301";
        public const string V3_NSP_HTTP = "http://localhost:11301/nsp";
        public const string V3_HTTP_TOKEN = "http://localhost:11311";
        public const string V3_NSP_HTTP_TOKEN = "http://localhost:11311/nsp";

        public const string V2_WS = "http://localhost:11200";
        public const string V2_NSP_WS = "http://localhost:11200/nsp";
        public const string V2_WS_TOKEN = "http://localhost:11210";
        public const string V2_NSP_WS_TOKEN = "http://localhost:11410/nsp";

        public const string V2_HTTP = "http://localhost:11201";
        public const string V2_NSP_HTTP = "http://localhost:11201/nsp";
        public const string V2_HTTP_TOKEN = "http://localhost:11211";
        public const string V2_NSP_HTTP_TOKEN = "http://localhost:11211/nsp";

        protected abstract string ServerUrl { get; }
        protected abstract string ServerTokenUrl { get; }

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
                Reconnection = false
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
        [DataRow("1:emit", null, "[null]")]
        [DataRow("1:emit", true, "[true]")]
        [DataRow("1:emit", false, "[false]")]
        [DataRow("1:emit", -1234567890, "[-1234567890]")]
        [DataRow("1:emit", 1234567890, "[1234567890]")]
        [DataRow("1:emit", -1.234567890, "[-1.23456789]")]
        [DataRow("1:emit", 1.234567890, "[1.23456789]")]
        [DataRow("1:emit", "hello\n世界\n🌍🌎🌏", "[\"hello\\n世界\\n🌍🌎🌏\"]")]
        [DynamicData(nameof(Emit1ParameterCases), DynamicDataSourceType.Method)]
        public async Task Should_Be_Able_To_Emit_1_Parameters_And_Emit_Back(string eventName, object data, string excepted)
        {
            using var io = CreateSocketIO();
            string json = null;
            io.On(eventName, res => json = res.ToString());

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data);
            await Task.Delay(100);

            json.Should().Be(excepted);
        }

        public static IEnumerable<object[]> Emit1ParameterCases()
        {
            return PrivateEmit1PrameterCase().Select(x => new[] { x.EventName, x.Data, x.Expected });
        }

        private static IEnumerable<(string EventName, object Data, string Expected)> PrivateEmit1PrameterCase()
        {
            return new (string EventName, object Data, string Expected)[]
            {
                ("1:emit", new { User = "abc", Password = "123" }, "[{\"User\":\"abc\",\"Password\":\"123\"}]"),
                ("1:emit", new { Result = true, Data = new { User = "abc", Password = "123" } }, "[{\"Result\":true,\"Data\":{\"User\":\"abc\",\"Password\":\"123\"}}]"),
                ("1:emit", new { Result = true, Data = new[] { "a", "b" } }, "[{\"Result\":true,\"Data\":[\"a\",\"b\"]}]"),
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
            await io.EmitAsync("callback_step1", async res =>
            {
                await res.CallbackAsync(guid);
            });
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
                Query = new Dictionary<string, string>
                {
                    { "token", "abc123" }
                }
            });
            io.OnConnected += (s, e) => times++;
            io.OnError += (s, e) => error = e;

            await io.ConnectAsync();
            await Task.Delay(100);

            times.Should().Be(0);
            error.Should().Be("Authentication error");
        }
        #endregion

        #region Auth
        [TestMethod]
        public async Task Should_Be_Able_To_Get_Auth()
        {
            UserDTO auth = null;
            string guid = Guid.NewGuid().ToString();
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
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
        public async Task Manually_Reconnect_Should_Be_Work()
        {
            int connectTimes = 0;
            int disconnectTimes = 0;
            using var io = CreateSocketIO();
            io.OnConnected += (s, e) => connectTimes++;
            io.OnDisconnected += (s, e) => disconnectTimes++;
            await io.ConnectAsync();
            await io.DisconnectAsync();
            await io.ConnectAsync();
            await io.DisconnectAsync();

            connectTimes.Should().Be(2);
            disconnectTimes.Should().Be(2);
        }
        #endregion

        #region Header
        [TestMethod]
        public async Task Should_Can_Get_Headers()
        {
            string response = null;
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { "CustomHeader", "CustomHeader-Value" }
                }
            });

            await io.ConnectAsync();
            await io.EmitAsync("get_headers", res => response = res.GetValue<string>());
            await Task.Delay(100);

            response.Should().Be("CustomHeader-Value");
        }
        #endregion

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