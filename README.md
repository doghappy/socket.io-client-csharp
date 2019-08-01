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
    // Next, you might parse the data in this way.
    var obj = JsonConvert.DeserializeObject<T>(res.Text);
    var jobj = JObject.Parse(res.Text);
    int code = jobj.Value<int>("code");
    bool hasMore = jobj["data"].Value<bool>("hasMore");
    var data = jobj["data"].ToObject<ResponseData>();
    // ...
});

// Connect to the server
await client.ConnectAsync();

// Emit test event, send string.
await client.EmitAsync("test", "EmitTest");

// Emit test event, send object.
await client.EmitAsync("test", new { code = 200 });

// ...

private void Client_OnOpened(Arguments.OpenedArgs args)
{
    Console.WriteLine(args.Sid);
    Console.WriteLine(args.PingInterval);
}

private void Client_OnConnected()
{
    Console.WriteLine("Connected to server");
}

private void Client_OnClosed()
{
    Console.WriteLine("Closed by server");
}
```
