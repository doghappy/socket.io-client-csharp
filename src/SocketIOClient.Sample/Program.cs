using Newtonsoft.Json;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Show Debug and Trace messages
            Console.OutputEncoding = Encoding.UTF8;
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            //var uri = new Uri("http://localhost:11003/");

            //var socket = new SocketIO(uri, new SocketIOOptions
            //{
            //    Transport = Transport.TransportProtocol.WebSocket,
            //    Query = new Dictionary<string, string>
            //    {
            //        {"token", "V3" }
            //    },
            //});
            var uri = new Uri("http://localhost:11002/");

            var socket = new SocketIO(uri, new SocketIOOptions
            {
                Transport = Transport.TransportProtocol.Polling,
                AutoUpgrade = false,
                EIO = 3,
                Query = new Dictionary<string, string>
                {
                    {"token", "V2" }
                },
            });

            socket.OnConnected += Socket_OnConnected;
            socket.OnPing += Socket_OnPing;
            socket.OnPong += Socket_OnPong;
            socket.OnDisconnected += Socket_OnDisconnected;
            socket.OnReconnectAttempt += Socket_OnReconnecting;
            socket.OnAny((name, response) =>
            {
                Console.WriteLine(name);
                Console.WriteLine(response);
            });
            socket.On("hi", response =>
            {
                // Console.WriteLine(response.ToString());
                Console.WriteLine(response.GetValue<string>());
            });

            //Console.WriteLine("Press any key to continue");
            //Console.ReadLine();

            await socket.ConnectAsync();

            Console.ReadLine();
        }

        private static void Socket_OnReconnecting(object sender, int e)
        {
            Console.WriteLine($"{DateTime.Now} Reconnecting: attempt = {e}");
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

            //while (true)
            //{
            //    await Task.Delay(1000);
            //await socket.EmitAsync("hi", DateTime.Now.ToString());
            //await socket.EmitAsync("welcome");
            await socket.EmitAsync("1 params", Encoding.UTF8.GetBytes("test"));
            //}
            //byte[] bytes = Encoding.UTF8.GetBytes("ClientCallsServerCallback_1Params_0");
            //await socket.EmitAsync("client calls the server's callback 1", bytes);
            //await socket.EmitAsync("1 params", Encoding.UTF8.GetBytes("hello world"));
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

    //class Program
    //{
    //    public static void Main(string[] args)
    //        => new Program().Main().GetAwaiter().GetResult();

    //    private static string GenerateToken()
    //    {
    //        string chars = "abcdefghi1234567890";

    //        StringBuilder token = new StringBuilder("WDN");

    //        while (token.Length < 35)
    //            token.Append(chars.OrderBy(r => Guid.NewGuid()).FirstOrDefault());

    //        return token.ToString().Trim();
    //    }

    //    public async Task Main()
    //    {
    //        var socket = new SocketIO("https://v3-rc.palringo.com:3051/",
    //            new SocketIOOptions()
    //            {
    //                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
    //                Query = new Dictionary<string, string>()
    //                {
    //                    ["token"] = GenerateToken(),
    //                    ["device"] = "wolfnetframework",
    //                    ["state"] = "1",// Online State = 1 (Online)
    //                },
    //            }
    //        ); ;

    //        socket.JsonSerializer = new NewtonsoftJsonSerializer()
    //        {
    //            OptionsProvider = () => new JsonSerializerSettings()
    //            {
    //                NullValueHandling = NullValueHandling.Ignore,
    //                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    //            }
    //        };


    //        socket.OnConnected += (s, e) => Console.WriteLine("Connected To V3 Server");

    //        socket.OnDisconnected += (s, reason) => Console.WriteLine($"Disconnected From V3 Server -${reason}");

    //        socket.OnError += (s, error) => Console.WriteLine($"Connection Error Occurred - {error}");

    //        /*
    //        socket.OnPing += (s, e) => Console.WriteLine($"Ping");
    //        socket.OnPong += (s, timespan) => Console.WriteLine($"Pong"); // 0 Ms???
    //        */
    //        socket.OnAny((name, body) => Console.WriteLine($"Recieved Packet - {name}"));

    //        socket.OnReconnectAttempt += (sender, attmept) => Console.WriteLine($"Reconnecting To V3 Servers - Attempt {attmept}");

    //        socket.OnReconnectFailed += (sender, reason) => Console.WriteLine($"Failed to reconnect to V3 servers - {reason}");

    //        await socket.ConnectAsync();

    //        await Task.Delay(-1); // Prevent program from exiting
    //    }
    //}
}