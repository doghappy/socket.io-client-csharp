using System.Diagnostics;
using System.Text;
using SocketIOClient;

PrintCurrentStatus();

var client = new SocketIO(new Uri("http://localhost:11400"));

client.On("1:emit", _ => Task.CompletedTask);

await client.ConnectAsync();

const int count = 1000;
const int delay = 20;
for (var i = 1; i <= count; i++)
{
    Console.SetCursorPosition(0, 9);
    Console.Write($"{i} / {count}");
    await client.EmitAsync("1:emit", [i]);
    await Task.Delay(delay);
    await client.EmitAsync("1:emit", [Encoding.UTF8.GetBytes(i.ToString())]);
    await Task.Delay(delay);
    await client.EmitAsync("1:ack", ["action"], _ => Task.CompletedTask);
    await Task.Delay(delay);
}
Console.WriteLine();

PrintCurrentStatus();

void PrintCurrentStatus()
{
    Process currentProcess = Process.GetCurrentProcess();
    Console.WriteLine("------------------------------------");
    Console.WriteLine($"WorkingSet: {currentProcess.WorkingSet64 / 1024 / 1024} MB");
    Console.WriteLine($"PrivateMemorySize: {currentProcess.PrivateMemorySize64 / 1024 / 1024} MB");
    Console.WriteLine($"VirtualMemorySize: {currentProcess.VirtualMemorySize64 / 1024 / 1024} MB");

    Console.WriteLine($"TotalProcessorTime: {currentProcess.TotalProcessorTime}");
    Console.WriteLine($"UserProcessorTime: {currentProcess.UserProcessorTime}");
    Console.WriteLine($"PrivilegedProcessorTime: {currentProcess.PrivilegedProcessorTime}");

    Console.WriteLine($"HandleCount: {currentProcess.HandleCount}");
    Console.WriteLine($"ThreadCount: {currentProcess.Threads.Count}");
}