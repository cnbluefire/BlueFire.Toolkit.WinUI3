using BlueFire.Toolkit.WinUI3.Graphics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    internal class CompositionGraphicsDeviceHolder : DependencyObject, IDisposable
    {
        #region Statics

        private static CompositionGraphicsDeviceHolder? globalDeviceHolder;
        private static object globalDeviceHolderLocker = new object();

        internal static CompositionGraphicsDeviceHolder GlobalDeviceHolder
        {
            get
            {
                if (globalDeviceHolder == null)
                {
                    lock (globalDeviceHolderLocker)
                    {
                        if (globalDeviceHolder == null)
                        {
                            globalDeviceHolder = new CompositionGraphicsDeviceHolder(
                                CompositionTarget.GetCompositorForCurrentThread(),
                                false);
                        }
                    }
                }

                return globalDeviceHolder;
            }
        }

        #endregion Statics

        private bool disposedValue;

        private CompositionGraphicsDevice compositionGraphicsDevice;
        private CanvasDevice? canvasDevice;
        private CanvasDeviceHolder? deviceHolder;
        private SemaphoreSlim locker;
        private bool isLocked;
        private CanvasDevice? lostDevice;

        public CompositionGraphicsDeviceHolder(Compositor compositor, bool forceSoftwareRenderer)
        {
            locker = new SemaphoreSlim(1, 1);

            Compositor = compositor;
            ForceSoftwareRenderer = forceSoftwareRenderer;

            CreateCanvasDevice();

            compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, canvasDevice);
        }

        private void DeviceHolder_DeviceLost(CanvasDeviceHolder? deviceHolder, EventArgs args)
        {
            lostDevice = canvasDevice;
            canvasDevice = null;

            if (deviceHolder != null)
            {
                deviceHolder.DeviceLost -= DeviceHolder_DeviceLost;
            }

            if (DispatcherQueue.HasThreadAccess)
            {
                ResetDevice();
            }
            else
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => ResetDevice());
            }
        }

        public Compositor Compositor { get; }

        public CompositionGraphicsDevice CompositionGraphicsDevice => compositionGraphicsDevice;

        public CanvasDevice? CanvasDevice => canvasDevice;

        public bool ForceSoftwareRenderer { get; }

        internal bool IsLocked => isLocked;

        public bool DeviceRecreating => lostDevice != null;

        [MemberNotNull(nameof(canvasDevice), nameof(deviceHolder))]
        private void CreateCanvasDevice()
        {
            using (Lock())
            {
                deviceHolder?.Dispose();
                lostDevice?.Dispose();
                lostDevice = null;

                canvasDevice = new CanvasDevice(ForceSoftwareRenderer);
                deviceHolder = new CanvasDeviceHolder(canvasDevice, DispatcherQueue);
                deviceHolder.DeviceLost += DeviceHolder_DeviceLost;
            }
        }

        private void ResetDevice()
        {
            lock (compositionGraphicsDevice)
            {
                if (disposedValue) return;
                CreateCanvasDevice();
                compositionGraphicsDevice.Trim();
                CanvasComposition.SetCanvasDevice(compositionGraphicsDevice, canvasDevice);
            }
        }

        public IDisposable Lock()
        {
            return new DeviceLocker(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                lock (compositionGraphicsDevice)
                {
                    if (deviceHolder != null)
                    {
                        deviceHolder.DeviceLost -= DeviceHolder_DeviceLost;
                        deviceHolder.Dispose();
                        deviceHolder = null!;
                    }
                    canvasDevice?.Dispose();
                    compositionGraphicsDevice?.Dispose();
                }

                disposedValue = true;
            }
        }

        ~CompositionGraphicsDeviceHolder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class DeviceLocker : IDisposable
        {
            private CompositionGraphicsDeviceHolder holder;
            private SemaphoreSlim locker;

            public DeviceLocker(CompositionGraphicsDeviceHolder holder)
            {
                this.holder = holder;
                locker = holder.locker;
#if DEBUG
                Debug.Assert(!holder.isLocked);
#endif
                holder.isLocked = true;
                locker.Wait();
            }

            public void Dispose()
            {
                if (holder != null)
                {
                    lock (holder)
                    {
                        locker?.Release();
                        locker = null!;

                        holder.isLocked = false;
                        holder = null!;
                    }
                }
            }
        }
    }
}
