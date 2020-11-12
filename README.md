# Socket.IO-client for .NET

An elegant socket.io client for .NET

[![Build Status](https://herowong.visualstudio.com/socket.io-client/_apis/build/status/doghappy.socket.io-client-csharp?branchName=master)](https://herowong.visualstudio.com/socket.io-client/_build/latest?definitionId=15&branchName=master)

## How to use

If your TargetFramework is `.NET Framework` and it runs on `Windows7/Windows Server 2008 R2`, please install [![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient.NetFx-%23004880)](https://www.nuget.org/packages/SocketIOClient.NetFx)

Otherwise, we recommend you to install [![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient-%23004880)](https://www.nuget.org/packages/SocketIOClient)

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

#### Get multiple response values

**Client:**

```cs
var client = new SocketIO("http://localhost:11000/");
client.OnConnected += async (sender, e) =>
{
    await client.EmitAsync("change", new
    {
        code = 200,
        message = "val1"
    }, "val2");
};
client.On("change", response =>
{
    // You can get the JSON string of the response by calling response.ToString()
    // After that you can decide how to parse the response data.
    // For example: ["val2", { "code": 200, "message": "val1" }]
    string resVal1 = response.GetValue<string>();
    ChangeResponse resVal2 = response.GetValue<ChangeResponse>(1);

    // If you don't want to create a model, you can parse it like this
    string message = response.GetValue(1).Value<string>("message");
    int code = response.GetValue(1).Value<int>("code");

    // More specific usage: https://github.com/jamesnk/newtonsoft.json
});
await client.ConnectAsync();
```

```cs
class ChangeResponse
{
    public int Code { get; set; }
    public string Message { get; set; }
}
```

**Server:**

```ts
socket.on("change", (val1, val2) => {
    socket.emit("change", val2, val1);
})
```

#### SSL & Proxy

SSL and Proxy are related to your program

**.NET Core**

```cs
var proxy = new System.Net.WebProxy("http://example.com");
proxy.Credentials = new NetworkCredential("username", "password");

var socket = client.Socket as ClientWebSocket;
socket.Config = options =>
{
    options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
    {
        Console.WriteLine("SslPolicyErrors: " + sslPolicyErrors);
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }
        return true;
    };
	
	// Set Proxy
    options.Proxy = proxy;
};
await client.ConnectAsync();
```

**.NET Framework**

.NET Framework is more complicated

```cs
var proxy = new System.Net.WebProxy("http://example.com");
proxy.Credentials = new NetworkCredential("username", "password");

if (client.Socket is WebSocketSharpClient)
{
    var socket = client.Socket as WebSocketSharpClient;
    socket.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls;
    socket.Proxy = proxy;
    socket.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
    {
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }
        Console.WriteLine(sslPolicyErrors);
        return false;
    };
}
else
{
    var socket = client.Socket as ClientWebSocket;
    System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
    {
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
        {
            return true;
        }
        Console.WriteLine(sslPolicyErrors);
        return false;
    };

    // Set Proxy
	socket.Config = options => options.Proxy = proxy;
}
await client.ConnectAsync();
```

### Change log

[SocketIOClient](./CHANGELOG.md)  
[SocketIOClient.NetFx](./CHANGELOG-NetFx.md)
