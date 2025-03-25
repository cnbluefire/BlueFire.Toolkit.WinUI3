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
    public partial class WorkerQueueObject : IDisposable
    {
        private bool disposedValue;
        private readonly ApartmentState apartmentState;

        private Thread? thread;
        private DispatcherQueueController? dispatcherQueueController;
        private DispatcherQueue? dispatcherQueue;
        private Lock locker = new Lock();

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

                            lock (waitEvent)
                            {
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
