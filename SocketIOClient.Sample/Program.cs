using Newtonsoft.Json;
using System;
using System.IO;
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
            client.OnClosed += async () =>
            {
                await Task.Delay(60000);
                await client.ConnectAsync();
                await client.EmitAsync("test", "test");
            };

            client.OnClosed += Client_OnClosed;
            client.OnConnected += Client_OnConnected;
            //client.OnOpened += Client_OnOpened;
            //client.OnAborted += () => Console.WriteLine("Aborted");

            // Listen server events
            client.On("ws_message -new", res =>
            {
                Console.WriteLine(res.Text);
            });

            await client.ConnectAsync();

            // Emit test event, send string.
            await client.EmitAsync("ws_message -new", "ws_message-new");
            //await client.EmitAsync("close", "close");
        }

        static void Test1(CancellationTokenSource tokenSource)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    await Task.Delay(1000);
                    Console.WriteLine(DateTime.Now);
                }
            }, tokenSource.Token);
        }
    }
}
