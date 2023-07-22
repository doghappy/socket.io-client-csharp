using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIO.Serializer.MessagePack;
using SocketIO.Serializer.Tests.Models;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    public abstract class WebSocketMpTests : WebSocketTests
    {
        protected override SocketIO CreateSocketIO(SocketIOOptions options)
        {
            var io = new SocketIO(ServerUrl, options);
            SetSerializer(io);
            return io;
        }

        protected override SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            var io = new SocketIO(ServerTokenUrl, options);
            SetSerializer(io);
            return io;
        }

        private void SetSerializer(SocketIO io)
        {
            io.Serializer = new SocketIOMessagePackSerializer(ContractlessStandardResolver.Options, io.Options.EIO);
        }

        [TestMethod]
        [DynamicData(nameof(Emit1ParameterCases), DynamicDataSourceType.Method)]
        public new async Task Should_emit_1_parameter_and_emit_back(
            int caseId,
            string eventName,
            object data,
            string expectedJson,
            IEnumerable<byte[]>? expectedBytes)
        {
            using var io = CreateSocketIO();

            SocketIOResponse? response = null;
            io.On(eventName, res => response = res);

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data);
            await Task.Delay(100);

            response.Should().NotBeNull();
            response!.ToString().Should().Be(expectedJson);
            response.InComingBytes.Should().BeNull();
        }

        public static IEnumerable<object?[]> Emit1ParameterCases()
        {
            return Emit1ParameterTupleCase()
                .Select((x, caseId) => new[]
                {
                    caseId,
                    x.EventName,
                    x.Data,
                    x.ExpectedJson,
                    x.ExpectedBytes
                });
        }

        private static IEnumerable<(
            string EventName,
            object Data,
            string ExpectedJson,
            IEnumerable<byte[]>? ExpectedBytes)> Emit1ParameterTupleCase()
        {
            return new (string EventName, object Data, string ExpectedJson, IEnumerable<byte[]>? ExpectedBytes)[]
            {
                ("1:emit", null!, "[null]", null),
                ("1:emit", true, "[true]", null),
                ("1:emit", false, "[false]", null),
                ("1:emit", -1234567890, "[-1234567890]", null),
                ("1:emit", 1234567890, "[1234567890]", null),
                ("1:emit", -1.234567890, "[-1.23456789]", null),
                ("1:emit", 1.234567890, "[1.23456789]", null),
                ("1:emit", "hello\n世界\n🌍🌎🌏", "[\"hello\\n世界\\n🌍🌎🌏\"]", null),
                ("1:emit", new { User = "abc", Password = "123" }, "[{\"User\":\"abc\",\"Password\":\"123\"}]", null),
                ("1:emit",
                    new { Result = true, Data = new { User = "abc", Password = "123" } },
                    "[{\"Result\":true,\"Data\":{\"User\":\"abc\",\"Password\":\"123\"}}]",
                    null),
                ("1:emit",
                    new { Result = true, Data = new[] { "a", "b" } },
                    "[{\"Result\":true,\"Data\":[\"a\",\"b\"]}]",
                    null),
                ("1:emit",
                    new { Result = true, Data = "🦊🐶🐱"u8.ToArray() },
                    "[{\"Result\":true,\"Data\":\"8J+mivCfkLbwn5Cx\"}]",
                    new[] { new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 } }),
                ("1:emit",
                    new { Result = "test"u8.ToArray(), Data = "🦊🐶🐱"u8.ToArray() },
                    "[{\"Result\":\"dGVzdA==\",\"Data\":\"8J+mivCfkLbwn5Cx\"}]",
                    new[]
                    {
                        new byte[] { 0x74, 0x65, 0x73, 0x74 },
                        new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 }
                    }),
            };
        }
        
        [TestMethod]
        public override async Task Should_keep_trying_to_reconnect()
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
            SetSerializer(io);
            var times = 0;
            io.OnReconnectAttempt += (_, _) => times++;
            await io.Invoking(async x => await x.ConnectAsync())
                .Should()
                .ThrowAsync<ConnectionException>();
            times.Should().Be(5);
        }
    }
}