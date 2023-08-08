using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using PTP_WAIT_CALLBACK = Windows.Win32.System.Threading.PTP_WAIT_CALLBACK;
using ID3D11Device4 = Windows.Win32.Graphics.Direct3D11.ID3D11Device4;
using IDXGIFactory7 = Windows.Win32.Graphics.Dxgi.IDXGIFactory7;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    public unsafe class Direct3DDeviceLostHelper : IDisposable
    {
        private bool disposedValue;

        private IDirect3DDevice direct3DDevice;
        private object locker;
        private PTP_WAIT_CALLBACK waitEventCallback;
        private nint threadPoolWait;
        private EventWaitHandle? waitHandle;
        private uint cookie;
        private uint cookie2;
        private bool deviceLostRaised;
        private bool forceSoftwareRenderer;

        internal Direct3DDeviceLostHelper(IDirect3DDevice direct3DDevice, bool forceSoftwareRenderer)
        {
            this.direct3DDevice = direct3DDevice;
            this.forceSoftwareRenderer = forceSoftwareRenderer;
            locker = new object();
            waitEventCallback = new PTP_WAIT_CALLBACK(OnDeviceLost);
            WatchDevice();
        }

        private void WatchDevice()
        {
            if (disposedValue) return;

            lock (locker)
            {
                if (waitHandle != null) return;

                var d3dDevice = direct3DDevice;
                if (d3dDevice == null) return;

                var device = GraphicsHelper.GetInterface<ID3D11Device4>(d3dDevice);
                if (device == null) return;

                threadPoolWait = PInvoke.CreateThreadpoolWait(waitEventCallback, (void*)0, default);
                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                var safeHandle = waitHandle.SafeWaitHandle;
                var handle = new HANDLE(safeHandle.DangerousGetHandle());

                PInvoke.SetThreadpoolWait(new Windows.Win32.System.Threading.PTP_WAIT(threadPoolWait), safeHandle, null);
                device.RegisterDeviceRemovedEvent(handle, out cookie);

                if (!forceSoftwareRenderer)
                {
                    var dxgiFactory = GraphicsHelper.GetDXGIFactory<IDXGIFactory7>(d3dDevice);
                    if (dxgiFactory != null)
                    {
                        dxgiFactory.RegisterAdaptersChangedEvent(handle, out cookie2);
                    }
                }
            }
        }

        private void StopWatchDevice()
        {
            lock (locker)
            {
                if (waitHandle != null)
                {
                    PInvoke.CloseThreadpoolWait(new Windows.Win32.System.Threading.PTP_WAIT(threadPoolWait));

                    var d3dDevice = direct3DDevice;
                    if (d3dDevice != null)
                    {
                        var device = GraphicsHelper.GetInterface<ID3D11Device4>(d3dDevice);
                        if (device != null)
                        {
                            device.UnregisterDeviceRemoved(cookie);

                            if (cookie2 != 0)
                            {
                                GraphicsHelper.GetDXGIFactory<IDXGIFactory7>(d3dDevice)?
                                    .UnregisterAdaptersChangedEvent(cookie2);
                            }
                        }
                    }

                    threadPoolWait = 0;
                    cookie = 0;
                    waitHandle.Dispose();
                }
            }
        }

        private unsafe void OnDeviceLost(
            Windows.Win32.System.Threading.PTP_CALLBACK_INSTANCE Instance,
            void* Context,
            Windows.Win32.System.Threading.PTP_WAIT Wait,
            uint WaitResult)
        {
            bool flag = false;

            lock (locker)
            {
                if (deviceLostRaised) return;

                flag = true;
                deviceLostRaised = true;
            }

            if (flag) DeviceLost?.Invoke(this, null);
        }


        private delegate void NativeDeviceLostEventHandler(nint Instance, nint Context, nint Wait, uint WaitResult);

        public event TypedEventHandler<Direct3DDeviceLostHelper, object?>? DeviceLost;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                StopWatchDevice();

                waitEventCallback = null!;
                direct3DDevice = null!;

                disposedValue = true;
            }
        }

        ~Direct3DDeviceLostHelper()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
