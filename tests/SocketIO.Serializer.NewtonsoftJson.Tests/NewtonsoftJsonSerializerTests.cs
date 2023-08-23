using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SocketIO.Core;
using SocketIO.Serializer.Core;
using SocketIO.Serializer.Tests.Models;

namespace SocketIO.Serializer.NewtonsoftJson.Tests;

public class NewtonsoftJsonSerializerTests
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
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\"]"
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
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\",null]"
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
                        Type = SerializedMessageType.Text,
                        Text = "42/nsp,[\"test\"]"
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
                        Type = SerializedMessageType.Text,
                        Text = "42/nsp,[\"test\",true]"
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
                        Type = SerializedMessageType.Text,
                        Text = "42/nsp,[\"test\",true,false,123]"
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
                        Type = SerializedMessageType.Text,
                        Text = "452-/nsp,[\"test\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]"
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
        var serializer = new NewtonsoftJsonSerializer();
        var items = serializer.Serialize(EngineIO.V4, eventName, ns, data);
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
                (null, EngineIO.V3, null, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, null, null)!,
                (null, EngineIO.V3, new { userId = 1 }, new Dictionary<string, string>
                {
                    ["hello"] = "world"
                }, null),
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
                    }),
                (null, EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40"
                })!,
                ("/test", EngineIO.V4, null, null, new SerializedItem
                {
                    Text = "40/test,"
                })!,
                (null, EngineIO.V4, new { userId = 1 }, null, new SerializedItem
                {
                    Text = "40{\"userId\":1}"
                })!,
                ("/test", EngineIO.V4, new { userId = 1 }, null, new SerializedItem
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
        object? auth,
        IEnumerable<KeyValuePair<string, string>> queries,
        SerializedItem? expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.SerializeConnectedMessage(eio, ns, auth, queries)
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
                    JsonArray = JArray.Parse("[\"V3: onAny\"]")
                }),
            (
                EngineIO.V4,
                "42/test,[\"hi\",\"V3: onAny\"]",
                new
                {
                    Type = MessageType.Event,
                    Event = "hi",
                    Namespace = "/test",
                    JsonArray = JArray.Parse("[\"V3: onAny\"]")
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
                    JsonArray = JArray.Parse("[]")
                }),
            (
                EngineIO.V4,
                "431[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    JsonArray = JArray.Parse("[\"nice\"]")
                }),
            (
                EngineIO.V4,
                "43/test,1[\"nice\"]",
                new
                {
                    Type = MessageType.Ack,
                    Id = 1,
                    Namespace = "/test",
                    JsonArray = JArray.Parse("[\"nice\"]")
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
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Deserialize(eio, text)
            .Should().BeEquivalentTo(expected, options => options
                .Using<JArray>(x => x.Subject.ToString().Should().Be(x.Expectation.ToString()))
                .WhenTypeIs<JArray>());
    }

    private static IEnumerable<(EngineIO eio, List<SerializedItem> items, List<object> expected)>
        DeserializeEioBinaryTupleCases =>
        new (EngineIO eio, List<SerializedItem> items, List<object> expected)[]
        {
            (
                EngineIO.V3,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    }
                },
                new List<object>()),
            (
                EngineIO.V3,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        BinaryCount = 1,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        BinaryCount = 1,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V3,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        Namespace = "/test",
                        BinaryCount = 1,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-/test,[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        Namespace = "/test",
                        BinaryCount = 1,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V3,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        BinaryCount = 1,
                        Id = 30,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-30[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        BinaryCount = 1,
                        Id = 30,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-/test,30[\"1 params\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.Binary,
                        Namespace = "/test",
                        BinaryCount = 1,
                        Id = 30,
                        Event = "1 params",
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "461-6[{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.BinaryAck,
                        BinaryCount = 1,
                        Id = 6,
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
            (
                EngineIO.V4,
                new List<SerializedItem>
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "461-/test,6[{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[1]
                    }
                },
                new List<object>
                {
                    new
                    {
                        Type = MessageType.BinaryAck,
                        BinaryCount = 1,
                        Namespace = "/test",
                        Id = 6,
                        JsonArray = JArray.Parse("[{\"_placeholder\":true,\"num\":0}]")
                    }
                }),
        };

    public static IEnumerable<object[]> DeserializeEioBinaryCases => DeserializeEioBinaryTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eio,
            x.items,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeEioBinaryCases))]
    public void Should_deserialize_eio_and_binary(
        int caseId,
        EngineIO eio,
        List<SerializedItem> items,
        List<object> expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        var list = new List<IMessage>();
        foreach (var item in items)
        {
            var message = item.Type == SerializedMessageType.Text
                ? serializer.Deserialize(eio, item.Text)
                : serializer.Deserialize(eio, item.Binary);
            if (message is not null)
            {
                list.Add(message);
            }
        }

        list.Should().BeEquivalentTo(expected, options => options
            .Using<JArray>(x => x.Subject.ToString().Should().Be(x.Expectation.ToString()))
            .WhenTypeIs<JArray>());
    }

    private static IEnumerable<(
        IMessage message,
        int index,
        JsonSerializerSettings options,
        object expected)> DeserializeGenericMethodTupleCases =>
        new (IMessage message, int index, JsonSerializerSettings options, object expected)[]
        {
            (new JsonMessage(MessageType.Event)
            {
                ReceivedText = "[\"event\",1]"
            }, 0, null, 1)!,
            (new JsonMessage(MessageType.Event)
            {
                ReceivedText = "[\"event\",\"hello\"]"
            }, 0, null, "hello")!,
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                }, 1, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    }
                },
                new UserPasswordDto
                {
                    User = "admin",
                    Password = "test"
                }),
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText =
                        "[\"event\",{\"_placeholder\":true,\"num\":0},{\"size\":2023,\"name\":\"test.txt\",\"bytes\":{\"_placeholder\":true,\"num\":1}}]",
                    ReceivedBinary = new List<byte[]> { "hello world!"u8.ToArray(), "🐮🍺"u8.ToArray() }
                }, 0, null, "hello world!"u8.ToArray())!,
            (
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText =
                        "[\"event\",{\"_placeholder\":true,\"num\":0},{\"size\":2023,\"name\":\"test.txt\",\"bytes\":{\"_placeholder\":true,\"num\":1}}]",
                    ReceivedBinary = new List<byte[]> { "hello world!"u8.ToArray(), "🐮🍺"u8.ToArray() }
                }, 1, new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    }
                }, new FileDto
                {
                    Size = 2023,
                    Name = "test.txt",
                    Bytes = "🐮🍺"u8.ToArray()
                }),
        };

    public static IEnumerable<object?[]> DeserializeGenericMethodCases => DeserializeGenericMethodTupleCases
        .Select((x, caseId) => new[]
        {
            caseId,
            x.message,
            x.index,
            x.options,
            x.expected
        });

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_generic_type_by_message_and_index(
        int caseId,
        IMessage message,
        int index,
        JsonSerializerSettings options,
        object expected)
    {
        var serializer = new NewtonsoftJsonSerializer(options);
        var actual = serializer.GetType()
            .GetMethod(
                nameof(NewtonsoftJsonSerializer.Deserialize),
                BindingFlags.Public | BindingFlags.Instance,
                new[] { typeof(IMessage), typeof(int) })!
            .MakeGenericMethod(expected.GetType())
            .Invoke(serializer, new object?[] { message, index });
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(DeserializeGenericMethodCases))]
    public void Should_deserialize_non_generic_type_by_message_and_index_and_type(
        int caseId,
        IMessage message,
        int index,
        JsonSerializerSettings options,
        object expected)
    {
        var serializer = new NewtonsoftJsonSerializer(options);
        var actual = serializer.Deserialize(message, index, expected.GetType());
        actual.Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        int packetId,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializePacketIdNamespaceDataTupleCases =>
        new (int packetId, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            (0, null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "430[\"string\",1,true,null]"
                    }
                }),
            (23, "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "43/test,23[\"string\",1,true,null]"
                    }
                }),
            (8964, null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "461-8964[123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
            (8964, "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "461-/test,8964[123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
        };

    public static IEnumerable<object?[]> SerializePacketIdNamespaceDataCases => SerializePacketIdNamespaceDataTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.packetId,
            x.ns,
            x.data,
            x.expected
        });

    [Theory]
    [MemberData(nameof(SerializePacketIdNamespaceDataCases))]
    public void Should_serialize_packet_id_and_namespace_and_data(
        int caseId,
        int packetId,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Serialize(EngineIO.V4, packetId, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        string eventName,
        int packetId,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializeEventPacketIdNamespaceDataTupleCases =>
        new (string eventName, int packetId, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            ("event", 0, null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "420[\"event\",\"string\",1,true,null]"
                    }
                }),
            ("event", 23, "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42/test,23[\"event\",\"string\",1,true,null]"
                    }
                }),
            ("event", 8964, null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-8964[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
            ("event", 8964, "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-/test,8964[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
        };

    public static IEnumerable<object?[]> SerializeEventPacketIdNamespaceDataCases =>
        SerializeEventPacketIdNamespaceDataTupleCases
            .Select((x, caseId) => new object?[]
            {
                caseId,
                x.eventName,
                x.packetId,
                x.ns,
                x.data,
                x.expected
            });

    [Theory]
    [MemberData(nameof(SerializeEventPacketIdNamespaceDataCases))]
    public void Should_serialize_event_packet_id_and_namespace_and_data(
        int caseId,
        string eventName,
        int packetId,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Serialize(EngineIO.V4, eventName, packetId, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        string eventName,
        string? ns,
        object?[] data,
        List<SerializedItem> expected)> SerializeEventNamespaceDataTupleCases =>
        new (string eventName, string? ns, object?[] data, List<SerializedItem> expected)[]
        {
            ("event", null, new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"event\",\"string\",1,true,null]"
                    }
                }),
            ("event", "/test", new object?[] { "string", 1, true, null },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42/test,[\"event\",\"string\",1,true,null]"
                    }
                }),
            ("event", null, new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
            ("event", "/test", new object?[]
                {
                    123456.789,
                    new UserPasswordDto
                    {
                        User = "test",
                        Password = "hello"
                    },
                    new FileDto
                    {
                        Size = 2023,
                        Name = "test.txt",
                        Bytes = "🐮🍺"u8.ToArray()
                    }
                },
                new()
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text =
                            "451-/test,[\"event\",123456.789,{\"User\":\"test\",\"Password\":\"hello\"},{\"Size\":2023,\"Name\":\"test.txt\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = "🐮🍺"u8.ToArray()
                    }
                }),
        };

    public static IEnumerable<object?[]> SerializeEventNamespaceDataCases =>
        SerializeEventNamespaceDataTupleCases
            .Select((x, caseId) => new object?[]
            {
                caseId,
                x.eventName,
                x.ns,
                x.data,
                x.expected
            });

    [Theory]
    [MemberData(nameof(SerializeEventNamespaceDataCases))]
    public void Should_serialize_event_and_namespace_and_data(
        int caseId,
        string eventName,
        string ns,
        object[] data,
        List<SerializedItem> expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Serialize(EngineIO.V4, eventName, ns, data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<(
        JsonSerializerSettings? options,
        IMessage message,
        string expected)> MessageToJsonTupleCases =>
        new (JsonSerializerSettings? options, IMessage message, string expected)[]
        {
            (
                null,
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",1]"
                },
                "[1]"),
            (
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                },
                new JsonMessage(MessageType.Event)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                },
                @"[
  ""hello"",
  {
    ""user"": ""admin"",
    ""password"": ""test""
  }
]"),
            (
                null,
                new JsonMessage(MessageType.Ack)
                {
                    ReceivedText = "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"
                },
                "[\"event\",\"hello\",{\"user\":\"admin\",\"password\":\"test\"}]"),
        };

    public static IEnumerable<object?[]> MessageToJsonCases => MessageToJsonTupleCases
        .Select((x, caseId) => new object?[]
        {
            caseId,
            x.options,
            x.message,
            x.expected
        });

    [Theory]
    [MemberData(nameof(MessageToJsonCases))]
    public void Message_should_be_able_to_json(
        int caseId,
        JsonSerializerSettings? options,
        IMessage message,
        string expected)
    {
        var serializer = new NewtonsoftJsonSerializer(options);
        serializer.MessageToJson(message)
            .Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Should_throw_Exception_if_data_depth_greater_than_configured_when_serializing()
    {
        var options = new JsonSerializerSettings
        {
            MaxDepth = 1
        };
        var serializer = new NewtonsoftJsonSerializer(options);
        var message = new JsonMessage(MessageType.Ack)
        {
            ReceivedText = "[{\"value\":1,\"next\":{\"value\":2}}]"
        };
        serializer.Invoking(s => s.Deserialize<Depth>(message, 0))
            .Should()
            .Throw<JsonReaderException>()
            .WithMessage("The reader's MaxDepth of 1 has been exceeded. Path '[0].next', line 1, position 19.");
    }
}