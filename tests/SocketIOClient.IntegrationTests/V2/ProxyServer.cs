using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace SocketIOClient.IntegrationTests.V2;

public class ProxyServer(ITestOutputHelper output) : IAsyncDisposable
{
    private WebApplication _app = null!;

    public List<string> ResponseTexts { get; } = new();

    public string? ProxyUrl { get; private set; }

    private static readonly HttpClient Http = new HttpClient(new SocketsHttpHandler
    {
        UseProxy = false
    });

    private void ThrowIfAppInitialized()
    {
        if (_app is not null)
        {
            throw new InvalidOperationException("The server has already been initialized.");
        }
    }

    private async Task StartAsync(CancellationToken cancellationToken, Func<HttpContext, CancellationToken, Task> handler)
    {
        ThrowIfAppInitialized();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        app.UseWebSockets();
        app.Run(async ctx => await handler(ctx, cancellationToken));

        await app.StartAsync(cancellationToken);
        _app = app;
        ProxyUrl = app.Urls.Single();
    }

    public async Task StartHttpAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken, ForwardHttp);
    }

    public async Task StartWebSocketAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken, ForwardWebSocket);
    }

    private async Task ForwardHttp(HttpContext ctx, CancellationToken cancellationToken)
    {
        using var req = CloneRequest(ctx.Request);
        output.WriteLine("Forwarding request to {0} {1}", ctx.Request.Method, req.RequestUri);

        using var response = await Http.SendAsync(req, cancellationToken);
        ctx.Response.StatusCode = (int)response.StatusCode;
        output.WriteLine("Forwarding response to upstream. status code: {0}", ctx.Response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        output.WriteLine(body);
        ResponseTexts.Add(body);
        await ctx.Response.WriteAsync(body, cancellationToken);
    }

    private async Task ForwardWebSocket(HttpContext ctx, CancellationToken cancellationToken)
    {
        if (!ctx.WebSockets.IsWebSocketRequest)
        {
            return;
        }

        using var clientWebSocket = await ctx.WebSockets.AcceptWebSocketAsync();
        using var serverWebSocket = new ClientWebSocket();
        var targetUri = GetUpstreamUri(ctx.Request);
        var wsUri = new UriBuilder(targetUri)
        {
            Scheme = ctx.Request.Scheme == "https" ? "wss" : "ws",
        }.Uri;
        await serverWebSocket.ConnectAsync(wsUri, cancellationToken);

        await Task.WhenAll(
            RelayWebSocket(serverWebSocket, clientWebSocket, ResponseTexts, cancellationToken),
            RelayWebSocket(clientWebSocket, serverWebSocket, null, cancellationToken)
        );
    }

    private static HttpRequestMessage CloneRequest(HttpRequest httpRequest)
    {
        var targetUri = GetUpstreamUri(httpRequest);
        var req = new HttpRequestMessage(new HttpMethod(httpRequest.Method), targetUri);
        CopyHeaders(req, httpRequest.Headers);
        CopyBody(req, httpRequest);
        return req;
    }

    private static Uri GetUpstreamUri(HttpRequest httpRequest)
    {
        var targetUri = new UriBuilder
        {
            Scheme = httpRequest.Scheme,
            Host = httpRequest.Host.Host,
            Port = httpRequest.Host.Port ?? -1,
            Path = httpRequest.Path.Value,
            Query = httpRequest.QueryString.Value
        }.Uri;
        return targetUri;
    }

    private static void CopyBody(HttpRequestMessage reqMsg, HttpRequest httpRequest)
    {
        if (httpRequest.ContentLength > 0 || httpRequest.Headers.ContainsKey("Transfer-Encoding"))
        {
            reqMsg.Content = new StreamContent(httpRequest.Body);

            foreach (var header in httpRequest.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    reqMsg.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }
    }

    private static void CopyHeaders(HttpRequestMessage req, IHeaderDictionary requestHeaders)
    {
        foreach (var header in requestHeaders)
        {
            req.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    private static async Task RelayWebSocket(WebSocket from, WebSocket to, List<string>? capture, CancellationToken cancellationToken)
    {
        var buffer = new byte[8 * 1024];

        while (from.State == WebSocketState.Open && to.State == WebSocketState.Open)
        {
            var result = await from.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await to.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription,
                    cancellationToken);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                capture?.Add(text);
            }

            await to.SendAsync(
                new ArraySegment<byte>(buffer, 0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}