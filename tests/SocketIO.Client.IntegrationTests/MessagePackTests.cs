﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Serializer.Tests.Models;
using SocketIO.Client.IntegrationTests.Utils;

namespace SocketIO.Client.IntegrationTests
{
    [TestClass]
    public abstract class MessagePackTests : SocketIOTests
    {
        protected override SocketIOClient CreateSocketIO()
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

        protected override SocketIOClient CreateSocketIO(SocketIOOptions options)
        {
            options.EIO = EIO;
            options.Transport = Transport;
            var io = new SocketIOClient(ServerUrl, options);
            io.SetMessagePackSerializer();
            return io;
        }

        protected override SocketIOClient CreateTokenSocketIO(SocketIOOptions options)
        {
            options.EIO = EIO;
            options.Transport = Transport;
            var io = new SocketIOClient(ServerTokenUrl, options);
            io.SetMessagePackSerializer();
            return io;
        }

        protected override void ConfigureSerializerForEmitting1Parameter(SocketIOClient io)
        {
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
                    null),
                ("1:emit",
                    new { Result = "test"u8.ToArray(), Data = "🦊🐶🐱"u8.ToArray() },
                    "[{\"Result\":\"dGVzdA==\",\"Data\":\"8J+mivCfkLbwn5Cx\"}]",
                    null),
            };

        protected override IEnumerable<(object Data, string Expected, List<byte[]> Bytes)> AckCases =>
            new (object Data, string Expected, List<byte[]> Bytes)[]
            {
                ("ack", "[\"ack\"]", null!),
                (FileDto.IndexHtml,
                    "[{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":\"SGVsbG8gV29ybGQh\"}]",
                    null!)
            };
    }
}