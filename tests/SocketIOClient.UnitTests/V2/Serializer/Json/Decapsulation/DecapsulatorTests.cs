using FluentAssertions;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Serializer.Json.Decapsulation;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Serializer.Json.Decapsulation;

public class DecapsulatorTests
{
    [Theory]
    [InlineData("", false, null, null)]
    [InlineData("0", true, MessageType.Opened, "")]
    [InlineData(
        "0{\"sid\":\"123\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
        true,
        MessageType.Opened,
        "{\"sid\":\"123\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}")]
    public void Decapsulate_WhenCalled_AlwaysPass(string text, bool success, MessageType? type, string data)
    {
        var decapsulator = new Decapsulator();
        var result = decapsulator.Decapsulate(text);
        
        result.Should()
            .BeEquivalentTo(new DecapsulationResult
            {
                Success = success,
                Type = type,
                Data = data
            });
    }
}