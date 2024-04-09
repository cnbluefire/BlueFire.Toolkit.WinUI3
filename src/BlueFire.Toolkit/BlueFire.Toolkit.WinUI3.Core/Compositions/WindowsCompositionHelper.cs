using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI;
using System.Runtime.InteropServices;
using Windows.UI.Composition.Desktop;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinCompositor = Windows.UI.Composition.Compositor;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;
using WinDispatcherQueue = Windows.System.DispatcherQueue;
using System.Runtime.Versioning;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    /// <summary>
    /// Interop with Windows.UI.Composition.Compositor.
    /// </summary>
    public static class WindowsCompositionHelper
    {
        private static WinCompositor? compositor;
        private static WinDispatcherQueueController? dispatcherQueueController;
        private static object locker = new object();
        private static bool hasInteropCompositor = false;

        public static WinCompositor Compositor => EnsureCompositor();

        public static WinDispatcherQueue DispatcherQueue
        {
            get
            {
                EnsureCompositor();
                return dispatcherQueueController!.DispatcherQueue;
            }
        }

        public enum CompositorInitializeResult
        {
            Success,
            Failed,
            AlreadyInitialized,
        }

        /// <summary>
        /// Create a thumbnail visual for the window.
        /// </summary>
        /// <param name="hwndDestination">The handle to the window that will use the DWM thumbnail. Must be a top-level window.</param>
        /// <param name="hwndSource">The handle to the window to use as the thumbnail source. Must be a top-level window.</param>
        /// <param name="sourceClientAreaOnly">True to use only the thumbnail source's client area; otherwise, false.</param>
        /// <param name="hThumbnailId">A pointer to a handle that, when this function returns successfully, represents the registration of the DWM thumbnail.</param>
        /// <returns></returns>
        public static Windows.UI.Composition.Visual? CreateVisualFromHwnd(nint hwndDestination, nint hwndSource, bool sourceClientAreaOnly, out nint hThumbnailId)
        {
            hThumbnailId = 0;

            if (!hasInteropCompositor) return null;
            if (hwndDestination == 0 || hwndSource == 0) return null;

            return InteropCompositor.CreateVisualFromHwnd(Compositor, new HWND(hwndDestination), new HWND(hwndSource), sourceClientAreaOnly, out hThumbnailId);
        }

        private static WinCompositor EnsureCompositor()
        {
            if (compositor == null)
            {
                lock (locker)
                {
                    if (compositor == null)
                    {
                        var obj = new object();

                        // 在子线程创建Compositor
                        dispatcherQueueController = CreateDispatcherQueueController(false);
                        dispatcherQueueController.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.High, () =>
                        {
                            compositor = InteropCompositor.CreateCompositor();
                            hasInteropCompositor = true;

                            if (compositor == null)
                            {
                                hasInteropCompositor = false;
                                compositor = new WinCompositor();
                            }

                            lock (obj)
                            {
                                Monitor.PulseAll(obj);
                            }
                        });

                        lock (obj)
                        {
                            Monitor.Wait(obj);
                        }
                    }
                }
            }

            return compositor!;
        }

        internal static unsafe DesktopWindowTarget CreateDesktopWindowTarget(WindowId windowId, bool topMost)
        {
            if (windowId.Value == 0) throw new ArgumentNullException(nameof(windowId));

            var hWnd = new HWND((nint)windowId.Value);

            ComPtr<Windows.Win32.System.WinRT.Composition.ICompositorDesktopInterop> interop = default;
            nint pTarget = 0;

            try
            {
                ComObjectHelper.QueryInterface(Compositor, Windows.Win32.System.WinRT.Composition.ICompositorDesktopInterop.IID_Guid, out interop);

                ((delegate* unmanaged[Stdcall]<Windows.Win32.System.WinRT.Composition.ICompositorDesktopInterop*, Windows.Win32.Foundation.HWND, Windows.Win32.Foundation.BOOL, void**, Windows.Win32.Foundation.HRESULT>)(*(void***)(interop.AsPointer()))[3])(
                    interop.AsTypedPointer(),
                    hWnd,
                    topMost,
                    (void**)(&pTarget));

                return DesktopWindowTarget.FromAbi(pTarget);
            }
            finally
            {
                ComPtr<IUnknown>.Attach(pTarget).Release();
                interop.Release();
            }
        }

        private static WinDispatcherQueueController CreateDispatcherQueueController(bool currentThread)
        {
            var options = new Windows.Win32.System.WinRT.DispatcherQueueOptions()
            {
                apartmentType = Windows.Win32.System.WinRT.DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA,
                threadType = currentThread ? Windows.Win32.System.WinRT.DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT :
                    Windows.Win32.System.WinRT.DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_DEDICATED,
                dwSize = (uint)Marshal.SizeOf<Windows.Win32.System.WinRT.DispatcherQueueOptions>()
            };
            Windows.Win32.PInvoke.CreateDispatcherQueueController(options, out var result).ThrowOnFailure();
            return result;
        }
    }
}
