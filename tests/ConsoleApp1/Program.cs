// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Net.WebSockets;
using System.Text;

var client = new ClientWebSocket();
client.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
var uri = new Uri("wss://echo.websocket.org/");
using var connCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await client.ConnectAsync(uri, connCts.Token);

Console.WriteLine(client.State);

_ = Task.Run(async () =>
{
    while (true)
    {
        // await Task.Delay(1000);
        // Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} {client.State}");

        await Task.Delay(1000);
        var bytes = Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture));
        try
        {
            await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine("***");
            Console.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine(e.Message);
            throw;
        }
    }
});
while (true)
{
    var buffer = new byte[1024];
    Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} Receiving...");
    await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    var text = Encoding.UTF8.GetString(buffer);
    Console.WriteLine($"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} Received {text}");
}

// while (true)
// {
//     await Task.Delay(1000);
//     var bytes = Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture));
//     try
//     {
//         await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
//     }
//     catch (Exception e)
//     {
//         Console.WriteLine("***");
//         Console.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture));
//         Console.WriteLine(e.Message);
//         throw;
//     }
// }