using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common;
using SocketIOClient.Exceptions;
using SocketIOClient.Protocol.WebSocket;
using SocketIOClient.Test.Core;
using Xunit;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests;

public class ProxyAndSslTests(ITestOutputHelper output)
{
    private readonly SocketIOOptions _options = new()
    {
        EIO = EngineIO.V4,
        Transport = TransportProtocol.Polling,
        Reconnection = false,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
        AutoUpgrade = false,
    };

    private SocketIO NewSocketIO(Uri url, Action<IServiceCollection> configure)
    {
        return new SocketIO(url, _options, services =>
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

    private readonly Uri _httpsUri = new("https://localhost:11414");

    [Fact]
    public async Task HttpClient_ServerCertHasError_ThrowConnectionException()
    {
        var io = NewSocketIO(_httpsUri, _ => { });
        await io.Invoking(x => x.ConnectAsync()).Should().ThrowAsync<ConnectionException>();
    }

    [Fact]
    public async Task HttpClient_IgnoreServerCertError_AlwaysPass()
    {
        var callback = false;
        var io = NewSocketIO(_httpsUri, services =>
        {
            services.AddSingleton<HttpClient>(_ =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) =>
                    {
                        callback = true;
                        return true;
                    }
                };

                return new HttpClient(handler);
            });
        });
        await io.ConnectAsync();

        callback.Should().BeTrue();
    }

    private readonly Uri _wssUri = new("https://localhost:11404");

    [Fact]
    public async Task WebSocket_ServerCertHasError_ThrowConnectionException()
    {
        _options.Transport = TransportProtocol.WebSocket;
        var io = NewSocketIO(_wssUri, _ => { });

        await io.Invoking(x => x.ConnectAsync()).Should().ThrowAsync<ConnectionException>();
    }

    [Fact]
    public async Task WebSocket_IgnoreServerCertError_AlwaysPass()
    {
        var callback = false;
        _options.Transport = TransportProtocol.WebSocket;
        var io = NewSocketIO(_wssUri, services =>
        {
            services.AddSingleton(new WebSocketOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) =>
                {
                    callback = true;
                    return true;
                }
            });
        });
        await io.ConnectAsync();

        callback.Should().BeTrue();
    }
}