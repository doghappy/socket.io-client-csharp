using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SocketIOClient.Test.Core;

public static class XUnitLoggerExtensions
{
    public static ILogger<T> CreateLogger<T>(this ITestOutputHelper output)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new XUnitLoggerProvider(output));
        });
        return factory.CreateLogger<T>();
    }
}