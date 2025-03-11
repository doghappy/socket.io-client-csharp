using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Session.EngineIOHttpAdapter;

public class EngineIO4AdapterTests
{
    private readonly EngineIO4Adapter _adapter = new();

    [Fact]
    public void ToHttpRequest_GivenAnEmptyArray_ThrowException()
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(new List<byte[]>()))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The array cannot be empty");
    }

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem1Byte = new(new List<byte[]>
    {
        new byte[] { 1 },
    }, new HttpRequest
    {
        BodyText = "bAQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem10Bytes = new(new List<byte[]>
    {
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyText = "bAAECAwQFBgcICQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) TwoItems1Byte10Bytes = new(new List<byte[]>
    {
        new byte[] { 1 },
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyText = "bAQ==\u001EbAAECAwQFBgcICQ==",
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Text,
    });

    private static IEnumerable<(ICollection<byte[]> bytes, IHttpRequest req)> ToHttpRequestStrongTypeCases
    {
        get
        {
            yield return OneItem1Byte;
            yield return OneItem10Bytes;
            yield return TwoItems1Byte10Bytes;
        }
    }

    public static IEnumerable<object[]> ToHttpRequestCases =>
        ToHttpRequestStrongTypeCases.Select(x => new object[] { x.bytes, x.req });

    [Theory]
    [MemberData(nameof(ToHttpRequestCases))]
    public void ToHttpRequest_WhenCalled_AlwaysPass(ICollection<byte[]> bytes, IHttpRequest result)
    {
        var req = _adapter.ToHttpRequest(bytes);
        req.Should().BeEquivalentTo(result);
    }

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesSingleHelloWorld = new(
        "hello world!",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "hello world!" },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesPingAndHelloWorld = new(
        "2\u001Ehello world!",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "2" },
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "hello world!" },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) GetMessagesPingAndBytes = new(
        "2\u001EbAQ==",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Text, Text = "2" },
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [1] },
        ]);

    private static readonly (string raw, IEnumerable<ProtocolMessage> messages) Get2Bytes = new(
        "bAA==\u001EbAQ==",
        [
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [0] },
            new ProtocolMessage { Type = ProtocolMessageType.Bytes, Bytes = [1] },
        ]);

    public static TheoryData<string, IEnumerable<ProtocolMessage>> GetMessagesCases =>
        new()
        {
            {
                GetMessagesSingleHelloWorld.raw,
                GetMessagesSingleHelloWorld.messages
            },
            {
                GetMessagesPingAndHelloWorld.raw,
                GetMessagesPingAndHelloWorld.messages
            },
            {
                GetMessagesPingAndBytes.raw,
                GetMessagesPingAndBytes.messages
            },
            {
                Get2Bytes.raw,
                Get2Bytes.messages
            },
        };

    [Theory]
    [MemberData(nameof(GetMessagesCases))]
    public void GetMessages_WhenCalled_AlwaysPass(string raw, IEnumerable<ProtocolMessage> messages)
    {
        _adapter.GetMessages(raw)
            .Should()
            .BeEquivalentTo(messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToHttpRequest_GivenAnInvalidContent_ThrowException([CanBeNull] string content)
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(content))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The content cannot be null or empty");
    }

    [Theory]
    [InlineData(" ", " ")]
    [InlineData("hello, world!", "hello, world!")]
    public void ToHttpRequest_GivenValidContent_ReturnSameBodyText(string content, string expected)
    {
        var req = _adapter.ToHttpRequest(content);
        req.BodyText.Should().Be(expected);
    }
}