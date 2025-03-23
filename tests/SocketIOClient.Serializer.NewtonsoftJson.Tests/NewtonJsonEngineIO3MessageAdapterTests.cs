using FluentAssertions;
using SocketIOClient.Core.Messages;

namespace SocketIOClient.Serializer.NewtonsoftJson.Tests;

// ReSharper disable once InconsistentNaming
public class NewtonJsonEngineIO3MessageAdapterTests
{
    private readonly NewtonJsonEngineIO3MessageAdapter _adapter = new();

    [Theory]
    [InlineData("", null)]
    [InlineData("/nsp,", "/nsp")]
    public void DeserializeConnectedMessage_WhenCalled_AlwaysPass(string text, string? ns)
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