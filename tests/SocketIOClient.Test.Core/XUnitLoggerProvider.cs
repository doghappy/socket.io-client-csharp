using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SocketIOClient.Test.Core;

public class XUnitLoggerProvider(ITestOutputHelper output) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(output, categoryName);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}