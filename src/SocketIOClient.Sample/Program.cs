using Newtonsoft.Json;
using SocketIOClient.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            //var uri = new Uri("http://localhost:11003/nsp");
            //var uri = new Uri("http://localhost:11000");
            var uri = new Uri("http://localhost:11003");
            //var uri = new Uri("https://socket-io.doghappy.wang");

            var socket = new SocketIO(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    //{"token", "io" }
                    {"token", "v3" }
                },
                EIO = 4
            });

            socket.GetConnectInterval = () => new MyConnectInterval();


            //var client = socket.Socket as ClientWebSocket;
            //client.Config = options =>
            //{
            //    options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            //    {
            //        Console.WriteLine("SslPolicyErrors: " + sslPolicyErrors);
            //        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            //        {
            //            return true;
            //        }
            //        return false;
            //    };
            //};

            socket.OnConnected += Socket_OnConnected;
            socket.OnPing += Socket_OnPing;
            socket.OnPong += Socket_OnPong;
            socket.OnDisconnected += Socket_OnDisconnected;
            socket.OnReconnecting += Socket_OnReconnecting;
            await socket.ConnectAsync();

            socket.On("hi", response =>
            {
                Console.WriteLine($"server: {response.GetValue<string>()}");
            });

            //socket.On("bytes", response =>
            //{
            //    var bytes = response.GetValue<ByteResponse>();
            //    Console.WriteLine($"bytes.Source = {bytes.Source}");
            //    Console.WriteLine($"bytes.ClientSource = {bytes.ClientSource}");
            //    Console.WriteLine($"bytes.Buffer.Length = {bytes.Buffer.Length}");
            //    Console.WriteLine($"bytes.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            //});
            //socket.OnReceivedEvent += (sender, e) =>
            //{
            //    if (e.Event == "bytes")
            //    {
            //        var bytes = e.Response.GetValue<ByteResponse>();
            //        Console.WriteLine($"OnReceivedEvent.Source = {bytes.Source}");
            //        Console.WriteLine($"OnReceivedEvent.ClientSource = {bytes.ClientSource}");
            //        Console.WriteLine($"OnReceivedEvent.Buffer.Length = {bytes.Buffer.Length}");
            //        Console.WriteLine($"OnReceivedEvent.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            //    }
            //};

            Console.ReadLine();
        }

        private static void Socket_OnReconnecting(object sender, int e)
        {
            Console.WriteLine($"Reconnecting: attempt = {e}");
        }

        private static void Socket_OnDisconnected(object sender, string e)
        {
            Console.WriteLine("disconnect: " + e);
        }

        private static async void Socket_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Socket_OnConnected");
            var socket = sender as SocketIO;
            Console.WriteLine("Socket.Id:" + socket.Id);
            await socket.EmitAsync("hi", "SocketIOClient.Sample");

            //await socket.EmitAsync("ack", response =>
            //{
            //    Console.WriteLine(response.ToString());
            //}, "SocketIOClient.Sample");

            //await socket.EmitAsync("bytes", "c#", new
            //{
            //    source = "client007",
            //    bytes = Encoding.UTF8.GetBytes("dot net")
            //});

            //socket.On("client binary callback", async response =>
            //{
            //    await response.CallbackAsync();
            //});

            //await socket.EmitAsync("client binary callback", Encoding.UTF8.GetBytes("SocketIOClient.Sample"));

            //socket.On("client message callback", async response =>
            //{
            //    await response.CallbackAsync(Encoding.UTF8.GetBytes("CallbackAsync();"));
            //});
            //await socket.EmitAsync("client message callback", "SocketIOClient.Sample");
        }

        private static void Socket_OnPing(object sender, EventArgs e)
        {
            Console.WriteLine("Ping");
        }

        private static void Socket_OnPong(object sender, TimeSpan e)
        {
            Console.WriteLine("Pong: " + e.TotalMilliseconds);
        }
    }

    class ByteResponse
    {
        public string ClientSource { get; set; }

        public string Source { get; set; }

        [JsonProperty("bytes")]
        public byte[] Buffer { get; set; }
    }

    class ClientCallbackResponse
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("bytes")]
        public byte[] Bytes { get; set; }
    }
}
