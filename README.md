# Socket.IO-client for .NET

An elegant socket.io client for .NET, it supports socket.io server v2/v3/v4, and has implemented http polling and websocket.

[![Build Status](https://herowong.visualstudio.com/socket.io-client/_apis/build/status/doghappy.socket.io-client-csharp?branchName=master)](https://herowong.visualstudio.com/socket.io-client/_build/latest?definitionId=15&branchName=master)
[![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient-%23004880)](https://www.nuget.org/packages/SocketIOClient)
[![Target Framework](https://img.shields.io/badge/Target%20Framework-.NET%20Standard%202.0-%237014e8)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support)
[![NuGet](https://img.shields.io/nuget/dt/SocketIOClient)](https://www.nuget.org/packages/SocketIOClient)

# Table of Contents

- [Quick start](#quick-start)
  - [Options](#options)
  - [Ack](#ack)
  - [Binary messages](#binary-messages)
  - [JsonSerializer](#jsonserializer)
  - [ClientWebSocket Options](#clientwebsocket-options)
  - [Windows 7 Support](#windows-7-support)
  - [Xamarin](#xamarin)
- [Breaking changes](#breaking-changes)
  - [Breaking changes in 3.0.0](#breaking-changes-in-300)
  - [Breaking changes in 2.2.4](#breaking-changes-in-224)
  - [Breaking changes in 2.2.0](#breaking-changes-in-220)
- [Change log](#change-log)
- [Sponsors](#Sponsors)

# Quick start

Connect to the socket.io server, listen events and emit some data.

```cs
var client = new SocketIO("http://localhost:11000/");

client.On("hi", response =>
{
    // You can print the returned data first to decide what to do next.
    // output: ["hi client"]
    Console.WriteLine(response);

    string text = response.GetValue<string>();

    // The socket.io server code looks like this:
    // socket.emit('hi', 'hi client');
});

client.On("test", response =>
{
    // You can print the returned data first to decide what to do next.
    // output: ["ok",{"id":1,"name":"tom"}]
    Console.WriteLine(response);
    
    // Get the first data in the response
    string text = response.GetValue<string>();
    // Get the second data in the response
    var dto = response.GetValue<TestDTO>(1);

    // The socket.io server code looks like this:
    // socket.emit('hi', 'ok', { id: 1, name: 'tom'});
});

client.OnConnected += async (sender, e) =>
{
    // Emit a string
    await client.EmitAsync("hi", "socket.io");

    // Emit a string and an object
    var dto = new TestDTO { Id = 123, Name = "bob" };
    await client.EmitAsync("register", "source", dto);
};
await client.ConnectAsync();
```

## Options

The way to override the default options is as follows:

```cs
var client = new SocketIO("http://localhost:11000/", new SocketIOOptions
{
    Query = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("token", "abc123"),
        new KeyValuePair<string, string>("key", "value")
    }
});
```

| Option | Default value | Description |
| :- | :- | :- |
| `Path` | `/socket.io` | name of the path that is captured on the server side |
| `Reconnection` | `true` | whether to reconnect automatically |
| `ReconnectionAttempts` | `int.MaxValue` | number of reconnection attempts before giving up |
| `ReconnectionDelay` | `1000` | how long to initially wait before attempting a new reconnection. Affected by +/- `RandomizationFactor`, for example the default initial delay will be between 500 to 1500ms. |
| `RandomizationFactor` | `0.5` | 0 <= RandomizationFactor <= 1 |
| `ConnectionTimeout` | `20000` | connection timeout |
| `Query` | `IEnumerable<KeyValuePair<string, string>>` | additional query parameters that are sent when connecting a namespace (then found in `socket.handshake.query` object on the server-side) |
| `EIO` | `4` | If your server is using socket.io server v2.x, please explicitly set it to 3 |
| `ExtraHeaders` | `null` | Headers that will be passed for each request to the server (via xhr-polling and via websockets). These values then can be used during handshake or for special proxies. |
| `Transport` | `WebSocket` | Websocket is used by default, you can change to http polling. |

## Ack

### The server executes the client's ack function

**Client**

```cs
await client.EmitAsync("ack", response =>
{
    // You can print the returned data first to decide what to do next.
    // output: [{"result":true,"message":"Prometheus - server"}]
    Console.WriteLine(response);
    var result = response.GetValue<BaseResult>();
}, "Prometheus");
```

**Server**

```js
socket.on("ack", (name, fn) => {
    fn({
        result: true,
        message: `${name} - server`
    });
});
```

### The client executes the server's ack function

**Client**

```cs
client.On("ack2", async response =>
{
    // You can print the returned data first to decide what to do next.
    // output: [1, 2]
    Console.WriteLine(response);
    int a = response.GetValue<int>();
    int b = response.GetValue<int>(1);
    
    await response.CallbackAsync(b, a);
});
```

**Server**

```js
socket.emit("ack2", 1, 2, (arg1, arg2) => {
    console.log(`arg1: ${arg1}, arg2: ${arg2}`);
});
```

The output of the server is:

```
arg1: 2, arg2: 1
```

## Binary messages

This example shows how to emit and receive binary messages, The library uses System.Text.Json to serialize and deserialize json by default.

```cs
class FileDTO
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("bytes")]
    public byte[] Bytes { get; set; }
}
```

```cs
client.OnConnected += async (sender, e) =>
{
    await client.EmitAsync("upload", new FileDTO
    {
        Name = "template.html"
        MimeType = "text/html",
        bytes = Encoding.UTF8.GetBytes("<div>test</div>")
    });
};

client.On("new files", response =>
{
    // You can print the returned data first to decide what to do next.
    // output: [{"name":"template.html","mimeType":"text/html","bytes":{"_placeholder":true,"num":0}}]
    Console.WriteLine(response);
    var result = response.GetValue<FileDTO>();
    Console.WriteLine(Encoding.UTF8.GetString(result.Bytes))
});
```

## JsonSerializer

The library uses System.Text.Json to serialize and deserialize json by default, If you want to change JsonSerializerOptions, you can do this:

```cs
var client = new SocketIO("http://localhost:11000/");
var jsonSerializer = socket.JsonSerializer as SystemTextJsonSerializer;
jsonSerializer.OptionsProvider = () => new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
```

Of course you can also use Newtonsoft.Json library, for this, you need to install `SocketIOClient.Newtonsoft.Json` dependency.

```cs
var jsonSerializer = new NewtonsoftJsonSerializer();
jsonSerializer.OptionsProvider = () => new JsonSerializerSettings
{
    ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new CamelCaseNamingStrategy()
    }
};
socket.JsonSerializer = jsonSerializer;
```

## ClientWebSocket Options

You can set proxy and add headers for WebSocket client, etc.

```cs
var client = new SocketIO("http://localhost:11000/");
client.ClientWebSocketProvider = () =>
{
    var clientWebSocket = new DefaultClientWebSocket
    {
        ConfigOptions = o =>
        {
            var options = o as ClientWebSocketOptions;

            var proxy = new WebProxy("http://example.com");
            proxy.Credentials = new NetworkCredential("username", "password");
            options.Proxy = proxy;

            options.SetRequestHeader("key", "value");

            options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                Console.WriteLine("SslPolicyErrors: " + sslPolicyErrors);
                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                {
                    return true;
                }
                return true;
            };
        }
    };
    return clientWebSocket;
};
```

## Windows 7 Support

The library uses System.Net.WebSockets.ClientWebSocket by default. Unfortunately, it does not support Windows 7 or Windows Server 2008 R2. You will get a PlatformNotSupportedException. To solve this problem, you need to install the `SocketIOClient.Windows7` dependency and then change the implementation of ClientWebSocket.

```cs
client.ClientWebSocketProvider = () => new ClientWebSocketManaged();
```

## Xamarin

The library will always try to connect to the server, and an exception will be thrown when the connection fails. The library catches some exception types, such as: TimeoutException, WebSocketException, HttpRequestException and OperationCanceledException. If it is one of them, the library will continue to try to connect to the server. If there are other exceptions, the library will stop reconnecting and throw exception to the upper layer. You need extra attention in Xamarin.

For Xamarin.Android you should add the following code:

```cs
    public partial class MainPage: ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public SocketIO Socket {get;}
    }

    ...

    public class MainActivity: global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            ...
            var app = new App();
            var mainPage = app.MainPage as MainPage;
            mainPage.AddExpectedException(typeof(Java.Net.SocketException));
            mainPage.AddExpectedException(typeof(Java.Net.SocketTimeoutException));
            LoadApplication(app);
        }

        ...
    }
```

I don't have a macOS device, and I don't know the specific exceptions of Xamarin.iOS. Welcome to create a pr and update this document. thanks :)

# Breaking changes

## Breaking changes in 3.0.0

> While WebSocket is clearly the best way to establish a bidirectional communication, experience has shown that it is not always possible to establish a WebSocket connection, due to corporate proxies, personal firewall, antivirus software…

https://socket.io/docs/v4/how-it-works/#Transports

- SocketIOClient v3.x supports http polling, but if websocket is available, the library will choose to use websocket. If you want to use http polling and do not want the library to upgrade the transport, please set `Options.AutoUpgrade = false`.
- Socket.io server v2.x is no longer supported. If a large number of users use this version, please feedback.

### Specific break changes

#### 1. EIO option removed

Since socket.io server v2 is not supported, the EIO option is not required.

#### 2. Removed the 'Socket' object

Use ClientWebSocketProvider instead of Socket object.

## Breaking changes in 2.2.4

Before SocketIOClient v2.2.4, the default EIO is 3, which works with socket.io v2.x, in SocketIOClient v2.2.4, the default EIO is 4, which works with socket.io v3.x and v4.x

## Breaking changes in 2.2.0

SocketIOClient v2.2.0 makes `System.Text.Json` the default JSON serializer. If you'd like to continue to use `Newtonsoft.Json`, add the **SocketIOClient.Newtonsoft.Json** NuGet package and set your **JsonSerializer** to **NewtonsoftJsonSerializer** on your SocketIO instance. System.Text.Json is faster and uses less memory.

# Change log

[SocketIOClient](./CHANGELOG.md)

# Sponsors

- [gcoverd](https://github.com/gcoverd), 250 AUD
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/40455), April 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/41876), May 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/44350), June 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/46822), July 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/49090), August 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/51776), September 2021
- [darrachequesne](https://github.com/darrachequesne) ([socket.io team](https://github.com/socketio/socket.io)), [500 USD](https://opencollective.com/socketio/expenses/54770), October 2021
