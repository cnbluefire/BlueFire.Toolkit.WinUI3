using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;
using ID3D11Device4 = Windows.Win32.Graphics.Direct3D11.ID3D11Device4;
using IDXGIFactory7 = Windows.Win32.Graphics.Dxgi.IDXGIFactory7;

namespace BlueFire.Toolkit.WinUI3.Graphics
{
    internal unsafe class Direct3DDeviceLostHelper : IDisposable
    {
        private static int globalId = 0;
        private static Dictionary<int, Direct3DDeviceLostHelper> instances = new Dictionary<int, Direct3DDeviceLostHelper>();

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
        private int id;

        internal unsafe Direct3DDeviceLostHelper(IDirect3DDevice direct3DDevice, bool forceSoftwareRenderer)
        {
            id = Interlocked.Increment(ref globalId);
            lock (instances)
            {
                instances[id] = this;
            }

            this.direct3DDevice = direct3DDevice;
            this.forceSoftwareRenderer = forceSoftwareRenderer;
            locker = new object();
            waitEventCallback = new PTP_WAIT_CALLBACK()
            {
                Func = &GlobalOnDeviceLost
            };
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

                using var device = GraphicsHelper.GetInterfacePtr<ID3D11Device4>(d3dDevice);
                if (!device.HasValue) return;

                threadPoolWait = PInvoke.CreateThreadpoolWait(waitEventCallback.Func, (void*)id, (Windows.Win32.System.Threading.TP_CALLBACK_ENVIRON_V3*)0);
                waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                var safeHandle = waitHandle.SafeWaitHandle;

                PInvoke.SetThreadpoolWait(new Windows.Win32.System.Threading.PTP_WAIT(threadPoolWait), safeHandle, null);
                device.Value.RegisterDeviceRemovedEvent(safeHandle, out cookie);

                if (!forceSoftwareRenderer)
                {
                    var dxgiFactory = GraphicsHelper.GetDXGIFactoryPtr<IDXGIFactory7>(d3dDevice);
                    if (!dxgiFactory.HasValue)
                    {
                        dxgiFactory.Value.RegisterAdaptersChangedEvent(safeHandle, out cookie2);
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
                        using var device = GraphicsHelper.GetInterfacePtr<ID3D11Device4>(d3dDevice);
                        if (device.HasValue)
                        {
                            device.Value.UnregisterDeviceRemoved(cookie);

                            if (cookie2 != 0)
                            {
                                GraphicsHelper.GetDXGIFactoryPtr<IDXGIFactory7>(d3dDevice).Value
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

                direct3DDevice = null!;

                disposedValue = true;

                lock (instances)
                {
                    instances.Remove(id);
                }
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

        private unsafe struct PTP_WAIT_CALLBACK
        {
            internal delegate* unmanaged[Stdcall]<global::Windows.Win32.System.Threading.PTP_CALLBACK_INSTANCE, void*, global::Windows.Win32.System.Threading.PTP_WAIT, uint, void> Func;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe void GlobalOnDeviceLost(
            Windows.Win32.System.Threading.PTP_CALLBACK_INSTANCE Instance,
            void* Context,
            Windows.Win32.System.Threading.PTP_WAIT Wait,
            uint WaitResult)
        {
            var id = (int)Context;
            lock (instances)
            {
                if (instances.TryGetValue(id, out var helper))
                {
                    helper.OnDeviceLost(Instance, Context, Wait, WaitResult);
                }
            }
        }
    }
}
