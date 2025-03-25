using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace BlueFire.Toolkit.Extensions
{
    public static class SystemDispatcherQueueExtensions
    {
        internal static T RunSynchronously<T>(this Windows.System.DispatcherQueue dispatcherQueue, Func<Task<T>> func)
        {
            if (dispatcherQueue.HasThreadAccess) throw new NotSupportedException();

            T? result = default;
            ExceptionDispatchInfo? exceptionInfo = null;

            var locker = new object();

            lock (locker)
            {
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        result = await func();
                    }
                    catch (Exception ex)
                    {
                        exceptionInfo = ExceptionDispatchInfo.Capture(ex);
                    }
                    finally
                    {
                        lock (locker) { Monitor.Pulse(locker); }
                    }
                });

                Monitor.Wait(locker);
            }

            exceptionInfo?.Throw();

            return result!;
        }


        public static Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            cancellationToken.ThrowIfCancellationRequested();

            if (dispatcherQueue.HasThreadAccess)
            {
                action.Invoke();
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource();

                dispatcherQueue.TryEnqueue(priority, () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    try
                    {
                        action.Invoke();
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }
        }

        public static Task<T> EnqueueAsync<T>(this DispatcherQueue dispatcherQueue, Func<T> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);

            cancellationToken.ThrowIfCancellationRequested();

            if (dispatcherQueue.HasThreadAccess)
            {
                var result = func.Invoke();
                return Task.FromResult(result);
            }
            else
            {
                var tcs = new TaskCompletionSource<T>();

                dispatcherQueue.TryEnqueue(priority, () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    try
                    {
                        var result = func.Invoke();
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }
        }

        public static Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Func<CancellationToken, Task> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);

            cancellationToken.ThrowIfCancellationRequested();

            if (dispatcherQueue.HasThreadAccess)
            {
                return func.Invoke(cancellationToken);
            }
            else
            {
                var tcs = new TaskCompletionSource();

                dispatcherQueue.TryEnqueue(priority, async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    try
                    {
                        await func.Invoke(cancellationToken);
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }
        }

        public static Task<T?> EnqueueAsync<T>(this DispatcherQueue dispatcherQueue, Func<CancellationToken, Task<T?>> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);

            cancellationToken.ThrowIfCancellationRequested();

            if (dispatcherQueue.HasThreadAccess)
            {
                return func.Invoke(cancellationToken);
            }
            else
            {
                var tcs = new TaskCompletionSource<T?>();

                dispatcherQueue.TryEnqueue(priority, async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    try
                    {
                        var result = await func.Invoke(cancellationToken);
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return tcs.Task;
            }
        }


        public static void RunSynchronously(this DispatcherQueue dispatcherQueue, Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (dispatcherQueue.HasThreadAccess)
            {
                action.Invoke();
            }
            else
            {
                ExceptionDispatchInfo? eInfo = null;

                var locker = new object();

                lock (locker)
                {
                    dispatcherQueue.TryEnqueue(priority, () =>
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            eInfo = ExceptionDispatchInfo.Capture(ex);
                        }
                        finally
                        {
                            lock (locker)
                            {
                                Monitor.Pulse(locker);
                            }
                        }
                    });

                    Monitor.Wait(locker);
                }

                eInfo?.Throw();
            }
        }

        public static T? RunSynchronously<T>(this DispatcherQueue dispatcherQueue, Func<T?> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            ArgumentNullException.ThrowIfNull(func);

            if (dispatcherQueue.HasThreadAccess)
            {
                return func.Invoke();
            }
            else
            {
                T? result = default;
                ExceptionDispatchInfo? eInfo = null;

                var locker = new object();

                lock (locker)
                {
                    dispatcherQueue.TryEnqueue(priority, () =>
                    {
                        try
                        {
                            result = func.Invoke();
                        }
                        catch (Exception ex)
                        {
                            eInfo = ExceptionDispatchInfo.Capture(ex);
                        }
                        finally
                        {
                            lock (locker)
                            {
                                Monitor.Pulse(locker);
                            }
                        }
                    });

                    Monitor.Wait(locker);
                }

                eInfo?.Throw();

                return result;
            }
        }
    }
}
