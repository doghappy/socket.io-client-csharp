using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using NSubstitute;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;
using SocketIOClient.Transport.WebSockets;

namespace SocketIOClient.UnitTests;

public static class TestHelper
{
    public static void ForE4ConnectAsync(this IHttpClient http)
    {
        http.SendAsync(
                Arg.Any<HttpRequestMessage>(), 
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                Content = new StringContent("40{\"sid\":\"sid\"}")
                {
                     Headers =
                     {
                         ContentType = new MediaTypeHeaderValue("text/plain")
                     }
                }
            });
    }
    
    public static void ForUpgradeWebSocketAsync(this IHttpClient http)
    {
        http.GetStringAsync(Arg.Any<Uri>())
            .Returns("40{\"sid\":\"sid\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}");
    }

    public static void ForEmitAsync(this IHttpClient http)
    {
        http.PostAsync(
                Arg.Any<string>(),
                Arg.Any<HttpContent>(), 
                Arg.Any<CancellationToken>())
            .Returns(new HttpResponseMessage
            {
                Content = new StringContent("")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("text/plain")
                    }
                }
            });
    }

    public static void ForConnectAsync(this IClientWebSocket ws)
    {
        ws.State.Returns(WebSocketState.Open);
        var buffer1 = "0{\"sid\":\"sid1\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}"u8.ToArray();
        ws.ReceiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new WebSocketReceiveResult
            {
                EndOfMessage = true,
                MessageType = TransportMessageType.Text,
                Buffer = buffer1,
                Count = buffer1.Length
            });

        var buffer2 = "40{\"sid\":\"sid2\"}"u8.ToArray();
        ws.ReceiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new WebSocketReceiveResult
            {
                EndOfMessage = true,
                MessageType = TransportMessageType.Text,
                Buffer = buffer2,
                Count = buffer2.Length
            });
    }
}