# Socket.IO-client for .NET

语言：中文简体 ｜ [English](./README.md)

socket.io client 的 .NET 实现, 它支持 socket.io server v2/v3/v4, 并且支持 Http 轮询和 WebSocket 两种通信模式。

[![Build Status](https://dev.azure.com/doghappy/socket.io-client/_apis/build/status/Unit%20Test%20and%20Integration%20Test?branchName=master)](https://dev.azure.com/doghappy/socket.io-client/_build/latest?definitionId=16&branchName=master)
[![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient-%23004880)](https://www.nuget.org/packages/SocketIOClient)
[![NuGet](https://img.shields.io/nuget/dt/SocketIOClient)](https://www.nuget.org/packages/SocketIOClient)

# 目录

- [Quick start](#quick-start)
  - [Options](#options)
  - [Ack](#ack)
  - [Binary messages](#binary-messages)
  - [Serializer](#serializer)
  - [自签名证书](#自签名证书)
  - [代理](#代理)
- [开发](#开发)
- [Change log](#change-log)
- [Thanks](#thanks)

# Quick start

连接到 socket.io 服务，接收和发送事件

```cs
var client = new SocketIO(new Uri("http://localhost:11400"));

client.On("event", ctx =>
{
    // RawText: ["event","Hello World!", 1, {\"Name\":\"Alice\",\"Age\":18}]
    // 数组中的第一个元素是 event name，之后的元素是 event 携带的数据
    Console.WriteLine(ctx.RawText);

    // 使用索引 0 获取数据部分的第1个数据，在这个例子中数据类型是 string
    var message = ctx.GetValue<string>(0)!;
    Console.WriteLine(message); // Hello World!

    // 使用索引 1 获取数据部分的第2个数据，数据类型是 int
    var id = ctx.GetValue<int>(1);
    Console.WriteLine(id); // 1

    // 使用索引 2 获取数据部分的第3个数据，数据类型是 User 类型
    var user = ctx.GetValue<User>(2)!;
    Console.WriteLine($"Name: {user.Name}, Age: {user.Age}"); // Name: Alice, Age: 18

    return Task.CompletedTask;
});
```

## Options

这里提供了一个重载，用来自定义 Options

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), new SocketIOOptions
{
    Query = new NameValueCollection
    {
        ["user"] = "Alice"
    },
    // ...
});
```

| Option               | 默认值 | 描述|
|:--|:--|:--|
| Path                 | /socket.io | 服务的路径|
| Reconnection         | true       | 初次连接时失败时，true 继续尝试连接直到重连次数超过指定值，false 连接失败后立即报错，不会继续尝试|
| ReconnectionAttempts | 10         | 当 Reconnection 为 true 时，如果连接失败了会自动进行重试，重试次数为 ReconnectionAttempts|
| ReconnectionDelayMax | 5000       | 当重新连接失败后，会随机休眠一段时间后再次尝试连接，ReconnectionDelayMax 是随机数的上限|
| ConnectionTimeout    | 30s        | 每次请求建立连接的超时时间|
| Query                | null       | 在连接建立前，客户端可以通过此参数传递一些 query string 到服务端|
| EIO                  | V4         | 对于 socket.io server v2.x, 请设置 EIO = V3|
| ExtraHeaders         | null       | 将请求头随每个请求一起发送到服务端。这些值可以在握手阶段使用|
| Transport            | Polling    | 默认使用 Http Polling，当 AutoUpgrade 为 true 时，会自动升级到 WebSocket。如果服务端只支持 WebSocket 请设置 `Transport = TransportProtocol.WebSocket` |
| AutoUpgrade          | true       | 在握手后，如何服务端支持 WebSocket，而客户端使用的是 Polling，则会升级到 WebSocket|
| Auth                 | null       | 设置连接凭证，当 EIO = V3 时不支持此特性|

## Ack

发送一个带有回调函数的事件，服务端在处理结束后，后调用客户端的回调函数，并传递相关的数据。

**Client**

```cs
await client.EmitAsync("hi", ["Hi, I'm Client"], ack =>
{
    Console.WriteLine(ack.RawText);
    // RawText: ["Hi, I'm Client","Hi, I'm Server"]

    var message1 = ack.GetValue<string>(0)!;
    Console.WriteLine(message1); // Hi, I'm Client

    var message2 = ack.GetValue<string>(1)!;
    Console.WriteLine(message2); // Hi, I'm Server

    return Task.CompletedTask;
});
```

**Server**

```js
socket.on('hi', (m1, fn) => {
    fn(m1, 'Hi, I\'m Server');
});
```

监听一个带有回调函数的事件，客户端在处理结束后，会向服务端传递相关的数据，当服务端收到数据后会在服务端执行回调函数。

**Client**

```cs
client.On("add", async ctx =>
{
    var a = ctx.GetValue<int>(0);
    var b = ctx.GetValue<int>(1);
    var c = a + b;

    await ctx.SendAckDataAsync([c]);
});
```

**Server**

```js
socket.emit('add', 1, 2, c => {
    console.log(c); // 3
});
```

## Binary messages

发送和监听带有 byte[] 的复杂类型数据。默认使用 System.Text.Json 作为 json 序列化

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

上述例子使用了 [JsonPropertyName] 来自定义 json 属性名，全局配置 [JsonSerializerOptions](#serializer) 也是支持的。

```cs
await client.EmitAsync("1:emit", [
    new FileDTO
    {
        Name = "template.html",
        MimeType = "text/html",
        Bytes = Encoding.UTF8.GetBytes("<div>test</div>")
    }
]);

client.On("new files", ctx =>
{
    // RawText: ["new files", {"name":"template.html","mimeType":"text/html","bytes":{"_placeholder":true,"num":0}}]
    var result = ctx.GetValue<FileDTO>();
    Console.WriteLine(Encoding.UTF8.GetString(result.Bytes))
});
```

## Serializer

默认情况下使用 System.Text.Json 进行序列化与反序列化，如果你想配置 JsonSerializerOptions：

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSystemTextJson(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
});
```

当然，如果你想使用 Newtonsoft.Json 则需要安装 `SocketIOClient.Serializer.NewtonsoftJson`

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddNewtonsoftJson(new JsonSerializerSettings());
});
```

## 自签名证书

如果你的 socket.io 服务端使用的是受信任的 CA 签发的证书，请不要使用这些 API，避免安全问题。
但如果 socket.io 服务端使用了自签名证书，你可能需要自定义证书校验逻辑：

### Http Polling

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSingleton<HttpClient>(_ =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (s, cert, chain, policyError) =>
            {
                var isValid = ...
                return isValid;
            }
        };

        return new HttpClient(handler);
    });
});
```

### WebSocket

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSingleton(new WebSocketOptions
    {
        RemoteCertificateValidationCallback = (s, cert, chain, policyError) =>
        {
            var isValid = ...
            return isValid;
        };
    });
});
```

> 注意：默认情况下优先使用 Polling 与服务端通信，如果服务端支持 WebSocket，则会升级到 WebSocket 信道，在这种场景下，你可能需要同时为两者配置 CertificateValidationCallback

## 代理

在某些特殊的网络环境中，需要配置代理才能与服务器通信，或者出于开发和调试的目的，使用代理软件记录整个交互过程。

### Http Polling

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSingleton<HttpClient>(_ =>
    {
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy(proxyUrl),
            UseProxy = true
        };

        return new HttpClient(handler);
    });
});
```

### WebSocket

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSingleton(new WebSocketOptions
    {
        Proxy = new WebProxy(proxyUrl)
    });
});
```

> 注意：默认情况下优先使用 Polling 与服务端通信，如果服务端支持 WebSocket，则会升级到 WebSocket 信道，在这种场景下，你可能需要同时为两者配置代理

# 开发

此 Lib 目前是符合软件测试金字塔模型的，单元测试覆盖率 95%，可以在 Azure DevOps 界面的 Code Coverage 中看到。

如果需要在本地运行集成测试：

```
cd socket.io-client-csharp/tests/socket.io

npm run install-all # 安装依赖，只需要运行一次即可

npm run start # 启动 socket.io server 测试服务
```

# Change log

## [4.0.0] - 2026-01-28

### Architecture Refactor
- Reworked the internal architecture to improve modularity, maintainability, and long-term extensibility
- Clearer separation of responsibilities between core components
- Reduced coupling between modules, making future enhancements safer and easier

### Performance Enhancements
- Improved execution efficiency in key processing paths
- Reduced unnecessary allocations and redundant operations
- Optimized data access and internal workflows for better runtime performance

[See more](./CHANGELOG.md)

然后就可以运行集成测试了

# Thanks

[<img src="https://socket.io/images/logo.svg" width=100px/>](https://github.com/socketio/socket.io) [<img src="https://github.com/darrachequesne.png" width=100px/>](https://github.com/socketio/socket.io)

Thank [socket.io](https://socket.io/) and [darrachequesne](https://github.com/darrachequesne) for sponsoring the project on [Open Collective](https://opencollective.com/socketio/expenses/).

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider_icon.svg" width=100px/>](https://jb.gg/OpenSourceSupport)

We would like to thank JetBrains for supporting the project with free licenses of their fantastic tools.