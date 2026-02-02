using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using SocketIOClient.Common;
using SocketIOClient.Common.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.Serializer.SystemTextJson;
using SocketIOClient.Test.Core;

namespace SocketIOClient.UnitTests.Serializer.SystemTextJson;

public class SystemJsonSerializerTests
{
    private readonly Decapsulator _realDecapsulator = new();

    private SystemJsonSerializer NewSystemJsonSerializer(IEngineIOMessageAdapter engineIOMessageAdapter)
    {
        return NewSystemJsonSerializer(engineIOMessageAdapter, new JsonSerializerOptions());
    }

    private SystemJsonSerializer NewSystemJsonSerializer(IEngineIOMessageAdapter engineIOMessageAdapter, JsonSerializerOptions options)
    {
        return NewSystemJsonSerializer(_realDecapsulator, engineIOMessageAdapter, options);
    }

    private SystemJsonSerializer NewSystemJsonSerializer(JsonSerializerOptions options)
    {
        return NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>(), options);
    }

    private static SystemJsonSerializer NewSystemJsonSerializer(
        IDecapsulable decapsulator,
        IEngineIOMessageAdapter engineIOMessageAdapter,
        JsonSerializerOptions options)
    {
        var serializer = new SystemJsonSerializer(decapsulator, options);
        serializer.SetEngineIOMessageAdapter(engineIOMessageAdapter);
        return serializer;
    }

    [Fact]
    public void SerializeData_InvalidData_ThrowArgumentNullException()
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Invoking(x => x.Serialize(null))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeData_DataIsEmpty_ThrowArgumentException()
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Invoking(x => x.Serialize([]))
            .Should()
            .Throw<ArgumentException>();
    }

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyNormalEventName = new(
        ["event"],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\"]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlySpecialEventName = new(
        ["Ab_@ '\"{}[].?-*/\\"],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"Ab_@ '\\\"{}[].?-*/\\\\\"]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWithNull = new(
        ["event", null!],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",null]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1String = new(
        ["event", "hello, world!"],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",\"hello, world!\"]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1True = new(
        ["event", true],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",true]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1False = new(
        ["event", false],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",false]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1IntMax = new(
        ["event", int.MaxValue],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",2147483647]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1FloatMax = new(
        ["event", float.MaxValue],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",3.4028235E+38]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1Object = new(
        ["event", new { id = 1, name = "Alice" }],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "42[\"event\",{\"id\":1,\"name\":\"Alice\"}]",
            },
        ]);

    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWithBytesAndObject = new(
        ["event", TestFile.IndexHtml, new { id = 1, name = "Alice" }],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "451-[\"event\",{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}},{\"id\":1,\"name\":\"Alice\"}]",
            },
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = "Hello World!"u8.ToArray(),
            },
        ]);


    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith2Bytes = new(
        ["event", TestFile.IndexHtml, TestFile.NiuB],
        [
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "452-[\"event\",{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}},{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":1}}]",
            },
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = "Hello World!"u8.ToArray(),
            },
            new ProtocolMessage
            {
                Type = ProtocolMessageType.Bytes,
                Bytes = "🐮🍺"u8.ToArray(),
            },
        ]);

    private static IEnumerable<(object[] input, IEnumerable<ProtocolMessage> output)> SerializeDataOnlyStrongTypeCases
    {
        get
        {
            yield return SerializeDataOnlyNormalEventName;
            yield return SerializeDataOnlySpecialEventName;
            yield return SerializeDataOnlyWithNull;
            yield return SerializeDataOnlyWith1String;
            yield return SerializeDataOnlyWith1True;
            yield return SerializeDataOnlyWith1False;
            yield return SerializeDataOnlyWith1IntMax;
            yield return SerializeDataOnlyWith1FloatMax;
            yield return SerializeDataOnlyWith1Object;
            yield return SerializeDataOnlyWithBytesAndObject;
            yield return SerializeDataOnlyWith2Bytes;
        }
    }

    public static TheoryData<object[], IEnumerable<ProtocolMessage>> SerializeDataOnlyCases
    {
        get
        {
            var data = new TheoryData<object[], IEnumerable<ProtocolMessage>>();
            foreach (var item in SerializeDataOnlyStrongTypeCases)
            {
                data.Add(item.input, item.output);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SerializeDataOnlyCases))]
    public void Serialize_DataOnly_AlwaysPass(object[] data, IEnumerable<ProtocolMessage> expected)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var serializer = NewSystemJsonSerializer(options);
        serializer.Serialize(data).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("/test", "42/test,[\"event\"]")]
    public void Serialize_NamespaceNoBytes_ContainsNamespaceIfExists(string? ns, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.Serialize(["event"]);
        list[0].Text.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", "451-/test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void Serialize_NamespaceWithBytes_ContainsNamespaceIfExists(string? ns, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.Serialize(["event", TestFile.NiuB.Bytes]);
        list[0].Text.Should().Be(expected);
    }

    private static readonly (string text, IMessage message) DeserializeOpenedMessage = new(
        "0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
        new OpenedMessage
        {
            Sid = "123",
            Upgrades = ["websocket"],
            PingInterval = 10000,
            PingTimeout = 5000,
        });

    private static readonly (string text, IMessage message) DeserializeEio3ConnectedMessage = new(
        "40/test,",
        new ConnectedMessage { Namespace = "/test" });

    private static readonly (string text, IMessage message) DeserializeEio4NoNamespaceConnectedMessage = new(
        "40{\"sid\":\"123\"}",
        new ConnectedMessage { Sid = "123" });

    private static readonly (string text, IMessage message) DeserializeEio4NamespaceConnectedMessage = new(
        "40/test,{\"sid\":\"123\"}",
        new ConnectedMessage { Sid = "123", Namespace = "/test" });

    private static readonly (string text, IMessage message) DeserializeEmptyStringReturnNull = new("", null!);

    private static readonly (string text, IMessage message) DeserializeUnsupportedTextReturnNull = new("unsupported text", null!);

    private static readonly (string text, IMessage message) DeserializePing = new("2", new PingMessage());

    private static readonly (string text, IMessage message) DeserializePong = new("3", new PongMessage());

    private static readonly (string text, IMessage message) DeserializeNamespaceDisconnectedMessage = new("41/test,", new DisconnectedMessage { Namespace = "/test" });

    private static readonly (string text, IEventMessage message) DeserializeIdNamespaceEventMessage = new(
        "42/test,2[\"event\"]",
        new SystemJsonEventMessage
        {
            Event = "event",
            Id = 2,
            Namespace = "/test",
        });

    private static readonly (string text, IMessage message) DeserializeEventMessageNull = new(
        "42[\"hello\",null]",
        new SystemJsonEventMessage
        {
            Event = "hello",
        });

    private static readonly (string text, IMessage message) DeserializeNamespaceAckMessage = new(
        "43/test,1[\"nice\"]",
        new SystemJsonAckMessage
        {
            Id = 1,
            Namespace = "/test",
        });

    private static readonly (string text, IMessage message) DeserializeEio3ErrorMessage = new(
        "44\"Authentication error\"",
        new ErrorMessage
        {
            Namespace = null,
            Error = "Authentication error",
        });

    private static readonly (string text, IMessage message) DeserializeEio4ErrorMessage = new(
        "44/test,{\"message\":\"Authentication error\"}",
        new ErrorMessage
        {
            Namespace = "/test",
            Error = "Authentication error",
        });

    private static readonly (string text, IMessage message) DeserializeBinaryAckMessage = new(
        "461-/test,2[{\"_placeholder\":true,\"num\":0}]",
        new SystemJsonBinaryAckMessage
        {
            Id = 2,
            Namespace = "/test",
            BytesCount = 1,
            Bytes = [],
        });

    public static TheoryData<string, IMessage> DeserializeEio3Cases =>
        new()
        {
            { DeserializeOpenedMessage.text, DeserializeOpenedMessage.message },
            { DeserializeEio3ConnectedMessage.text, DeserializeEio3ConnectedMessage.message },
            { DeserializeEmptyStringReturnNull.text, DeserializeEmptyStringReturnNull.message },
            { DeserializeUnsupportedTextReturnNull.text, DeserializeUnsupportedTextReturnNull.message },
            { DeserializePing.text, DeserializePing.message },
            { DeserializePong.text, DeserializePong.message },
            { DeserializeNamespaceDisconnectedMessage.text, DeserializeNamespaceDisconnectedMessage.message },
            { DeserializeEventMessageNull.text, DeserializeEventMessageNull.message },
            { DeserializeIdNamespaceEventMessage.text, DeserializeIdNamespaceEventMessage.message },
            { DeserializeNamespaceAckMessage.text, DeserializeNamespaceAckMessage.message },
            { DeserializeEio3ErrorMessage.text, DeserializeEio3ErrorMessage.message },
            { DeserializeBinaryAckMessage.text, DeserializeBinaryAckMessage.message },
        };

    [Theory]
    [MemberData(nameof(DeserializeEio3Cases))]
    public void Deserialize_EngineIO3MessageAdapter_ReturnMessage(string text, IMessage expected)
    {
        var serializer = NewSystemJsonSerializer(new SystemJsonEngineIO3MessageAdapter());
        var message = serializer.Deserialize(text);
        message.Should()
            .BeEquivalentTo(expected,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(p => p.Name == nameof(SystemJsonAckMessage.DataItems))
                    .Excluding(p => p.Name == nameof(SystemJsonAckMessage.RawText))
                    .Excluding(p => p.Name == nameof(ISystemJsonAckMessage.JsonSerializerOptions)));
    }

    public static TheoryData<string, IMessage> DeserializeEio4Cases =>
        new()
        {
            { DeserializeOpenedMessage.text, DeserializeOpenedMessage.message },
            { DeserializeEio4NoNamespaceConnectedMessage.text, DeserializeEio4NoNamespaceConnectedMessage.message },
            { DeserializeEio4NamespaceConnectedMessage.text, DeserializeEio4NamespaceConnectedMessage.message },
            { DeserializeEmptyStringReturnNull.text, DeserializeEmptyStringReturnNull.message },
            { DeserializeUnsupportedTextReturnNull.text, DeserializeUnsupportedTextReturnNull.message },
            { DeserializePing.text, DeserializePing.message },
            { DeserializePong.text, DeserializePong.message },
            { DeserializeNamespaceDisconnectedMessage.text, DeserializeNamespaceDisconnectedMessage.message },
            { DeserializeEventMessageNull.text, DeserializeEventMessageNull.message },
            { DeserializeIdNamespaceEventMessage.text, DeserializeIdNamespaceEventMessage.message },
            { DeserializeNamespaceAckMessage.text, DeserializeNamespaceAckMessage.message },
            { DeserializeEio4ErrorMessage.text, DeserializeEio4ErrorMessage.message },
            { DeserializeBinaryAckMessage.text, DeserializeBinaryAckMessage.message },
        };

    [Theory]
    [MemberData(nameof(DeserializeEio4Cases))]
    public void Deserialize_EngineIO4MessageAdapter_ReturnMessage(string text, IMessage expected)
    {
        var serializer = NewSystemJsonSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize(text);
        message.Should()
            .BeEquivalentTo(expected,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(p => p.Name == nameof(SystemJsonAckMessage.DataItems))
                    .Excluding(p => p.Name == nameof(SystemJsonAckMessage.RawText))
                    .Excluding(p => p.Name == nameof(ISystemJsonAckMessage.JsonSerializerOptions)));
    }

    private static readonly (string text, object item1) DeserializeEventMessageString = new(
        "42[\"hello\",\"world\"]", "world");

    private static readonly (string text, object item1) DeserializeEventMessageTrue = new(
        "42[\"hello\",true]", true);

    private static readonly (string text, object item1) DeserializeEventMessageFalse = new(
        "42[\"hello\",false]", false);

    private static readonly (string text, object item1) DeserializeEventMessageIntMax = new(
        "42[\"hello\",2147483647]", int.MaxValue);

    private static readonly (string text, object item1) DeserializeEventMessageFloatMax = new(
        "42[\"hello\",3.4028235E+38]", float.MaxValue);

    private static readonly (string text, object item1) DeserializeEventMessageObject = new(
        "42[\"event\",{\"id\":1,\"name\":\"Alice\"}]",
        new { id = 1, name = "Alice" });

    private static readonly (string text, object item1) DeserializeEventMessageIntArray = new(
        "42[\"event\",[1,2,3]]",
        new List<int> { 1, 2, 3 });

    private static readonly (string text, object item1) DeserializeAckMessageString = new("431[\"nice\"]", "nice");

    public static TheoryData<string, object> DeserializeEventMessage1ItemCases =>
        new()
        {
            { DeserializeEventMessageString.text, DeserializeEventMessageString.item1 },
            { DeserializeEventMessageTrue.text, DeserializeEventMessageTrue.item1 },
            { DeserializeEventMessageFalse.text, DeserializeEventMessageFalse.item1 },
            { DeserializeEventMessageIntMax.text, DeserializeEventMessageIntMax.item1 },
            { DeserializeEventMessageFloatMax.text, DeserializeEventMessageFloatMax.item1 },
            { DeserializeEventMessageObject.text, DeserializeEventMessageObject.item1 },
            { DeserializeEventMessageIntArray.text, DeserializeEventMessageIntArray.item1 },
            { DeserializeAckMessageString.text, DeserializeAckMessageString.item1 },
        };

    [Theory]
    [MemberData(nameof(DeserializeEventMessage1ItemCases))]
    public void Deserialize_EventMessage_Return1Data(string text, object expected)
    {
        var serializer = NewSystemJsonSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize(text) as IDataMessage;
        var item1 = message!.GetValue(expected.GetType(), 0);
        item1.Should().BeEquivalentTo(expected);
    }

    private static readonly (string text, object item1, object item2) DeserializeEventMessage2Strings = new(
        "42[\"hello\",\"world\", \"test\"]", "world", "test");

    private static readonly (string text, object item1, object item2) DeserializeEventMessageObjectInt = new(
        "42[\"hello\",{\"id\":1,\"name\":\"Alice\"},-123456]",
        new { id = 1, name = "Alice" },
        -123456);

    public static TheoryData<string, object, object> DeserializeEventMessage2ItemsCases =>
        new()
        {
            { DeserializeEventMessage2Strings.text, DeserializeEventMessage2Strings.item1, DeserializeEventMessage2Strings.item2 },
            { DeserializeEventMessageObjectInt.text, DeserializeEventMessageObjectInt.item1, DeserializeEventMessageObjectInt.item2 },
        };

    [Theory]
    [MemberData(nameof(DeserializeEventMessage2ItemsCases))]
    public void Deserialize_EventMessage_Return2Data(string text, object expected1, object expected2)
    {
        var serializer = NewSystemJsonSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = serializer.Deserialize(text) as IEventMessage;
        var item1 = message!.GetValue(expected1.GetType(), 0);
        item1.Should().BeEquivalentTo(expected1);

        var item2 = message.GetValue(expected2.GetType(), 1);
        item2.Should().BeEquivalentTo(expected2);
    }

    [Fact]
    public void Deserialize_DecapsulationResultFalse_ReturnNull()
    {
        var decapsulator = Substitute.For<IDecapsulable>();
        decapsulator.DecapsulateRawText(Arg.Any<string>())
            .Returns(new DecapsulationResult
            {
                Success = false,
            });
        var adapter = new SystemJsonEngineIO4MessageAdapter();
        var serializer = NewSystemJsonSerializer(decapsulator, adapter, new JsonSerializerOptions());
        const string text = "0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}";

        serializer.Deserialize(text).Should().BeNull();
    }

    public static TheoryData<string, byte[], object> DeserializeBinaryEventMessage1ItemCases =>
        new()
        {
            {
                DeserializeBinaryEventMessageBinaryCase.text,
                DeserializeBinaryEventMessageBinaryCase.bytes,
                DeserializeBinaryEventMessageBinaryCase.item1
            },
            {
                DeserializeBinaryAckMessageBinaryCase.text,
                DeserializeBinaryAckMessageBinaryCase.bytes,
                DeserializeBinaryAckMessageBinaryCase.item1
            },
        };

    private static readonly (string text, byte[] bytes, object item1) DeserializeBinaryEventMessageBinaryCase = new(
        "451-/test,30[\"event\",{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]",
        TestFile.NiuB.Bytes,
        TestFile.NiuB);

    private static readonly (string text, byte[] bytes, object item1) DeserializeBinaryAckMessageBinaryCase = new(
        "461-/test,30[{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":0}}]",
        TestFile.NiuB.Bytes,
        TestFile.NiuB);

    [Theory]
    [MemberData(nameof(DeserializeBinaryEventMessage1ItemCases))]
    public void DeserializeGenericType_BinaryEventMessage_ReturnNiuB(string text, byte[] bytes, object expected)
    {
        var serializer = NewSystemJsonSerializer(new SystemJsonEngineIO4MessageAdapter());
        var message = (IBinaryAckMessage)serializer.Deserialize(text);

        message.Add(bytes);
        var item1 = message.GetValue<TestFile>(0);
        item1.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("42[\"event\"]", "[\"event\"]")]
    [InlineData("421[\"event\"]", "[\"event\"]")]
    [InlineData("42/test,2[\"event\"]", "[\"event\"]")]
    [InlineData("43/test,1[\"nice\"]", "[\"nice\"]")]
    [InlineData("461-/test,2[{\"_placeholder\":true,\"num\":0}]", "[{\"_placeholder\":true,\"num\":0}]")]
    public void Deserialize_DataMessage_RawTextIsAlwaysExpected(string raw, string expected)
    {
        var serializer = NewSystemJsonSerializer(new JsonSerializerOptions());

        var message = (IDataMessage)serializer.Deserialize(raw);
        message.RawText.Should().Be(expected);
    }

    [Fact]
    public void SerializeDataAndId_DataIsNull_ThrowArgumentNullException()
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Invoking(x => x.Serialize(null, 1))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeDataAndId_DataIsEmpty_ThrowArgumentNullException()
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Invoking(x => x.Serialize([], 1))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, 1, "421[\"event\"]")]
    [InlineData("", 2, "422[\"event\"]")]
    [InlineData("/test", 3, "42/test,3[\"event\"]")]
    public void Serialize_WhenCalled_ReturnCorrectText(string? ns, int id, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.Serialize(["event"], id);
        list[0].Text.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 4, "451-4[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", 5, "451-5[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", 6, "451-/test,6[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void Serialize_WithBytes_ReturnCorrectText(string? ns, int id, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.Serialize(["event", TestFile.NiuB.Bytes], id);
        list[0].Text.Should().Be(expected);
    }

    [Fact]
    public void Serialize_CamelCase_ReturnCorrectJson()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var serializer = NewSystemJsonSerializer(options);
        var json = serializer.Serialize(new
        {
            User = "admin",
            Password = "123456",
        });
        json.Should().Be("{\"user\":\"admin\",\"password\":\"123456\"}");
    }

    [Theory]
    [InlineData(null, 1, "431[1,\"2\"]")]
    [InlineData("", 2, "432[1,\"2\"]")]
    [InlineData("/test", 3, "43/test,3[1,\"2\"]")]
    public void SerializeAckData_WhenCalled_ReturnCorrectText(string? ns, int id, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.SerializeAckData([1, "2"], id);
        list[0].Text.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 4, "461-4[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", 5, "461-5[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("/test", 6, "461-/test,6[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void SerializeAckData_WithBytes_ReturnCorrectText(string? ns, int id, string expected)
    {
        var serializer = NewSystemJsonSerializer(Substitute.For<IEngineIOMessageAdapter>());
        serializer.Namespace = ns;
        var list = serializer.SerializeAckData(["event", TestFile.NiuB.Bytes], id);
        list[0].Text.Should().Be(expected);
    }
}