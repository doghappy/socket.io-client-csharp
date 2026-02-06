# Socket.IO-client for .NET

Languages: [中文简体](./README.zh.md) ｜ English

An elegant socket.io client for .NET, it supports socket.io server v2/v3/v4, and has implemented http polling and websocket.

[![Build Status](https://dev.azure.com/doghappy/socket.io-client/_apis/build/status/Unit%20Test%20and%20Integration%20Test?branchName=master)](https://dev.azure.com/doghappy/socket.io-client/_build/latest?definitionId=16&branchName=master)
[![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient-%23004880)](https://www.nuget.org/packages/SocketIOClient)
[![NuGet](https://img.shields.io/nuget/dt/SocketIOClient)](https://www.nuget.org/packages/SocketIOClient)

# Table of Contents

- [Quick start](#quick-start)
    - [Options](#options)
    - [Ack](#ack)
    - [Binary messages](#binary-messages)
    - [Serializer](#serializer)
    - [Self-signed certificate](#self-signed-certificate)
    - [Proxy](#Proxy)
- [Development](#development)
- [Change log](#change-log)
- [Thanks](#thanks)

# Quick start

Connect to a Socket.IO server to receive and emit events.

```cs
var client = new SocketIO(new Uri("http://localhost:11400"));

client.On("event", ctx =>
{
    // RawText: ["event","Hello World!", 1, {\"Name\":\"Alice\",\"Age\":18}]
    // The first element in the array is the event name,
    // and the subsequent elements are the data carried with the event.
    Console.WriteLine(ctx.RawText);

    // Use index 0 to access the first item in the data payload, which is of type string.
    var message = ctx.GetValue<string>(0)!;
    Console.WriteLine(message); // Hello World!

    // Use index 1 to access the second data item in the payload. The data type is int.
    var id = ctx.GetValue<int>(1);
    Console.WriteLine(id); // 1

    // Use index 2 to access the third data item in the payload. The data type is User.
    var user = ctx.GetValue<User>(2)!;
    Console.WriteLine($"Name: {user.Name}, Age: {user.Age}"); // Name: Alice, Age: 18

    return Task.CompletedTask;
});
```

## Options

An overload is provided here to allow custom configuration of the options.

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

| Option               | Default Value | Description                                                                                                                                                                                                      |
|:--|:--|:--|
| Path                 | /socket.io    | The service endpoint path.|
| Reconnection         | true          | If the initial connection attempt fails, set this to true to keep retrying until the maximum number of reconnection attempts is reached. Set it to false to fail immediately without any further retry attempts. |
| ReconnectionAttempts | 10            | When Reconnection is set to true, the client will automatically retry if the connection fails. The number of retry attempts is determined by ReconnectionAttempts.|
| ReconnectionDelayMax | 5000          | After a reconnection attempt fails, the client will wait for a random delay before trying again. ReconnectionDelayMax defines the upper bound of that random delay.|
| ConnectionTimeout    | 30s           | The timeout duration for each connection attempt.|
| Query                | null          | Before the connection is established, the client can use this parameter to send query string values to the server.|
| EIO                  | V4            | For Socket.IO server v2.x, please set EIO = 3.|
| ExtraHeaders         | null          | Send the request headers with each request to the server. These values can be used during the handshake phase.|
| Transport            | Polling       | By default, HTTP polling is used. When AutoUpgrade is set to true, the connection will automatically upgrade to WebSocket when possible. If the server only supports WebSocket, set Transport = TransportProtocol.WebSocket.|
| AutoUpgrade          | true          | After the handshake, if the server supports WebSocket while the client is currently using polling, the connection will be upgraded to WebSocket.|
| Auth                 | null          | Configure connection credentials. This feature is not supported when EIO = 3.|

## Ack

Emit an event with a callback function. After the server finishes processing, it will invoke the client’s callback function and pass back the relevant data.

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

Listen for an event that includes a callback function. After the client finishes processing, it will send the relevant data back to the server. Once the server receives the data, it will execute its callback function.

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

Send and receive complex data types that include byte[]. By default, System.Text.Json is used for JSON serialization.

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

The above example uses [JsonPropertyName] to customize JSON property names. Global configuration via [JsonSerializerOptions](#serializer) is also supported.

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

By default, System.Text.Json is used for serialization and deserialization. If you want to configure JsonSerializerOptions:

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddSystemTextJson(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
});
```

Of course, if you prefer to use Newtonsoft.Json, you need to install SocketIOClient.Serializer.NewtonsoftJson.

```cs
var client = new SocketIO(new Uri("http://localhost:11400"), services =>
{
    services.AddNewtonsoftJson(new JsonSerializerSettings());
});
```

## Self-signed certificate

If your socket.io server uses a certificate issued by a trusted CA, you should not use these APIs to avoid potential security risks.

However, if your socket.io server uses a self-signed certificate, you may need to customize the certificate validation logic:

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

> Note: By default, the client communicates with the server using polling first. If the server supports WebSocket, the connection will be upgraded to a WebSocket channel. In this scenario, you may need to configure CertificateValidationCallback for both transports.

## Proxy

In some network environments, a proxy must be configured in order to communicate with the server. Proxy can also be used for development and debugging purposes to capture and inspect the entire interaction.

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

> Note: By default, the client communicates with the server using polling first. If the server supports WebSocket, the connection will be upgraded to a WebSocket channel. In this case, you may need to configure a proxy for both transports.

# Development

This library currently follows the software testing pyramid model, with 95% unit test coverage. You can view the coverage details in the Code Coverage section of the Azure DevOps interface.

If you need to run the integration tests locally:

```
cd socket.io-client-csharp/tests/socket.io

npm run install-all # Install the dependencies. This only needs to be done once.

npm run start # Start socket.io server for integration testing
```

# Change log

## [4.0.0.2] - 2026-02-06

### Bugfix

- Trigger the OnDisconnected event when the protocol connection is closed.
- Fix an issue where the Emit ACK handler was not invoked in certain cases.

[See more](./CHANGELOG.md)

After that, you can run the integration tests.

# Thanks

[<img src="https://socket.io/images/logo.svg" width=100px/>](https://github.com/socketio/socket.io) [<img src="https://github.com/darrachequesne.png" width=100px/>](https://github.com/socketio/socket.io)

Thank [socket.io](https://socket.io/) and [darrachequesne](https://github.com/darrachequesne) for sponsoring the project on [Open Collective](https://opencollective.com/socketio/expenses/).

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider_icon.svg" width=100px/>](https://jb.gg/OpenSourceSupport)

We would like to thank JetBrains for supporting the project with free licenses of their fantastic tools.