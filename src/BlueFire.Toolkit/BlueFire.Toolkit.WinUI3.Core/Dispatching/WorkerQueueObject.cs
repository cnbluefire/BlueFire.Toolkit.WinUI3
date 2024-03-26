using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Core.Dispatching
{
    /// <summary>
    /// Create a background thread with a message loop.
    /// </summary>
    public class WorkerQueueObject : IDisposable
    {
        private bool disposedValue;
        private readonly ApartmentState apartmentState;

        private object locker = new object();
        private Thread? thread;
        private DispatcherQueueController? dispatcherQueueController;
        private DispatcherQueue? dispatcherQueue;

        protected bool IsDisposed => disposedValue;

        protected WorkerQueueObject(ApartmentState apartmentState)
        {
            this.apartmentState = apartmentState;
        }

        protected WorkerQueueObject() : this(ApartmentState.MTA) { }

        /// <summary>
        /// DispatcherQueue created on the background thread.
        /// </summary>
        protected internal DispatcherQueue DispatcherQueue
        {
            get
            {
                ThrowIfDisposed();

                if (dispatcherQueue == null)
                {
                    lock (locker)
                    {
                        if (dispatcherQueue == null)
                        {
                            var waitEvent = new object();
                            Exception? exception = null;

                            thread = new Thread(() =>
                            {
                                try
                                {
                                    dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();
                                    dispatcherQueue = dispatcherQueueController.DispatcherQueue;
                                    dispatcherQueue.EnsureSystemDispatcherQueue();
                                }
                                catch (Exception ex)
                                {
                                    exception = ex;
                                }
                                finally
                                {
                                    lock (waitEvent)
                                    {
                                        Monitor.Pulse(waitEvent);
                                    }
                                    dispatcherQueue?.RunEventLoop();
                                }
                            });
                            thread.IsBackground = true;
                            thread.Name = "Worker Queue Thread";
                            thread.SetApartmentState(apartmentState);
                            thread.Start();

                            lock (waitEvent)
                            {
                                Monitor.Wait(waitEvent);
                            }

                            if (exception != null)
                            {
                                ThrowAggregateException(exception);
                            }
                        }
                    }
                }

                return dispatcherQueue!;
            }
        }

        protected async Task RunOnDispatcherQueueAsync(Func<CancellationToken, Task> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (func == null) throw new ArgumentNullException(nameof(func));

            cancellationToken.ThrowIfCancellationRequested();

            if (DispatcherQueue.HasThreadAccess)
            {
                await func.Invoke(cancellationToken);
            }
            else
            {
                var tcs = new TaskCompletionSource();

                DispatcherQueue.TryEnqueue(priority, async () =>
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

                await tcs.Task;
            }
        }

        protected async Task<T?> RunOnDispatcherQueueAsync<T>(Func<CancellationToken, Task<T?>> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (func == null) throw new ArgumentNullException(nameof(func));

            cancellationToken.ThrowIfCancellationRequested();

            if (DispatcherQueue.HasThreadAccess)
            {
                return await func.Invoke(cancellationToken);
            }
            else
            {
                var tcs = new TaskCompletionSource<T?>();

                DispatcherQueue.TryEnqueue(priority, async () =>
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

                return await tcs.Task;
            }
        }


        protected T? RunOnDispatcherQueueSynchronously<T>(Func<CancellationToken, Task<T?>> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (func == null) throw new ArgumentNullException(nameof(func));

            cancellationToken.ThrowIfCancellationRequested();

            bool hasThreadAccess = DispatcherQueue.HasThreadAccess;
            ExceptionDispatchInfo? eInfo = null;
            T? result = default;

            DispatcherQueue.TryEnqueue(priority, async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result = await func.Invoke(cancellationToken);
                }
                catch (Exception ex)
                {
                    eInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    if (hasThreadAccess)
                    {
                        DispatcherQueue.EnqueueEventLoopExit();
                    }
                    else
                    {
                        lock (locker)
                        {
                            Monitor.Pulse(locker);
                        }
                    }
                }
            });

            if (hasThreadAccess)
            {
                DispatcherQueue.RunEventLoop(DispatcherRunOptions.QuitOnlyLocalLoop, null);
            }
            else
            {
                lock (locker)
                {
                    Monitor.Wait(locker);
                }
            }
            eInfo?.Throw();

            return result;
        }

        protected void RunOnDispatcherQueueSynchronously(Func<CancellationToken, Task> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (func == null) throw new ArgumentNullException(nameof(func));

            cancellationToken.ThrowIfCancellationRequested();

            bool hasThreadAccess = DispatcherQueue.HasThreadAccess;
            ExceptionDispatchInfo? eInfo = null;

            DispatcherQueue.TryEnqueue(priority, async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await func.Invoke(cancellationToken);
                }
                catch (Exception ex)
                {
                    eInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    if (hasThreadAccess)
                    {
                        DispatcherQueue.EnqueueEventLoopExit();
                    }
                    else
                    {
                        lock (locker)
                        {
                            Monitor.Pulse(locker);
                        }
                    }
                }
            });

            if (hasThreadAccess)
            {
                DispatcherQueue.RunEventLoop(DispatcherRunOptions.QuitOnlyLocalLoop, null);
            }
            else
            {
                lock (locker)
                {
                    Monitor.Wait(locker);
                }
            }
            eInfo?.Throw();
        }

        protected void RunOnDispatcherQueueSynchronously(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            ThrowIfDisposed();
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (DispatcherQueue.HasThreadAccess)
            {
                action.Invoke();
            }
            else
            {
                ExceptionDispatchInfo? eInfo = null;

                DispatcherQueue.TryEnqueue(priority, () =>
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

                lock (locker)
                {
                    Monitor.Wait(locker);
                }

                eInfo?.Throw();
            }
        }

        protected T? RunOnDispatcherQueueSynchronously<T>(Func<T?> func, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            ThrowIfDisposed();
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (DispatcherQueue.HasThreadAccess)
            {
                return func.Invoke();
            }
            else
            {
                T? result = default;
                ExceptionDispatchInfo? eInfo = null;

                DispatcherQueue.TryEnqueue(priority, () =>
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

                lock (locker)
                {
                    Monitor.Wait(locker);
                }

                eInfo?.Throw();

                return result;
            }
        }

        protected void ThrowIfDisposed()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(WorkerQueueObject));
            }
        }

        private void ThrowAggregateException(Exception innerException)
        {
            throw new AggregateException(innerException);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private void DisposeCore(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                try
                {
                    Dispose(disposing);
                }
                finally
                {
                    // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                    // TODO: 将大型字段设置为 null
                    disposedValue = true;
                }
            }
        }

        ~WorkerQueueObject()
        {
            DisposeCore(disposing: false);
        }

        public void Dispose()
        {
            DisposeCore(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
