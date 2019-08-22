using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Test().Wait();
            //var tokenSource = new CancellationTokenSource();
            //Test1(tokenSource);
            //Task.Delay(5000).Wait();
            //tokenSource.Cancel();
            Console.ReadLine();
        }

        static async Task Test()
        {
            var client = new SocketIO("http://localhost:3000");

            client.On("test", args => Console.WriteLine(args.Text));

            await client.ConnectAsync();

            client.OnConnected += async () =>
            {
                //await Task.Delay(3000);
                await client.EmitAsync("test", "cb");
            };

        }
    }
}
