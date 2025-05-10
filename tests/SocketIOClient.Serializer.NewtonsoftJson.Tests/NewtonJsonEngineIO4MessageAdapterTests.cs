using FluentAssertions;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson.Tests;

// ReSharper disable once InconsistentNaming
public class NewtonJsonEngineIO4MessageAdapterTests
{
    private readonly NewtonJsonEngineIO4MessageAdapter _adapter = new();

    [Theory]
    [InlineData("{\"sid\":\"123\"}", "123", null)]
    [InlineData("/test,{\"sid\":\"123\"}", "123", "/test")]
    public void DeserializeConnectedMessage_WhenCalled_AlwaysPass(string text, string sid, string? ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = sid,
            });
    }

    [Theory]
    [InlineData("{\"message\":\"error message\"}", "error message", null)]
    [InlineData("/test,{\"message\":\"error message\"}", "error message", "/test")]
    public void DeserializeErrorMessage_WhenCalled_AlwaysPass(string text, string error, string? ns)
    {
        var message = _adapter.DeserializeErrorMessage(text);
        message.Should()
            .BeEquivalentTo(new ErrorMessage
            {
                Namespace = ns,
                Error = error,
            });
    }
}