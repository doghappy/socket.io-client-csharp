using System;
using System.Collections.Generic;
using FluentAssertions;
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
    
    // TODO: Add more tests
}