using Microsoft.Extensions.Logging;

namespace SocketIOClient.Test.Core;

public static class TestHelper
{
    public static ILoggerFactory NewLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace)
                .AddConsole();
        });
    }
}