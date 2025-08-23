using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SocketIOClient.Test.Core;

public class XUnitLogger(ITestOutputHelper output, string category) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        output.WriteLine($"{logLevel} [{category}] {formatter(state, exception)}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}