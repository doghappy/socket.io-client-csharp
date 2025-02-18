using FluentAssertions;
using JetBrains.Annotations;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Serializer.Json.System;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Serializer.Json.System;

// ReSharper disable once InconsistentNaming
public class SystemJsonEngineIO4MessageAdapterTests
{
    private readonly SystemJsonEngineIO4MessageAdapter _adapter = new();

    [Theory]
    [InlineData("{\"sid\":\"123\"}", "123", null)]
    [InlineData("/test,{\"sid\":\"123\"}", "123", "/test")]
    public void DeserializeConnectedMessage_WhenCalled_AlwaysPass(string text, string sid, [CanBeNull] string ns)
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
    public void DeserializeErrorMessage_WhenCalled_AlwaysPass(string text, string error, [CanBeNull] string ns)
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