using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    internal static class DispatcherQueueExtensions
    {
        internal static T RunSync<T>(this Windows.System.DispatcherQueue dispatcherQueue, Func<Task<T>> func)
        {
            var locker = new object();

            T? result = default;
            Exception? ex = null;

            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    result = await func();
                }
                catch (Exception _ex)
                {
                    ex = _ex;
                }
                finally
                {
                    lock (locker)
                    {
                        Monitor.Pulse(locker);
                    }
                }
            });

            lock (locker)
            {
                Monitor.Wait(locker);
            }

            if (ex != null) throw new AggregateException(ex);

            return result!;
        }
    }
}
