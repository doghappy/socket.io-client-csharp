using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

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

        internal static IObservable<string> RemoveNamespace(this IObservable<string> observable, string ns)
        {
            return observable.Select(x => string.IsNullOrEmpty(ns) ? x : x.Substring(ns.Length));
        }
    }
}
