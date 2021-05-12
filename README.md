# Socket.IO-client for .NET

An elegant socket.io client for .NET, Supports `.NET Framework 4.5` and `.NET Standard 2.0`.

[![Build Status](https://herowong.visualstudio.com/socket.io-client/_apis/build/status/doghappy.socket.io-client-csharp?branchName=master)](https://herowong.visualstudio.com/socket.io-client/_build/latest?definitionId=15&branchName=master)
[![NuGet](https://img.shields.io/badge/NuGet-SocketIOClient-%23004880)](https://www.nuget.org/packages/SocketIOClient)

# How to use

[Wiki](https://github.com/doghappy/socket.io-client-csharp/wiki)

# Breaking changes in 2.2.0

SocketIOClient v2.2.0 makes `System.Text.Json` the default JSON serializer. If you'd like to continue to use `Newtonsoft.Json`, add the **SocketIOClient.Newtonsoft.Json** NuGet package and set your **JsonSerializer** to **NewtonsoftJsonSerializer** on your SocketIO instance. System.Text.Json is faster and uses less memory.

### Continue to use Newtonsoft.Json

```cs
var client = new SocketIO("http://localhost:11000/");
client.JsonSerializer = new NewtonsoftJsonSerializer(client.Options.EIO);
```

### Custom JsonSerializerOptions/System.Text.Json

```cs
class MyJsonSerializer : SystemTextJsonSerializer
{
    public MyJsonSerializer(int eio) : base(eio) {}

    public override JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOption();
        options.PropertyNameCaseInsensitive = true;
        return options;
    }
}

// ...

var client = new SocketIO("http://localhost:11000/");
client.JsonSerializer = new MyJsonSerializer(client.Options.EIO);
```

### Custom JsonSerializerSettings/Newtonsoft.Json

```cs
class MyJsonSerializer : NewtonsoftJsonSerializer
{
    public MyJsonSerializer(int eio) : base(eio) {}

    public override JsonSerializerSettings CreateOptions()
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new global::Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new global::Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
    }
}

// ...

var client = new SocketIO("http://localhost:11000/");
client.JsonSerializer = new MyJsonSerializer(client.Options.EIO);
```

## Development

Before development or testing, you need to install the nodejs.

```bash
# start socket.io v2 server
cd src/socket.io-server
npm i # If the dependencies are already installed, you can ignore this step.
npm start

# start socket.io v3 server
cd src/socket.io-server-v3
npm i # If the dependencies are already installed, you can ignore this step.
npm start
```

## Change log

[SocketIOClient](./CHANGELOG.md)

## Sponsors

- [gcoverd](https://github.com/gcoverd), 250 AUD
- [darrachequesne(socket.io team)](https://github.com/darrachequesne), 500 USD