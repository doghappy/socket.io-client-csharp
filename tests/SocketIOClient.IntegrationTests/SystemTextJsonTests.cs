using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Serializer.Tests.Models;
using SocketIOClient.IntegrationTests.Utils;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public abstract class SystemTextJsonTests : SocketIOTests
    {
        protected override SocketIO CreateSocketIO()
        {
            var options = new SocketIOOptions
            {
                EIO = EIO,
                AutoUpgrade = false,
                Reconnection = false,
                Transport = Transport,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            };
            return CreateSocketIO(options);
        }

        protected override SocketIO CreateSocketIO(SocketIOOptions options)
        {
            options.EIO = EIO;
            options.Transport = Transport;
            return new SocketIO(ServerUrl, options);
        }

        protected override SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            options.EIO = EIO;
            options.Transport = Transport;
            return new SocketIO(ServerTokenUrl, options);
        }

        protected override void ConfigureSerializerForEmitting1Parameter(SocketIO io)
        {
            io.ConfigureSystemTextJsonSerializerForEmitting1Parameter();
        }

        protected override IEnumerable<(
            string EventName,
            object Data,
            string ExpectedJson,
            IEnumerable<byte[]>? ExpectedBytes
            )> Emit1ParameterTupleCases => new (
            string EventName,
            object Data,
            string ExpectedJson,
            IEnumerable<byte[]>? ExpectedBytes)[]
            {
                ("1:emit", null!, "[null]", null),
                ("1:emit", true, "[true]", null),
                ("1:emit", false, "[false]", null),
                ("1:emit", -1234567890, "[-1234567890]", null),
                ("1:emit", 1234567890, "[1234567890]", null),
                ("1:emit", -1.234567890, "[-1.23456789]", null),
                ("1:emit", 1.234567890, "[1.23456789]", null),
                ("1:emit", "hello\n世界\n🌍🌎🌏", "[\"hello\\n世界\\n\\uD83C\\uDF0D\\uD83C\\uDF0E\\uD83C\\uDF0F\"]", null),
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
                    "[{\"Result\":true,\"Data\":{\"_placeholder\":true,\"num\":0}}]",
                    new[] { new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 } }),
                ("1:emit",
                    new { Result = "test"u8.ToArray(), Data = "🦊🐶🐱"u8.ToArray() },
                    "[{\"Result\":{\"_placeholder\":true,\"num\":0},\"Data\":{\"_placeholder\":true,\"num\":1}}]",
                    new[]
                    {
                        new byte[] { 0x74, 0x65, 0x73, 0x74 },
                        new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 }
                    }),
            };

        protected override IEnumerable<(object Data, string Expected, List<byte[]> Bytes)> AckCases =>
            new (object Data, string Expected, List<byte[]> Bytes)[]
            {
                ("ack", "[\"ack\"]", null!),
                (FileDto.IndexHtml,
                    "[{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]",
                    new List<byte[]> { FileDto.IndexHtml.Bytes })
            };
    }
}