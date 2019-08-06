using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
            Console.ReadLine();
        }

        static async Task Test()
        {
            //var client = new SocketIO("http://localhost:3000/");
            var client = new SocketIO("http://localhost:3000/path");

            client.OnClosed += Client_OnClosed;
            client.OnConnected += Client_OnConnected;
            //client.OnOpened += Client_OnOpened;
            //client.OnAborted += () => Console.WriteLine("Aborted");

            // Listen server events
            client.On("test", res =>
            {
                string resText = JsonConvert.DeserializeObject<string>(res.Text);
                Console.WriteLine(res.Text);
            });

            // Connect to the server
            await client.ConnectAsync();

            // Emit test event, send string.
            string text = File.ReadAllText("test.txt");
            await client.EmitAsync("test", text);
            //await client.EmitAsync("close", "close");
        }

        private static void Client_OnOpened(Arguments.OpenedArgs args)
        {
            Console.WriteLine(args.Sid);
            Console.WriteLine(args.PingInterval);
        }

        private static void Client_OnConnected()
        {
            Console.WriteLine("Connected to server");
        }

        private static void Client_OnClosed()
        {
            Console.WriteLine("Closed by server");
        }
    }
}
