var server = args[0];
WriteLine($"server: {server}");
var io = new SocketIOClient.SocketIO(server);
io.OnConnected += (_, _) => WriteLine("Connected");
io.OnDisconnected += (_, _) => WriteLine("Disconnected");
io.OnReconnectAttempt += (_, t) => WriteLine($"OnReconnectAttempt: {t}"); 
io.OnPing += (_, _) => WriteLine($"Ping"); 
io.OnPong += (_, t) => WriteLine($"OnPong: {t}");
await io.ConnectAsync();

while (true)
{
    WriteLine("Type 'exit' to exit.");
    var cmd = Console.ReadLine();
    if (cmd == "exit")
    {
        break;
    }
}

WriteLine("Exited");
return;

void WriteLine(string s)
{
    Console.WriteLine($"{DateTime.Now} {s}");
} 
