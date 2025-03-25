using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WindowBase
{
    public partial class WindowMessageListener : IDisposable
    {
        private bool disposedValue;

        private nint windowHandle;
        private nuint subClassId;
        private GCHandle gcHandle;

        private Lock featuresLocker = new Lock();
        private List<WindowFeatureBase>? features;

        private WindowMessageListener() { }

        public unsafe static WindowMessageListener? TryCreate(nint windowHandle)
        {
            if (windowHandle == 0 || !PInvoke.IsWindow((HWND)windowHandle)) return null;

            var windowThreadId = PInvoke.GetWindowThreadProcessId((HWND)windowHandle);
            var curThreadId = PInvoke.GetCurrentThreadId();
            if (windowThreadId != curThreadId) return null;

            var id = subClassIdTable.GetOrAdd(windowHandle, static k => new SubClassId());

            lock (id.Lock)
            {
                var listener = TryCreateCore(windowHandle, id.Increment());
                return listener;
            }
        }

        private unsafe static WindowMessageListener? TryCreateCore(nint windowHandle, nuint subClassId)
        {
            var hWnd = (HWND)windowHandle;
            nuint tmp = 0;
            if (GetWindowSubclass(hWnd, &SubWindowProc, subClassId, &tmp)) return null;

            var listener = new WindowMessageListener();
            var gcHandle = GCHandle.Alloc(listener, GCHandleType.Weak);
            if (PInvoke.SetWindowSubclass(hWnd, &SubWindowProc, subClassId, unchecked((nuint)(nint)gcHandle)))
            {
                listener.windowHandle = windowHandle;
                listener.subClassId = subClassId;
                return listener;
            }
            if (gcHandle.IsAllocated) gcHandle.Free();
            GC.KeepAlive(listener);
            return null;
        }

        public event WindowMessageReceivedEventHandler? MessageReceived;

        private void OnMessageReceived(WindowMessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }

        internal void AddFeature(WindowFeatureBase feature)
        {
            if (feature is null) return;
            if (windowHandle == 0) return;

            lock (featuresLocker)
            {
                feature.Initialize(windowHandle);
                if (features == null) features = [feature];
                else features.Add(feature);
            }
        }

        internal void RemoveFeature(WindowFeatureBase feature)
        {
            if (feature is null) return;

            lock (featuresLocker)
            {
                if (features?.Remove(feature) is true)
                {
                    feature.Dispose();
                }
            }
        }

        internal T? GetFeature<T>() where T : WindowFeatureBase
        {
            lock (featuresLocker)
            {
                return features?.OfType<T>().FirstOrDefault();
            }
        }

        internal void OnFeaturesMessageReceived(WindowMessageReceivedEventArgs args)
        {
            lock (featuresLocker)
            {
                if (features != null)
                {
                    for (int i = 0; i < features.Count; i++)
                    {
                        features[i].OnMessageReceived(args);
                    }
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe LRESULT SubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            var gcHandle = unchecked((GCHandle)(nint)(dwRefData));

            if (uMsg == PInvoke.WM_NCDESTROY || (uMsg != 0 && uMsg == removeListenerMessage))
            {
                PInvoke.RemoveWindowSubclass(hWnd, &SubWindowProc, uIdSubclass);
                if (gcHandle.IsAllocated) gcHandle.Free();
                return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
            }

            if (gcHandle.IsAllocated && gcHandle.Target is WindowMessageListener target)
            {
                var args = new WindowMessageReceivedEventArgs();

                args.WindowHandle = hWnd.Value;
                args.MessageId = uMsg;
                args.WParam = wParam.Value;
                args.LParam = lParam.Value;

                target.OnMessageReceived(args);
                if (args.Handled)
                {
                    return (LRESULT)args.LResult;
                }

                if (target.subClassId != uIdSubclass)
                {
                    return (LRESULT)0;
                }
            }
            else
            {
                RemoveListener(hWnd.Value, uIdSubclass);
            }
            return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private unsafe static void RemoveListener(nint windowHandle, nuint subClassId)
        {
            var hWnd = (HWND)windowHandle;
            if (!PInvoke.RemoveWindowSubclass(hWnd, &SubWindowProc, subClassId)
                && PInvoke.IsWindow(hWnd))
            {
                PInvoke.PostMessage(hWnd, RemoveListenerMessage, (WPARAM)0, (LPARAM)0);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                RemoveListener(windowHandle, subClassId);
                windowHandle = 0;
                subClassId = 0;
                if (gcHandle.IsAllocated)
                {
                    gcHandle.Free();
                    gcHandle = default;
                }

                disposedValue = true;
            }
        }

        ~WindowMessageListener()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static ConcurrentDictionary<nint, SubClassId> subClassIdTable = new ConcurrentDictionary<nint, SubClassId>();

        private class SubClassId
        {
            private uint id;
            public uint Id => id;
            public Lock Lock { get; } = new Lock();

            public uint Increment()
            {
                return Interlocked.Increment(ref id);
            }
        }

        private static Lock removeListenerMessageLock = new Lock();
        private static uint removeListenerMessage;

        private static uint RemoveListenerMessage
        {
            get
            {
                if (removeListenerMessage == 0)
                {
                    lock (removeListenerMessageLock)
                    {
                        if (removeListenerMessage == 0)
                        {
                            const string RemoveListenerMessageName = "BLUEFIRE_TOOLKIT_WINDOWMESSAGE_REMOVE";
                            unsafe
                            {
                                fixed (char* ptr = RemoveListenerMessageName)
                                {
                                    removeListenerMessage = PInvoke.RegisterWindowMessage(ptr);
                                }
                            }
                        }
                    }
                }
                return removeListenerMessage;
            }
        }

        private static nint GetWindowSubclassPtr = -1;

        private static unsafe bool GetWindowSubclass(
            HWND hWnd,
            delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, nuint, nuint, LRESULT> pfnSubclass,
            nuint uIdSubclass,
            [Optional] nuint* pdwRefData)
        {
            if (GetWindowSubclassPtr == -1)
            {
                var handle = NativeLibrary.Load("COMCTL32.dll");
                if (!NativeLibrary.TryGetExport(handle, "GetWindowSubclass", out GetWindowSubclassPtr))
                {
                    GetWindowSubclassPtr = 0;
                }
            }
            if (GetWindowSubclassPtr == 0) return false;
            return ((delegate* unmanaged[Stdcall]<HWND, nint, nuint, nuint*, BOOL>)GetWindowSubclassPtr)(
                hWnd,
                (nint)pfnSubclass,
                uIdSubclass,
                pdwRefData);
        }
    }
}
