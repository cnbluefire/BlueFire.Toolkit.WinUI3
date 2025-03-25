using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WindowBase
{
    internal abstract partial class WindowFeatureBase : IDisposable
    {
        private bool disposeValue;
        private nint windowHandle;

        public nint WindowHandle
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposeValue, this);
                return windowHandle;
            }
        }

        public void Initialize(nint windowHandle)
        {
            if (this.windowHandle == IntPtr.Zero)
            {
                this.windowHandle = windowHandle;
                InitializeCore(windowHandle);
            }
        }

        protected virtual void InitializeCore(nint windowHandle) { }

        public abstract void OnMessageReceived(WindowMessageReceivedEventArgs args);

        protected abstract void DisposeCore(nint windowHandle);

        public void Dispose()
        {
            if (!disposeValue)
            {
                var windowHandle = Interlocked.Exchange(ref this.windowHandle, 0);
                DisposeCore(windowHandle);
                disposeValue = true;
            }
        }
    }
}
