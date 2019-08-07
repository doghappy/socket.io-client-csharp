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
            client.OnClosed += async reason =>
            {
                //await Task.Delay(60000);
                //await client.ConnectAsync();
                //await client.EmitAsync("test", "test");
                if (reason == ServerCloseReason.ClosedByServer)
                {
                    // ...
                }
                else if (reason == ServerCloseReason.Aborted)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            await client.ConnectAsync();
                            break;
                        }
                        catch (WebSocketException ex)
                        {
                            // show tips
                            Console.WriteLine(ex.Message);
                            await Task.Delay(2000);
                        }
                    }
                    // show tips
                    Console.WriteLine("Tried to reconnect 3 times, unable to connect to the server");
                }
            };
            await client.ConnectAsync();
            await Task.Delay(10000);
            await client.EmitAsync("close", "close");
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
