# Socket.IO-client C#

This is the Socket.IO client for .NET, provide a simple way to connect to the Socket.IO server. The target framework is **.NET Standard 2.0**

[![Build Status](https://herowong.visualstudio.com/socket.io-client/_apis/build/status/doghappy.socket.io-client-csharp?branchName=master)](https://herowong.visualstudio.com/socket.io-client/_build/latest?definitionId=15&branchName=master)

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
};

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

client.OnConnected += async () =>
{
    // Emit test event, send string.
    await client.EmitAsync("test", "EmitTest");

    // Emit test event, send object.
    await client.EmitAsync("test", new { code = 200 });
};

// Connect to the server
await client.ConnectAsync();

// ...

private void Client_OnConnected()
{
    Console.WriteLine("Connected to server");
}
```

#### Emit byte array

```cs
await client.EmitAsync("message send", new
{
    body = new
    {
        data = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
        mimeType = "text/plain"
    }
});
```

#### Parse the received byte array

```cs
io.On("message send", a =>
{
    Console.WriteLine("Message: " + a.Text);
    int num = JObject.Parse(a.Text)["body"]["data"].Value<int>("num");
    Console.WriteLine("Buffer: " + Encoding.UTF8.GetString(a.Buffers[num]));
});
```