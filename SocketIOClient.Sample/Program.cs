using Newtonsoft.Json;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var client = new SocketIO("http://localhost:3000");
            //client.ConnectTimeout = TimeSpan.FromSeconds(5);

            //client.On("test", args =>
            //{
            //    string text = JsonConvert.DeserializeObject<string>(args.Text);
            //    Console.WriteLine(text);
            //});

            //client.OnConnected += async () =>
            //{
            //    for (int i = 0; i < 5; i++)
            //    {
            //        await client.EmitAsync("test", i.ToString());
            //        await Task.Delay(1000);
            //    }

            //    await client.EmitAsync("close", "close");
            //};

            //client.OnClosed += Client_OnClosed;

            //await client.ConnectAsync();

            //-----------------
            //var client = new SocketIO("https://socket.stex.com/");

            //client.On("App\\Events\\GlassRowChanged", res =>
            //{
            //    Console.WriteLine(res.Text);
            //});

            //client.OnConnected += async () =>
            //{
            //    var obj = new
            //    {
            //        channel = "orderbook_data250",
            //        auth = new { }
            //    };

            //    await client.EmitAsync("subscribe", obj);
            //};

            //await client.ConnectAsync();
            //-----------------
            var client = new SocketIO("http://localhost:3000");
            string result = null;
            client.On("emit-noting", res =>
            {
                result = res.Text;
                Console.WriteLine(result);
                //// await client.CloseAsync();
            });
            await client.ConnectAsync();
            await Task.Delay(1000);
            await client.EmitAsync("emit-noting", null);

            Console.ReadLine();
        }

        //private static void Client_OnClosed(ServerCloseReason reason)
        //{
        //    Console.WriteLine("reason: " + reason.ToString());
        //}
    }
}
