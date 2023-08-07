using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using SocketIO.Core;
using SocketIO.Serializer.SystemTextJson;
using SocketIO.Serializer.Tests.Models;

namespace SocketIOClient.IntegrationTests
{
    public abstract class SocketIOTests
    {
        protected abstract string ServerUrl { get; }
        protected abstract string ServerTokenUrl { get; }
        protected abstract EngineIO EIO { get; }
        protected abstract TransportProtocol Transport { get; }

        protected abstract SocketIO CreateSocketIO();
        protected abstract SocketIO CreateSocketIO(SocketIOOptions options);
        protected abstract SocketIO CreateTokenSocketIO(SocketIOOptions options);

        [ClassInitialize]
        public void InitializeCheck()
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
        [Timeout(2000)]
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
            // await Task.Delay(200);
            await io.DisconnectAsync();
            await io.ConnectAsync();
            // await Task.Delay(200);

            times.Should().Be(2);
        }

        #endregion

        #region Emit

        [TestMethod]
        public async Task Should_emit_1_parameter_and_emit_back()
        {
            // NOTE: [DynamicData] only supports getting test cases from static method.
            foreach (var testCase in Emit1ParameterTupleCases)
            {
                await Should_emit_1_parameter_and_emit_back(
                    testCase.EventName,
                    testCase.Data,
                    testCase.ExpectedJson,
                    testCase.ExpectedBytes);
            }
        }

        protected abstract void ConfigureSerializerForEmitting1Parameter(SocketIO io);

        private async Task Should_emit_1_parameter_and_emit_back(
            string eventName,
            object data,
            string expectedJson,
            IEnumerable<byte[]>? expectedBytes)
        {
            using var io = CreateSocketIO();
            // io.Serializer = new SystemTextJsonSerializer(new JsonSerializerOptions
            // {
            //     Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs)
            // }, io.Options.EIO);
            ConfigureSerializerForEmitting1Parameter(io);

            SocketIOResponse? response = null;
            io.On(eventName, res => response = res);

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data);
            await Task.Delay(100);

