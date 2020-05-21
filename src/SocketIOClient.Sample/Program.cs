using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
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

            var uri = new Uri("http://localhost:11000/nsp");
            var socket = new SocketIO(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    {"token", "io" }
                },
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            });

            socket.OnConnected += Socket_OnConnected;
            socket.OnPing += Socket_OnPing;
            socket.OnPong += Socket_OnPong;
            socket.OnDisconnected += Socket_OnDisconnected;

            await socket.ConnectAsync();

            Console.ReadLine();
        }

        private static void Socket_OnDisconnected(object sender, string e)
        {
            Console.WriteLine("disconnect: " + e);
        }

        private static async void Socket_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Socket_OnConnected");
            var client = sender as SocketIO;
            Console.WriteLine("Socket.Id:" + client.Id);

            client.On("hi", response =>
            {
                Console.WriteLine($"server: {response.GetValue<string>()}");
            });

            client.On("bytes", response =>
            {
                var bytes = response.GetValue<ByteResponse>();
                Console.WriteLine($"bytes.Source = {bytes.Source}");
                Console.WriteLine($"bytes.ClientSource = {bytes.ClientSource}");
                Console.WriteLine($"bytes.Buffer.Length = {bytes.Buffer.Length}");
                Console.WriteLine($"bytes.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            });
            client.OnReceivedEvent += (sender, e) =>
            {
                if (e.Event == "bytes")
                {
                    var bytes = e.Response.GetValue<ByteResponse>();
                    Console.WriteLine($"OnReceivedEvent.Source = {bytes.Source}");
                    Console.WriteLine($"OnReceivedEvent.ClientSource = {bytes.ClientSource}");
                    Console.WriteLine($"OnReceivedEvent.Buffer.Length = {bytes.Buffer.Length}");
                    Console.WriteLine($"OnReceivedEvent.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
                }
            };


            await client.EmitAsync("hi", "SocketIOClient.Sample");

            await client.EmitAsync("ack", response =>
            {
                Console.WriteLine(response.ToString());
            }, "SocketIOClient.Sample");

            await client.EmitAsync("bytes", "c#", new
            {
                source = "client007",
                bytes = Encoding.UTF8.GetBytes("dot net")
            });

            await client.EmitAsync("binary ack", response =>
            {
                var bytes = response.GetValue<ByteResponse>();
                Console.WriteLine($"(binary ack).Source = {bytes.Source}");
                Console.WriteLine($"(binary ack).ClientSource = {bytes.ClientSource}");
                Console.WriteLine($"(binary ack).Buffer.Length = {bytes.Buffer.Length}");
                Console.WriteLine($"(binary ack).Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            }, "C#", new
            {
                source = "client007",
                bytes = Encoding.UTF8.GetBytes("dot net")
            });
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
}
