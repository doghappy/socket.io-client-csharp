using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer.Decapsulation;
using SocketIOClient.UnitTests.V2.Serializer;

namespace SocketIOClient.Serializer.NewtonsoftJson.Tests;

public class NewtonJsonSerializerTests
{
    public NewtonJsonSerializerTests()
    {
        _serializer = new NewtonJsonSerializer(_realDecapsulator, new JsonSerializerSettings());
    }

    private readonly NewtonJsonSerializer _serializer;
    private readonly Decapsulator _realDecapsulator = new();

    [Fact]
    public void Serialize_DataOnlyAndDataIsNull_ThrowArgumentNullException()
    {
        _serializer.Invoking(x => x.Serialize(null))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Serialize_DataOnlyAndDataIsEmpty_ThrowArgumentException()
    {
        _serializer.Invoking(x => x.Serialize([]))
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
        ["event", null],
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
        var serializer = new NewtonJsonSerializer(_realDecapsulator, new JsonSerializerSettings());
        serializer.Serialize(data).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("test", "42test,[\"event\"]")]
    public void Serialize_NamespaceNoBytes_ContainsNamespaceIfExists(string? ns, string expected)
    {
        _serializer.Namespace = ns;
        var list = _serializer.Serialize(["event"]);
        list[0].Text.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("test", "451-test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void Serialize_NamespaceWithBytes_ContainsNamespaceIfExists(string? ns, string expected)
    {
        _serializer.Namespace = ns;
        var list = _serializer.Serialize(["event", TestFile.NiuB.Bytes]);
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
        new NewtonJsonEventMessage
        {
            Event = "event",
            Id = 2,
            Namespace = "/test",
        });

    private static readonly (string text, IMessage message) DeserializeEventMessageNull = new(
        "42[\"hello\",null]",
        new NewtonJsonEventMessage
        {
            Event = "hello",
        });

    private static readonly (string text, IMessage message) DeserializeNamespaceAckMessage = new(
        "43/test,1[\"nice\"]",
        new NewtonJsonAckMessage
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
        new NewtonJsonBinaryAckMessage
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
        _serializer.EngineIOMessageAdapter = new NewtonJsonEngineIO3MessageAdapter();
        var message = _serializer.Deserialize(text);
        message.Should()
            .BeEquivalentTo(expected,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(p => p.Name == nameof(NewtonJsonAckMessage.DataItems))
                    .Excluding(p => p.Name == nameof(INewtonJsonAckMessage.JsonSerializerSettings)));
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
        _serializer.EngineIOMessageAdapter = new NewtonJsonEngineIO4MessageAdapter();
        var message = _serializer.Deserialize(text);
        message.Should()
            .BeEquivalentTo(expected,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(p => p.Name == nameof(NewtonJsonAckMessage.DataItems))
                    .Excluding(p => p.Name == nameof(INewtonJsonAckMessage.JsonSerializerSettings)));
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
        _serializer.EngineIOMessageAdapter = new NewtonJsonEngineIO4MessageAdapter();
        var message = _serializer.Deserialize(text) as IAckMessage;
        var item1 = message!.GetDataValue(expected.GetType(), 0);
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
        _serializer.EngineIOMessageAdapter = new NewtonJsonEngineIO4MessageAdapter();
        var message = _serializer.Deserialize(text) as IEventMessage;
        var item1 = message!.GetDataValue(expected1.GetType(), 0);
        item1.Should().BeEquivalentTo(expected1);

        var item2 = message!.GetDataValue(expected2.GetType(), 1);
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
        var serializer = new NewtonJsonSerializer(decapsulator, new JsonSerializerSettings());
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
        _serializer.EngineIOMessageAdapter = new NewtonJsonEngineIO4MessageAdapter();
        var message = (IBinaryAckMessage)_serializer.Deserialize(text);

        message.Add(bytes);
        var item1 = message!.GetDataValue<TestFile>(0);
        item1.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void NewPingMessage_WhenCalled_ReturnPingMessage()
    {
        var ping = _serializer.NewPingMessage();
        ping.Should()
            .BeEquivalentTo(new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "2",
            });
    }

    [Fact]
    public void NewPongMessage_WhenCalled_ReturnPingMessage()
    {
        var pong = _serializer.NewPongMessage();
        pong.Should()
            .BeEquivalentTo(new ProtocolMessage
            {
                Type = ProtocolMessageType.Text,
                Text = "3",
            });
    }
}