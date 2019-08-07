# Socket.IO-client C#

This is the Socket.IO client for C#, which is base on `ClientWebSocket`, provide a simple way to connect to the Socket.IO server. The target framework is **.NET Standard 2.0**

### Nuget

```
Install-Package SocketIOClient
```

### Usage

```cs
var client = new SocketIO("http://localhost:3000")
{
    // if server need some parameters, you can add to here
    Parameters = new Dictionary<string, string>
    {
        { "uid", "" },
        { "token", "" }
    }
}

client.OnClosed += Client_OnClosed;
client.OnConnected += Client_OnConnected;

// Listen server events
client.On("test", res =>
{
    Console.WriteLine(res.Text);
    // Next, you might parse the data in this way.
    var obj = JsonConvert.DeserializeObject<T>(res.Text);
	// Or, read some fields
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

private void Client_OnConnected()
{
    Console.WriteLine("Connected to server");
}

private async void Client_OnClosed()
{
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
}
```
