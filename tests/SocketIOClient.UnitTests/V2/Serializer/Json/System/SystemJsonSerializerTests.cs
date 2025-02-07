using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using JetBrains.Annotations;
using NSubstitute;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Serializer.Json.Decapsulation;
using SocketIOClient.V2.Serializer.Json.System;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Serializer.Json.System;

public class SystemJsonSerializerTests
{
    public SystemJsonSerializerTests()
    {
        _serializer = new SystemJsonSerializer(_realDecapsulator);
    }
    
    private readonly SystemJsonSerializer _serializer;
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
        _serializer.Invoking(x => x.Serialize(Array.Empty<object>()))
            .Should()
            .Throw<ArgumentException>();
    }
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyNormalEventName = new(
        ["event"],
        [
            new ProtocolMessage
            {
              Type  = ProtocolMessageType.Text,
              Text = "42[\"event\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlySpecialEventName = new(
        ["Ab_@ '\"{}[].?-*/\\"],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"Ab_@ '\\\"{}[].?-*/\\\\\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWithNull = new(
        ["event", null],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",null]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1String = new(
        ["event", "hello, world!"],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",\"hello, world!\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1True = new(
        ["event", true],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",true]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1False = new(
        ["event", false],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",false]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1IntMax = new(
        ["event", int.MaxValue],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",2147483647]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1FloatMax = new(
        ["event", float.MaxValue],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",3.4028235E+38]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith1Object = new(
        ["event", new { id = 1, name = "Alice" }],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",{\"id\":1,\"name\":\"Alice\"}]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWithBytesAndObject = new(
        ["event",TestFiles.IndexHtml, new { id = 1, name = "Alice" }],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "451-[\"event\",{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}},{\"id\":1,\"name\":\"Alice\"}]",
            },
            new ProtocolMessage
            {
              Type  = ProtocolMessageType.Bytes,
              Bytes = "Hello World!"u8.ToArray(),
            },
        ]);
    
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) SerializeDataOnlyWith2Bytes = new(
        ["event",TestFiles.IndexHtml, TestFiles.NiuB],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "452-[\"event\",{\"Size\":1024,\"Name\":\"index.html\",\"Bytes\":{\"_placeholder\":true,\"num\":0}},{\"Size\":666,\"Name\":\"NiuB\",\"Bytes\":{\"_placeholder\":true,\"num\":1}}]",
            },
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Bytes,
                Bytes = "Hello World!"u8.ToArray(),
            },
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Bytes,
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
        var serializer = new SystemJsonSerializer(_realDecapsulator, options);
        serializer.Serialize(data).Should().BeEquivalentTo(expected);
    }
    
    [Theory]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("test", "42test,[\"event\"]")]
    public void Serialize_NamespaceNoBytes_ContainsNamespaceIfExists([CanBeNull] string ns, string expected)
    {
        _serializer.Namespace = ns;
        var list= _serializer.Serialize(["event"]);
        list[0].Text.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("test", "451-test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void Serialize_NamespaceWithBytes_ContainsNamespaceIfExists([CanBeNull] string ns, string expected)
    {
        _serializer.Namespace = ns;
        var list= _serializer.Serialize(["event", TestFiles.NiuB.Bytes]);
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
    
    private static readonly (string text, IMessage message) DeserializeEio3NoNamespaceConnectedMessage = new("40", new ConnectedMessage());

    public static TheoryData<string, IMessage> DeserializeEio3Cases =>
        new()
        {
            { DeserializeOpenedMessage.text, DeserializeOpenedMessage.message },
            { DeserializeEio3NoNamespaceConnectedMessage.text, DeserializeEio3NoNamespaceConnectedMessage.message },
        };
    
    public static TheoryData<string, IMessage> DeserializeEio4Cases =>
        new()
        {
            { DeserializeOpenedMessage.text, DeserializeOpenedMessage.message },
        };

    [Theory]
    [MemberData(nameof(DeserializeEio3Cases))]
    public void Deserialize_EngineIO3MessageAdapter_ReturnMessage(string text, IMessage expected)
    {
        _serializer.EngineIOMessageAdapter = new SystemJsonEngineIO3MessageAdapter();
        _serializer.Deserialize(text).Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(DeserializeEio4Cases))]
    public void Deserialize_EngineIO4MessageAdapter_ReturnMessage(string text, IMessage expected)
    {
        _serializer.EngineIOMessageAdapter = new SystemJsonEngineIO3MessageAdapter();
        _serializer.Deserialize(text).Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void Deserialize_DecapsulationResultFalse_ReturnNull()
    {
        var decapsulator = Substitute.For<IDecapsulable>();
        decapsulator.Decapsulate(Arg.Any<string>()).Returns(new DecapsulationResult
        {
            Success = false,
        });
        var serializer = new SystemJsonSerializer(decapsulator);
        const string text = "0{\"sid\":\"123\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}";
        
        serializer.Deserialize(text).Should().BeNull();
    }
}