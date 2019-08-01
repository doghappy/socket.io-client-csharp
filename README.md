# Socket.IO-client C#

This is the Socket.IO client for C#, which is base on `ClientWebSocket`, provide a simple way to connect to the Socket.IO server. The target framework is **.NET Standard 2.0**

### Nuget

Follow-up will be added to NuGet, please wait.

### Usage

```cs
var client = new SocketIO("http://localhost:3000");

client.OnClosed += Client_OnClosed;
client.OnConnected += Client_OnConnected;
client.OnOpened += Client_OnOpened;

// Listen server events
client.On("test", res =>
{
    Console.WriteLine(res.Text);
});

// Connect to the server
await client.ConnectAsync();

// Emit test event, send string.
await client.EmitAsync("test", "EmitTest");

// Emit test event, send object.
await client.EmitAsync("test", new { code = 200 });

...

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
```