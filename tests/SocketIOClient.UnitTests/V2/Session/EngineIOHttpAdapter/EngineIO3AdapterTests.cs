using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOHttpAdapter;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Session.EngineIOHttpAdapter;

public class EngineIO3AdapterTests
{
    private readonly EngineIO3Adapter _adapter = new();

    [Fact]
    public void ToHttpRequest_GivenAnEmptyArray_ThrowException()
    {
        _adapter
            .Invoking(x => x.ToHttpRequest(new List<byte[]>()))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The array cannot be empty");
    }

    [Fact]
    public void ToHttpRequest_GivenAnEmptySubBytes_ThrowException()
    {
        var bytes = new List<byte[]>
        {
            Array.Empty<byte>(),
        };
        _adapter
            .Invoking(x => x.ToHttpRequest(bytes))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("The sub array cannot be empty");
    }

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem1Byte = new(new List<byte[]>
    {
        new byte[] { 1 },
    }, new HttpRequest
    {
        BodyBytes =
        [
            1, 1, 255, 4, 1,
        ],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) OneItem10Bytes = new(new List<byte[]>
    {
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes =
        [
            1, 1, 0, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        ],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
    });

    private static readonly (ICollection<byte[]> bytes, IHttpRequest req) TwoItems1Byte10Bytes = new(new List<byte[]>
    {
        new byte[] { 1 },
        Enumerable.Range(0, 10).Select(x => (byte)x).ToArray(),
    }, new HttpRequest
    {
        BodyBytes =
        [
            1, 1, 255, 4, 1,
            1, 1, 0, 255, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        ],
        Method = RequestMethod.Post,
        BodyType = RequestBodyType.Bytes,
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/octet-stream" },
        },
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
}