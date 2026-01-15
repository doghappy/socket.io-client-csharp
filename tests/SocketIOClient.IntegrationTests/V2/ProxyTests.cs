using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Test.Core;
using SocketIOClient.V2.Protocol.WebSocket;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

public class ProxyTests(ITestOutputHelper output)
{
    private readonly SocketIOClient.V2.SocketIOOptions _options = new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
    };

    private SocketIOClient.V2.SocketIO NewSocketIO(Uri url, Action<IServiceCollection> configure)
    {
        return new SocketIOClient.V2.SocketIO(url, _options, services =>
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new XUnitLoggerProvider(output));
            });
            configure(services);
        });
    }

    [Fact]
    public async Task Proxy_HttpClientProxy_AlwaysPass()
    {
        await using var proxy = new ProxyServer(output);
        using var cts = new CancellationTokenSource();
        await proxy.StartHttpAsync(cts.Token);
        var proxyUrl = proxy.ProxyUrl;

        var io = NewSocketIO(new Uri("http://localhost:11410"), services =>
        {
            services.AddSingleton<HttpClient>(_ =>
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyUrl),
                    UseProxy = true
                };

                return new HttpClient(handler);
            });
        });
        await io.ConnectAsync(CancellationToken.None);

        proxy.ResponseTexts.Should().Contain(t => t.StartsWith("0{\"sid\":\""));
        proxy.ResponseTexts.Should().Contain(t => t.StartsWith("40"));
        await cts.CancelAsync();
    }

    [Fact]
    public async Task Proxy_WebSocketProxy_AlwaysPass()
    {
        _options.Transport = TransportProtocol.WebSocket;
        _options.ConnectionTimeout = TimeSpan.FromSeconds(6);

        await using var proxy = new ProxyServer(output);
        using var cts = new CancellationTokenSource();
        await proxy.StartWebSocketAsync(cts.Token);
        var proxyUrl = proxy.ProxyUrl;

        var io = NewSocketIO(new Uri("http://localhost:11400"), services =>
        {
            services.AddSingleton(new WebSocketOptions
            {
                Proxy = new WebProxy(proxyUrl)
            });
        });
        await io.ConnectAsync(CancellationToken.None);

        proxy.ResponseTexts.Should().Contain(t => t.StartsWith("0{\"sid\":\""));
        proxy.ResponseTexts.Should().Contain(t => t.StartsWith("40"));
        await cts.CancelAsync();
    }
}