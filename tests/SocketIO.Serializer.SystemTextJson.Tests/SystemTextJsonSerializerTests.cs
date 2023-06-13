using System.Text.Json.Nodes;
using FluentAssertions.Equivalency;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.SystemTextJson.Tests;

public class SystemTextJsonSerializerTests
{
    private static IEnumerable<(
            string eventName,
            string ns,
            EngineIO eio,
            object[] data,
            IEnumerable<SerializedItem> expectedItems)>
        SerializeTupleCases =>
        new (string eventName, string ns, EngineIO eio, object[] data, IEnumerable<SerializedItem> expectedItems)[]
        {
            (
                "test",
                string.Empty,
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\"]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[1],
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\",null]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\"]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\",true]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true, false, 123 },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp,[\"test\",true,false,123]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "452-nsp,[\"test\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 4, 5, 6 }
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V4,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
        };

    public static IEnumerable<object[]> SerializeCases => SerializeTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eventName,
            x.ns,
            x.eio,
            x.data,
            x.expectedItems
        });

    [Theory]
    [MemberData(nameof(SerializeCases))]
    public void Should_serialize_given_event_message(
        int caseId,
        string eventName,
        string ns,
        EngineIO eio,
        object[] data,
        IEnumerable<SerializedItem> expectedItems)
    {
        var serializer = new SystemTextJsonSerializer();
        var items = serializer.Serialize(eventName, ns, data);
        items.Should().BeEquivalentTo(expectedItems, config => config.WithStrictOrdering());
    }

    private static IEnumerable<(
            string? ns,
            EngineIO eio,
            string? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)>
        SerializeConnectedTupleCases =>
        new (string? ns, EngineIO eio, string? auth, IEnumerable<KeyValuePair<string, string>> queries, SerializedItem?
            expected)[]
            {
                (null, EngineIO.V3, null, null, new SerializedItem())!,
                (null, EngineIO.V3, "{\"userId\":1}", null, new SerializedItem())!,
                (null, EngineIO.V3, "{\"userId\":1}", new Dictionary<string, string>
                {
                    ["hello"] = "world"
                }, new SerializedItem())!,
                ("/test", EngineIO.V3, null, null, new SerializedItem
                {
                    Text = "40/test,"
                })!,
                ("/test", EngineIO.V3, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Text = "40/test?key=value,"
                    })!,
                (null, EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40"
                })!,
                ("/test", EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40/test,"
                })!,
                (null, EngineIO.V4, "{\"userId\":1}", null, new SerializedItem
                {
                    Text = "40{\"userId\":1}"
                })!,
                ("/test", EngineIO.V4, "{\"userId\":1}", null, new SerializedItem
                {
                    Text = "40/test,{\"userId\":1}"
                })!,
                (null, EngineIO.V4, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Text = "40"
                    })!,
            };

    public static IEnumerable<object?[]> SerializeConnectedCases => SerializeConnectedTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.ns,
            x.eio,
            x.auth,
            x.queries,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializeConnectedCases))]
    public void Should_serialize_connected_messages(
        int caseId,
        string? ns,
        EngineIO eio,
        string? auth,
        IEnumerable<KeyValuePair<string, string>> queries,
        SerializedItem? expected)
    {
        var serializer = new SystemTextJsonSerializer();
        serializer.SerializeConnectedMessage(ns, eio, auth, queries)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, string? text, object? expected)> DeserializeEioTextTupleCases =>
        new (EngineIO eio, string? text, object? expected)[]
        {
            (EngineIO.V3, string.Empty, null),
            (EngineIO.V3, "hello", null),
            (EngineIO.V4, "2", new { Type = MessageType.Ping }),
            (EngineIO.V4, "3", new { Type = MessageType.Pong }),
            (
                EngineIO.V4,
                "0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}",
                new
                {
                    Type = MessageType.Opened,
                    Sid = "wOuAvDB9Jj6yE0VrAL8N",
                    PingInterval = 25000,
                    PingTimeout = 30000,
                    Upgrades = new List<string> { "websocket" }
                }),
            (EngineIO.V3, "40", new { Type = MessageType.Connected }),
            (EngineIO.V3, "40/test,", new { Type = MessageType.Connected, Namespace = "/test" }),
            (
                EngineIO.V3,
                "40/test?token=eio3,",
                new
                {
                    Type = MessageType.Connected,
                    Namespace = "/test"
                }),
            (
                EngineIO.V4,
                "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "aMA_EmVTuzpgR16PAc4w"
                }),
            (
                EngineIO.V4,
                "40/test,{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}",
                new
                {
                    Type = MessageType.Connected,
                    Sid = "aMA_EmVTuzpgR16PAc4w",
                    Namespace = "/test"
                }),
            (EngineIO.V4, "41", new { Type = MessageType.Disconnected }),
            (EngineIO.V4, "41/test,", new { Type = MessageType.Disconnected, Namespace = "/test" }),
            (
                EngineIO.V4,
                "42[\"hi\",\"V3: onAny\"]",
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    JsonArray = JsonNode.Parse("[\"V3: onAny\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "42/test,[\"hi\",\"V3: onAny\"]",
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Namespace = "/test",
                    JsonArray = JsonNode.Parse("[\"V3: onAny\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "42/test,17[\"cool\"]",
                new
                {
                    Type = MessageType.Event,
                    Id = 17,
                    Namespace = "/test",
                    Event = "cool",
                    JsonArray = JsonNode.Parse("[]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "431[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    JsonArray = JsonNode.Parse("[\"nice\"]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "43/test,1[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    Namespace = "/test",
                    JsonArray = JsonNode.Parse("[\"nice\"]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "44\"Authentication error2\"",
                new
                {
                    Type = MessageType.Error,
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V4,
                "44{\"message\":\"Authentication error2\"}",
                new
                {
                    Type = MessageType.Error,
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V4,
                "44/test,{\"message\":\"Authentication error2\"}",
                new
                {
                    Type = MessageType.Error,
                    Namespace = "/test",
                    Error = "Authentication error2"
                }),
            (
                EngineIO.V3,
                "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V3,
                "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "451-/test,30[\"1 params\",{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.Binary,
                    Namespace = "/test",
                    BinaryCount = 1,
                    Id = 30,
                    Event = "1 params",
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "461-6[{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.BinaryAck,
                    BinaryCount = 1,
                    Id = 6,
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
            (
                EngineIO.V4,
                "461-/test,6[{\"_placeholder\":true,\"num\":0}]",
                new
                {
                    Type = MessageType.BinaryAck,
                    BinaryCount = 1,
                    Namespace = "/test",
                    Id = 6,
                    JsonArray = JsonNode.Parse("[{\"_placeholder\":true,\"num\":0}]")!.AsArray()
                }),
        };

    public static IEnumerable<object?[]> DeserializeEioTextCases => DeserializeEioTextTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.text,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeEioTextCases))]
    public void Should_deserialize_eio_and_text(int caseId, EngineIO eio, string? text, object? expected)
    {
        var excludingProps = new[]
        {
            nameof(JsonMessage.JsonArray)
        };
        var serializer = new SystemTextJsonSerializer();
        serializer.Deserialize(eio, text)
            .Should().BeEquivalentTo(expected, options => options
                .Using<JsonArray>(x => x.Subject.ToJsonString().Should().Be(x.Expectation.ToJsonString()))
                .WhenTypeIs<JsonArray>());
    }
}