using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using JetBrains.Annotations;
using SocketIOClient.V2;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Serializer.Json;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Serializer.Json;

public class SystemJsonSerializerTests
{
    public SystemJsonSerializerTests()
    {
        _serializer = new SystemJsonSerializer();
    }
    
    private readonly SystemJsonSerializer _serializer;
    
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
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyNormalEventName = new(
        ["event"],
        [
            new ProtocolMessage
            {
              Type  = ProtocolMessageType.Text,
              Text = "42[\"event\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlySpecialEventName = new(
        ["Ab_@ '\"{}[].?-*/\\"],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"Ab_@ '\\\"{}[].?-*/\\\\\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWithNull = new(
        ["event", null],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",null]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1String = new(
        ["event", "hello, world!"],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",\"hello, world!\"]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1True = new(
        ["event", true],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",true]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1False = new(
        ["event", false],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",false]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1IntMax = new(
        ["event", int.MaxValue],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",2147483647]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1FloatMax = new(
        ["event", float.MaxValue],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",3.4028235E+38]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith1Object = new(
        ["event", new { id = 1, name = "Alice" }],
        [
            new ProtocolMessage
            {
                Type  = ProtocolMessageType.Text,
                Text = "42[\"event\",{\"id\":1,\"name\":\"Alice\"}]",
            },
        ]);
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWithBytesAndObject = new(
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
    
    
    private static readonly (object[] input, IEnumerable<ProtocolMessage> output) EngineIO3DataOnlyWith2Bytes = new(
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
    
    private static IEnumerable<(object[] input, IEnumerable<ProtocolMessage> output)> EngineIO3DataOnlyStrongTypeCases
    {
        get
        {
            yield return EngineIO3DataOnlyNormalEventName;
            yield return EngineIO3DataOnlySpecialEventName;
            yield return EngineIO3DataOnlyWithNull;
            yield return EngineIO3DataOnlyWith1String;
            yield return EngineIO3DataOnlyWith1True;
            yield return EngineIO3DataOnlyWith1False;
            yield return EngineIO3DataOnlyWith1IntMax;
            yield return EngineIO3DataOnlyWith1FloatMax;
            yield return EngineIO3DataOnlyWith1Object;
            yield return EngineIO3DataOnlyWithBytesAndObject;
            yield return EngineIO3DataOnlyWith2Bytes;
        }
    }

    public static TheoryData<object[], IEnumerable<ProtocolMessage>> EngineIO3DataOnlyCases
    {
        get
        {
            var data = new TheoryData<object[], IEnumerable<ProtocolMessage>>();
            foreach (var item in EngineIO3DataOnlyStrongTypeCases)
            {
                data.Add(item.input, item.output);
            }
            return data;
        }
    }
    
    [Theory]
    [MemberData(nameof(EngineIO3DataOnlyCases))]
    public void Serialize_EngineIO3DataOnly_AlwaysPass(object[] data, IEnumerable<ProtocolMessage> expected)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var serializer = new SystemJsonSerializer(options)
        {
            EngineIO = EngineIO.V3,
        };
        serializer.Serialize(data).Should().BeEquivalentTo(expected);
    }
    
    [Theory]
    [InlineData(null, "42[\"event\"]")]
    [InlineData("", "42[\"event\"]")]
    [InlineData("test", "42test,[\"event\"]")]
    public void Serialize_EngineIO3NamespaceNoBytes_ContainsNamespaceIfExists([CanBeNull] string ns, string expected)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var serializer = new SystemJsonSerializer(options)
        {
            EngineIO = EngineIO.V3,
            Namespace = ns,
        };
        var list= serializer.Serialize(["event"]);
        list[0].Text.Should().Be(expected);
    }
    
    [Theory]
    [InlineData(null, "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("", "451-[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    [InlineData("test", "451-test,[\"event\",{\"_placeholder\":true,\"num\":0}]")]
    public void Serialize_EngineIO3NamespaceWithBytes_ContainsNamespaceIfExists([CanBeNull] string ns, string expected)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var serializer = new SystemJsonSerializer(options)
        {
            EngineIO = EngineIO.V3,
            Namespace = ns,
        };
        var list= serializer.Serialize(["event", TestFiles.NiuB.Bytes]);
        list[0].Text.Should().Be(expected);
    }
}