using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest
{
    public static class TestHelper
    {
        public static ILogger Logger = NullLogger.Instance;

        public static void OnNextLater<T>(this ISubject<T> subject, T data, int milliseconds = 120)
        {
            _ = Task.Run(() =>
            {
                Thread.Sleep(milliseconds);
                subject.OnNext(data);
            });
        }

        public static void OnNextLater<T>(this ISubject<T> subject, IEnumerable<T> data, int milliseconds = 120)
        {
            _ = Task.Run(() =>
            {
                Thread.Sleep(milliseconds);
                foreach (var item in data)
                {
                    subject.OnNext(item);
                }
            });
        }
    }
}
