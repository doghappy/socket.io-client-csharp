using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SocketIOClient.Test.Core;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests;

public abstract class SocketIOEngineIO4Tests(ITestOutputHelper output) : SocketIOTests(output)
{
    [Fact]
    public async Task ConnectAsync_ValidAuth_CanGetAuthFromServer()
    {
        var io = NewSocketIO(Url);
        io.Options.Reconnection = false;
        io.Options.Auth = new UserPasswordDto
        {
            User = "user",
            Password = "password"
        };

        await io.ConnectAsync(CancellationToken.None);

        UserPasswordDto? dto = null;
        await io.EmitAsync("get_auth", msg => dto = msg.GetValue<UserPasswordDto>(0));
        await Task.Delay(DefaultDelay);

        dto.Should().BeEquivalentTo(new UserPasswordDto
        {
            User = "user",
            Password = "password"
        });
    }
}