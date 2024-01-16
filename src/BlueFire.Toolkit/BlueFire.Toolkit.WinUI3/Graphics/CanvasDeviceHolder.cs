using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using WinDispatcherQueue = Windows.System.DispatcherQueue;
using ID3D11Device4 = Windows.Win32.Graphics.Direct3D11.ID3D11Device4;
using IDXGIFactory1 = Windows.Win32.Graphics.Dxgi.IDXGIFactory1;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    internal class CanvasDeviceHolder : IDisposable
    {
        private CanvasDevice canvasDevice;
        private Direct3DDeviceLostHelper? deviceLostHelper;
        private DispatcherQueue? dispatcherQueue;
        private WinDispatcherQueue? winDispatcherQueue;
        private bool disposedValue;

        public CanvasDeviceHolder(CanvasDevice canvasDevice, WinDispatcherQueue? dispatcherQueue) : this(canvasDevice)
        {
            winDispatcherQueue = dispatcherQueue;
        }

        public CanvasDeviceHolder(CanvasDevice canvasDevice, DispatcherQueue? dispatcherQueue) : this(canvasDevice)
        {
            this.dispatcherQueue = dispatcherQueue;
        }

        public CanvasDeviceHolder(CanvasDevice device)
        {
            canvasDevice = device;
            deviceLostHelper = new Direct3DDeviceLostHelper(device, device.ForceSoftwareRenderer);
            deviceLostHelper.DeviceLost += DeviceLostHelper_DeviceLost;
        }

        public CanvasDevice CanvasDevice => canvasDevice;

        private bool RunOrEnqueue(Action action)
        {
            if (dispatcherQueue != null)
            {
                if (dispatcherQueue.HasThreadAccess)
                {
                    action.Invoke();
                }
                else
                {
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => action.Invoke());
                }
                return true;
            }
            else if (winDispatcherQueue != null)
            {
                if (winDispatcherQueue.HasThreadAccess)
                {
                    action.Invoke();
                }
                else
                {
                    winDispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.High, () => action.Invoke());
                }
                return true;
            }

            return false;
        }

        private void DeviceLostHelper_DeviceLost(Direct3DDeviceLostHelper sender, object? args)
        {
            var deviceLostHelper = this.deviceLostHelper;

            if (RunOrEnqueue(() =>
            {
                deviceLostHelper?.Dispose();
            }))
            {
                this.deviceLostHelper = null;
            }

            DeviceLost?.Invoke(this, EventArgs.Empty);
        }

        internal event DeviceLostEventHandler? DeviceLost;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (deviceLostHelper != null)
                {
                    RunOrEnqueue(() =>
                    {
                        deviceLostHelper?.Dispose();
                        deviceLostHelper = null;
                    });
                }
                dispatcherQueue = null;
                winDispatcherQueue = null;
                canvasDevice = null!;

                disposedValue = true;
            }
        }

        ~CanvasDeviceHolder()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal delegate void DeviceLostEventHandler(CanvasDeviceHolder? deviceHolder, EventArgs args);
    }
}
