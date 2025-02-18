using FluentAssertions;
using JetBrains.Annotations;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Serializer.Json.System;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Serializer.Json.System;

// ReSharper disable once InconsistentNaming
public class SystemJsonEngineIO3MessageAdapterTests
{
    private readonly SystemJsonEngineIO3MessageAdapter _adapter = new();

    [Theory]
    [InlineData("", null)]
    [InlineData("/nsp,", "/nsp")]
    public void DeserializeConnectedMessage_WhenCalled_AlwaysPass(string text, [CanBeNull] string ns)
    {
        var message = _adapter.DeserializeConnectedMessage(text);
        message.Should()
            .BeEquivalentTo(new ConnectedMessage
            {
                Namespace = ns,
                Sid = null,
            });
    }

    [Theory]
    [InlineData("\"error message\"", "error message")]
    [InlineData("\"\\\"Authentication error\\\"\"", "\"Authentication error\"")]
    public void DeserializeErrorMessage_WhenCalled_AlwaysPass(string text, string error)
    {
        var message = _adapter.DeserializeErrorMessage(text);
        message.Should()
            .BeEquivalentTo(new ErrorMessage
            {
                // TODO: is namespace supported by eio3?
                Namespace = null,
                Error = error,
            });
    }
}