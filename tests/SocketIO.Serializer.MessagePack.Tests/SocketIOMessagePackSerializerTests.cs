using MessagePack;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.MessagePack.Tests;

public class SocketIOMessagePackSerializerTests
{
    private static IEnumerable<(
            string eventName,
            string ns,
            object[] data,
            IEnumerable<SerializedItem> expectedItems)>
        SerializeTupleCases =>
        new (string eventName, string ns, object[] data, IEnumerable<SerializedItem> expectedItems)[]
        {
            (
                "test",
                string.Empty,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object> { "test" }
                        })
                    }
                }),
            (
                "test",
                string.Empty,
                new object[1],
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object> { "test", null! }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test" }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[] { true },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test", true }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[] { true, false, 123 },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object> { "test", true, false, 123 }
                        })
                    }
                }),
            (
                "test",
                string.Empty,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/",
                            Data = new List<object>
                            {
                                "test",
                                new byte[] { 1, 2, 3 }
                            }
                        })
                    }
                }),
            (
                "test",
                "/nsp",
                new object[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = MessagePackSerializer.Serialize(new PackMessage2
                        {
                            Type = PackMessageType.Event,
                            Nsp = "/nsp",
                            Data = new List<object>
                            {
                                "test",
                                new byte[] { 1, 2, 3 },
                                new byte[] { 4, 5, 6 }
                            }
                        })
                    }
                }),
        };

    public static IEnumerable<object[]> SerializeCases => SerializeTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eventName,
            x.ns,
            x.data,
            x.expectedItems
        });

    [Theory]
    [MemberData(nameof(SerializeCases))]
    public void Should_serialize_given_event_message(
        int caseId,
        string eventName,
        string ns,
        object[] data,
        IEnumerable<SerializedItem> expectedItems)
    {
        var serializer = new SocketIOMessagePackSerializer();
        var items = serializer.Serialize(eventName, ns, data);
        items.Should().BeEquivalentTo(expectedItems, config => config.WithStrictOrdering());
    }

    private static IEnumerable<(
            string? ns,
            EngineIO eio,
            object? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)>
        SerializeConnectedTupleCases =>
        new (
            string? ns,
            EngineIO eio,
            object? auth,
            IEnumerable<KeyValuePair<string, string>> queries,
            SerializedItem? expected)[]
            {
                // https://msgpack.solder.party/
                (null, EngineIO.V3, null, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, new Dictionary<string, string>
                {
                    ["hello"] = "world"
                }, null),
                ("/test", EngineIO.V3, null, null, new()
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73,
                        0x74
                    }
                })!,
                ("/test", EngineIO.V3, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        // {"type":0,"query":"key=value","nsp":"/test?key=value"}
                        Binary = new byte[]
                        {
                            0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA5, 0x71, 0x75, 0x65, 0x72, 0x79, 0xA9, 0x6B,
                            0x65, 0x79, 0x3D, 0x76, 0x61, 0x6C, 0x75, 0x65, 0xA3, 0x6E, 0x73, 0x70, 0xAF, 0x2F, 0x74,
                            0x65, 0x73, 0x74, 0x3F, 0x6B, 0x65, 0x79, 0x3D, 0x76, 0x61, 0x6C, 0x75, 0x65
                        }
                    }),
                (null, EngineIO.V4, null, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":null,"nsp":"/"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E, 0x73,
                        0x70, 0xA1, 0x2F
                    }
                })!,
                ("/test", EngineIO.V4, null, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":null,"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E, 0x73,
                        0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                    }
                })!,
                (null, EngineIO.V4, new { userId = 1 }, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":{"userId":1},"nsp":"/"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x81, 0xA6, 0x75, 0x73,
                        0x65, 0x72, 0x49, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                    }
                })!,
                ("/test", EngineIO.V4, new { userId = 1 }, null, new SerializedItem
                {
                    Type = SerializedMessageType.Binary,
                    // {"type":0,"data":{"userId":1},"nsp":"/test"}
                    Binary = new byte[]
                    {
                        0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x81, 0xA6, 0x75, 0x73,
                        0x65, 0x72, 0x49, 0x64, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
                    }
                })!,
                (null, EngineIO.V4, null,
                    new Dictionary<string, string>
                    {
                        ["key"] = "value"
                    },
                    new SerializedItem
                    {
                        Type = SerializedMessageType.Binary,
                        // {"type":0,"data":null,"nsp":"/"}
                        Binary = new byte[]
                        {
                            0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x00, 0xA4, 0x64, 0x61, 0x74, 0x61, 0xC0, 0xA3, 0x6E,
                            0x73, 0x70, 0xA1, 0x2F
                        }
                    }),
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
        object auth,
        IEnumerable<KeyValuePair<string, string>> queries,
        SerializedItem? expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.SerializeConnectedMessage(ns, eio, auth, queries)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, string? text, object? expected)> DeserializeTextTupleCases =>
        new (EngineIO eio, string? text, object? expected)[]
        {
            // (EngineIO.V3, string.Empty, null),
            // (EngineIO.V3, "hello", null),
            // (EngineIO.V4, "2", new { Type = MessageType.Ping }),
            // (EngineIO.V4, "3", new { Type = MessageType.Pong }),
            // (
            //     EngineIO.V4,
            //     "0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}",
            //     new
            //     {
            //         Type = MessageType.Opened,
            //         Sid = "wOuAvDB9Jj6yE0VrAL8N",
            //         PingInterval = 25000,
            //         PingTimeout = 30000,
            //         Upgrades = new List<string> { "websocket" }
            //     }),
            // (EngineIO.V3, "4{\"type\":0,\"nsp\":\"/\"}", new { Type = MessageType.Connected }),
            // (EngineIO.V3, "4{\"type\":0,\"nsp\":\"/test\"}", new { Type = MessageType.Connected, Namespace = "/test" }),
            // (
            //     EngineIO.V3,
            //     "4{\"type\":0,\"query\":\"token=eio3\",\"nsp\":\"/test?token=eio3\"}",
            //     new
            //     {
            //         Type = MessageType.Connected,
            //         Namespace = "/test?token=eio3"
            //     }),
            // (
            //     EngineIO.V4,
            //     "4{\"type\":0,\"data\":{\"sid\":\"test-id\"}}",
            //     new
            //     {
            //         Type = MessageType.Connected,
            //         Sid = "test-id"
            //     }),
            // (
            //     EngineIO.V4,
            //     "4{\"type\":0,\"data\":{\"sid\":\"test-id\"},\"nsp\":\"/test\"}",
            //     new
            //     {
            //         Type = MessageType.Connected,
            //         Sid = "test-id",
            //         Namespace = "/test"
            //     }),

            /*
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
             */
            // (
            //     EngineIO.V3,
            //     "4{\"type\":4,\"data\":\"Authentication error\",\"nsp\":\"/\"}",
            //     new
            //     {
            //         Type = MessageType.Error,
            //         Error = "Authentication error"
            //     }),
            /*
               (
                  EngineIO.V4,
                  "44{\"message\":\"Authentication error\"}",
                  new
                  {
                      Type = MessageType.Error,
                      Error = "Authentication error"
                  }),
              (
                  EngineIO.V4,
                  "44/test,{\"message\":\"Authentication error\"}",
                  new
                  {
                      Type = MessageType.Error,
                      Namespace = "/test",
                      Error = "Authentication error"
                  }),
             */
        };

    public static IEnumerable<object?[]> DeserializeTextCases => DeserializeTextTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.text,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeTextCases))]
    public void Should_deserialize_text(
        int caseId,
        EngineIO eio,
        string? text,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.Deserialize(eio, text)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(EngineIO eio, byte[] binary, object? expected)> DeserializeBinaryTupleCases =>
        new (EngineIO eio, byte[] binary, object? expected)[]
        {
            // (
            //     EngineIO.V4,
            //     new byte[] { 0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F },
            //     new
            //     {
            //         Type = MessageType.Disconnected,
            //         Namespace = "/"
            //     }),
            // (
            //     EngineIO.V4,
            //     new byte[]
            //     {
            //         0x82, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x01, 0xA3, 0x6E, 0x73, 0x70, 0xA5, 0x2F, 0x74, 0x65, 0x73, 0x74
            //     },
            //     new
            //     {
            //         Type = MessageType.Disconnected,
            //         Namespace = "/test"
            //     }),
            (
                EngineIO.V4,
                new byte[]
                {
                    0x83, 0xA4, 0x74, 0x79, 0x70, 0x65, 0x02, 0xA4, 0x64, 0x61, 0x74, 0x61, 0x92, 0xA2, 0x68, 0x69, 0xA9, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x2E, 0x69, 0x6F, 0xA3, 0x6E, 0x73, 0x70, 0xA1, 0x2F
                },
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Data = new object[] { "socket.io" },
                    Namespace = "/"
                }),
        };

    public static IEnumerable<object?[]> DeserializeBinaryCases => DeserializeBinaryTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.eio,
            x.binary,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeBinaryCases))]
    public void Should_deserialize_binary(
        int caseId,
        EngineIO eio,
        byte[] binary,
        object expected)
    {
        var serializer = new SocketIOMessagePackSerializer();
        serializer.Deserialize(eio, binary)
            .Should().BeEquivalentTo(expected);
    }
}