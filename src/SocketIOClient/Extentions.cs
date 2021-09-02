using System;
using System.Reactive.Linq;

namespace SocketIOClient
{
    public static class Extentions
    {
        internal static IObservable<T> Log<T>(this IObservable<T> observable, string name = "")
        {
            return observable.Do(
                x => Console.WriteLine($"{name} - OnNext({x})"),
                ex =>
                {
                    Console.WriteLine($"{name} - OnError:");
                    Console.WriteLine($"\t {ex}");
                },
                () => Console.WriteLine($"{name} - OnCompleted()")
            );
        }
    }
}
