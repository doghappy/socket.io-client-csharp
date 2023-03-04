# Socket.IO-client for .NET

An elegant socket.io client for .NET, it supports socket.io server v2/v3/v4, and has implemented http polling and websocket.

[![Build Status](https://dev.azure.com/doghappy/socket.io-client/_apis/build/status/Unit%20Test%20and%20Integration%20Test?branchName=master)](https://dev.azure.com/doghappy/socket.io-client/_build/latest?definitionId=16&branchName=master)
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
| `EIO` | `V4` | If your server is using socket.io server v2.x, please explicitly set it to V3 |
| `ExtraHeaders` | `null` | Headers that will be passed for each request to the server (via xhr-polling and via websockets). These values then can be used during handshake or for special proxies. |
| `Transport` | `Polling` | Websocket is used by default, you can change to http polling. |
| `AutoUpgrade` | `true` | If websocket is available, it will be automatically upgrade to use websocket |
| `Auth` | `null` | Credentials that are sent when accessing a namespace |

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
var jsonSerializer = client.JsonSerializer as SystemTextJsonSerializer;
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
client.JsonSerializer = jsonSerializer;
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
            mainPage.Socket.AddExpectedException(typeof(Java.Net.SocketException));
            mainPage.Socket.AddExpectedException(typeof(Java.Net.SocketTimeoutException));
            LoadApplication(app);
        }

        ...
    }
```

I don't know the specific exceptions of Xamarin.iOS. Welcome to create a pr and update this document. thanks :)

# Change log

## [3.0.8] - 2023-03-04

### Added

- Expose namepsace as a readonly property #304

### Changed

- Update NuGet dependencies
- Cancel reconnecting when calling Disconnect or Dispose #307
- Improve proformance

## [3.0.7] - 2022-11-29

### Added

- Support custom User-Agent header

### Changed

- Fixed OnAny does not fire when received binary messages
- Update NuGet dependencies
- Fixed http pooling concurrency issues
- Improve proformance

## [3.0.6] - 2022-03-17

### Added

- auth handshake for socket.io server v3
- support auto upgrade transport protocol

[See more](./CHANGELOG.md)

# Thanks

[<img src="https://socket.io/images/logo.svg" width=100px/>](https://github.com/socketio/socket.io) [<img src="https://github.com/darrachequesne.png" width=100px/>](https://github.com/socketio/socket.io) 

Thank [socket.io](https://socket.io/) and [darrachequesne](https://github.com/darrachequesne) for sponsoring the project on [Open Collective](https://opencollective.com/socketio/expenses/).

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider_icon.svg" width=100px/>](https://jb.gg/OpenSourceSupport)

We would like to thank JetBrains for supporting the project with free licenses of their fantastic tools.