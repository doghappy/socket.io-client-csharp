# Socket.IO-client for .NET

An elegant socket.io client for .NET

[![Build Status](https://herowong.visualstudio.com/socket.io-client/_apis/build/status/doghappy.socket.io-client-csharp?branchName=master)](https://herowong.visualstudio.com/socket.io-client/_build/latest?definitionId=15&branchName=master)

## How to use

### Nuget

```
Install-Package SocketIOClient
```

### Example of usage

#### Emit an event

**Client:**

```cs
var client = new SocketIO("http://localhost:11000/");
client.On("hi", response =>
{
    string text = response.GetValue<string>();
});
client.OnConnected += async (sender, e) =>
{
    await client.EmitAsync("hi", ".net core");
};
await client.ConnectAsync();
```

**Server:**

```ts
socket.on("hi", name => {
    socket.emit("hi", `hi ${name}, You are connected to the server`);
});
```

#### Emit with Ack

**Client:**

```cs
var client = new SocketIO("http://localhost:11000/");
client.OnConnected += async (sender, e) =>
{
    await client.EmitAsync("ack", response =>
    {
        result = response.GetValue();
    }, ".net core");
};
await client.ConnectAsync();
```

**Server:**

```ts
socket.on("ack", (name, fn) => {
    fn({
        result: true,
        message: `ack(${name})`
    });
});
```

#### Emit with Binary

**Client:**

```cs
var client = new SocketIO("http://localhost:11000/");
client.OnConnected += async (sender, e) =>
{
    await client.EmitAsync("bytes", name, new
    {
        source = "client001",
        bytes = Encoding.UTF8.GetBytes(".net core")
    });
};

client.On("bytes", response =>
{
    var result = response.GetValue<ByteResponse>();
});

await client.ConnectAsync();
```

```cs
class ByteResponse
{
    public string ClientSource { get; set; }

    public string Source { get; set; }

    [JsonProperty("bytes")]
    public byte[] Buffer { get; set; }
}
```

**Server:**

```ts
socket.on("bytes", (name, data) => {
    const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
    socket.emit("bytes", {
        clientSource: data.source,
        source: "server",
        bytes
    });
});
```