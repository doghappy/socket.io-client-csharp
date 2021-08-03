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

            var uri = new Uri("http://localhost:11003");

            var socket = new SocketIO(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    {"token", "V3" }
                }
            });


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
            socket.OnReconnectAttempt += Socket_OnReconnecting;

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

            await socket.EmitAsync("hi", "SocketIOClient.Sample");
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
}