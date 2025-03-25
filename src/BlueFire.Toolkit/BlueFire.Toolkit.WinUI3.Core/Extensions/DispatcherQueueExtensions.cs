using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    internal static class DispatcherQueueExtensions
    {
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

        public static void RunSynchronously(this DispatcherQueue dispatcherQueue, Func<CancellationToken, Task> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);

            ExceptionDispatchInfo? eInfo = null;

            dispatcherQueue.TryEnqueue(priority, async () =>
            {
                try
                {
                    await func.Invoke(cancellationToken);
                }
                catch (Exception ex)
                {
                    eInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    dispatcherQueue.EnqueueEventLoopExit();
                }
            });

            dispatcherQueue.RunEventLoop(DispatcherRunOptions.QuitOnlyLocalLoop, null);

            eInfo?.Throw();
        }

        public static T? RunSynchronously<T>(this DispatcherQueue dispatcherQueue, Func<CancellationToken, Task<T>> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);

            ExceptionDispatchInfo? eInfo = null;

            T? result = default;

            dispatcherQueue.TryEnqueue(priority, async () =>
            {
                try
                {
                    result = await func.Invoke(cancellationToken);
                }
                catch (Exception ex)
                {
                    eInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    dispatcherQueue.EnqueueEventLoopExit();
                }
            });

            dispatcherQueue.RunEventLoop(DispatcherRunOptions.QuitOnlyLocalLoop, null);

            eInfo?.Throw();

            return result;
        }
    }
}
