using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using NSubstitute;
using SocketIOClient.Transport.Http;

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
}