            response.Should().NotBeNull();
            response!.ToString().Should().Be(expectedJson);
            response.InComingBytes.Should().BeEquivalentTo(
                expectedBytes,
                options => options.WithStrictOrdering());
        }

        public IEnumerable<object?[]> Emit1ParameterCases => Emit1ParameterTupleCases
            .Select((x, caseId) => new[]
            {
                caseId,
                x.EventName,
                x.Data,
                x.ExpectedJson,
                x.ExpectedBytes
            });

        protected abstract IEnumerable<(
            string EventName,
            object Data,
            string ExpectedJson,
            IEnumerable<byte[]>? ExpectedBytes
            )> Emit1ParameterTupleCases { get; }

        [TestMethod]
        [DataRow("2:emit", true, false, "[true,false]")]
        [DataRow("2:emit", -1234567890, 1234567890, "[-1234567890,1234567890]")]
        [DataRow("2:emit", -1.234567890, 1.234567890, "[-1.23456789,1.23456789]")]
        [DataRow("2:emit", "hello", "世界", "[\"hello\",\"世界\"]")]
        [DynamicData(nameof(Emit2ParameterCases), DynamicDataSourceType.Method)]
        public async Task Should_emit_2_parameters_and_emit_back(string eventName,
            object data1,
            object data2,
            string excepted)
        {
            using var io = CreateSocketIO();
            // io.Serializer = new SystemTextJsonSerializer(new JsonSerializerOptions
            // {
            //     Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs)
            // }, io.Options.EIO);
            ConfigureSerializerForEmitting1Parameter(io);

            string? json = null;
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

        private static IEnumerable<(string EventName, object Data1, object Data2, string Expected)>
            PrivateEmit2PrameterCase()
        {
            return new (string EventName, object Data1, object Data2, string Expected)[]
            {
                ("2:emit", new { User = "abc", Password = "123" }, "ok",
                    "[{\"User\":\"abc\",\"Password\":\"123\"},\"ok\"]"),
                ("2:emit", new { Result = true, Data = new { User = "abc", Password = "123" } }, 789,
                    "[{\"Result\":true,\"Data\":{\"User\":\"abc\",\"Password\":\"123\"}},789]"),
                ("2:emit", new { Result = true, Data = new[] { "a", "b" } }, new[] { 1, 2 },
                    "[{\"Result\":true,\"Data\":[\"a\",\"b\"]},[1,2]]"),
            };
        }

        protected abstract IEnumerable<(object Data, string Expected, List<byte[]> Bytes)> AckCases { get; }

        [TestMethod]
        public async Task Emit_1_data_then_execute_ack_on_client()
        {
            using var io = CreateSocketIO();

            await io.ConnectAsync();

            foreach (var item in AckCases)
            {
                SocketIOResponse? response = null;
                await io.EmitAsync("1:ack", res => response = res, item.Data);
                await Task.Delay(1000);

                response!.ToString().Should().Be(item.Expected);
                response!.InComingBytes.Should().BeEquivalentTo(item.Bytes);
            }
        }

        #endregion

        #region Callback

        [TestMethod]
        public async Task Client_should_about_to_call_CallbackAsync()
        {
            var text = nameof(Client_should_about_to_call_CallbackAsync);
            string? resText = null;
            using var io = CreateSocketIO();
            io.On("client sending data to server", async res => await res.CallbackAsync(text));
            io.On("server received data", res => resText = res.GetValue<string>());

            await io.ConnectAsync();
            await io.EmitAsync("client will be sending data to server");
            await Task.Delay(100);

            resText.Should().Be(nameof(Client_should_about_to_call_CallbackAsync));
        }

        [TestMethod]
        public async Task Client_should_about_to_call_CallbackAsync_with_bytes()
        {
            SocketIOResponse? response = null;
            using var io = CreateSocketIO();
            io.On("client sending data to server", async res => await res.CallbackAsync(FileDto.IndexHtml));
            io.On("server received data", res => response = res);

            await io.ConnectAsync();
            await io.EmitAsync("client will be sending data to server");
            await Task.Delay(100);

            var test = response!.GetValue<FileDto>();
            var text = "0x" + BitConverter.ToString(test.Bytes).Replace("-", ", 0x");
            response!.GetValue<FileDto>().Should().BeEquivalentTo(FileDto.IndexHtml);
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
        public async Task Should_trigger_OnError_if_token_is_incorrect()
        {
            var times = 0;
            string? error = null;
            using var io = CreateTokenSocketIO(new SocketIOOptions
            {
                Transport = Transport,
                AutoUpgrade = false,
                Reconnection = false,
                EIO = EIO,
                Query = new Dictionary<string, string>
                {
                    { "token", "abc123" }
                }
            });
            io.OnConnected += (_, _) => times++;
            io.OnError += (_, e) => error = e;

            await io.ConnectAsync();

            times.Should().Be(0);
            error.Should().Be("Authentication error");
        }

        #endregion

        #region Auth

        [TestMethod]
        public virtual async Task Should_get_auth()
        {
            if (EIO == EngineIO.V3)
            {
                return;
            }

            UserPasswordDto? auth = null;
            var guid = Guid.NewGuid().ToString();
            using var io = CreateSocketIO(new SocketIOOptions
            {
                Reconnection = false,
                EIO = EIO,
                Auth = new UserPasswordDto
                {
                    User = "test",
                    Password = guid
                },
                ConnectionTimeout = TimeSpan.FromSeconds(1)
            });

            await io.ConnectAsync();
            await io.EmitAsync("get_auth", res => auth = res.GetValue<UserPasswordDto>());
            await Task.Delay(100);

            auth!.User.Should().Be("test");
            auth.Password.Should().Be(guid);
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
            var times = 0;
            string? reason = null;
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
            var times = 0;
            string? reason = null;
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

        [TestMethod]
        [DataRow(false, 0, 0, 1)]
        [DataRow(true, 0, 0, 1)]
        [DataRow(true, 1, 1, 2)]
        [DataRow(true, 5, 5, 6)]
        public async Task Should_trigger_OnReconnectAttempt_and_OnReconnectFailed(
            bool reconnection,
            int attempts,
            int expectedAttempts,
            int expectedErrors)
        {
            using var io = new SocketIO("http://localhost:4404", new SocketIOOptions
            {
                Reconnection = reconnection,
                ReconnectionAttempts = attempts,
            });
            var attemptTimes = 0;
            var failedTimes = 0;
            var errorTimes = 0;
            io.OnReconnectAttempt += (_, _) => attemptTimes++;
            io.OnReconnectFailed += (_, _) => failedTimes++;
            io.OnReconnectError += (_, _) => errorTimes++;
            await io.Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();
            attemptTimes.Should().Be(expectedAttempts);
            errorTimes.Should().Be(expectedErrors);
            failedTimes.Should().Be(1);
        }

        [TestMethod]
        public virtual async Task Should_keep_trying_to_reconnect()
        {
            using var io = new SocketIO("http://localhost:4404", new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                Reconnection = true,
                ReconnectionDelay = 1000,
                RandomizationFactor = 0.5,
                ReconnectionDelayMax = 10000,
                ReconnectionAttempts = 5,
            });
            var times = 0;
            io.OnReconnectAttempt += (_, _) => times++;
            await io.Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();
            times.Should().Be(5);
        }

        #endregion

        [TestMethod]
        [DataRow("CustomHeader", "CustomHeader-Value")]
        [DataRow("User-Agent", "dotnet-socketio[client]/socket")]
        [DataRow("user-agent", "dotnet-socketio[client]/socket")]
        public async Task ExtraHeaders(string key, string value)
        {
            string? actual = null;
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
        public async Task OnAny_should_be_triggered()
        {
            string? eventName = null;
            string? text = null;
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

        [TestMethod]
        [DataRow(4000, 0, 0)]
        [DataRow(5100, 1, 1)]
        [DataRow(10100, 2, 2)]
        public async Task Ping_pong_test(int ms, int expectedPingTimes, int expectedPongTimes)
        {
            var pingTimes = 0;
            var pongTimes = 0;
            using var io = CreateSocketIO();
            io.OnPing += (_, _) => pingTimes++;
            io.OnPong += (_, _) => pongTimes++;
            await io.ConnectAsync();
            await Task.Delay(ms);
            pingTimes.Should().Be(expectedPingTimes);
            pongTimes.Should().Be(expectedPongTimes);
        }
    }
}