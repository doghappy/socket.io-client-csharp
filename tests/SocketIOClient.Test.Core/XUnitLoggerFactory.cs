using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SocketIOClient.Test.Core;

public class XUnitLoggerFactory(ITestOutputHelper output): ILoggerFactory
{
    public ILogger CreateLogger(string category)
    {
        return new XUnitLogger(output, category);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}