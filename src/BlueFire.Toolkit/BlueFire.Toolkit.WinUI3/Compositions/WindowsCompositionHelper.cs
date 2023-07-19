using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using WinRT;
using WinCompositor = Windows.UI.Composition.Compositor;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;
using WinDispatcherQueue = Windows.System.DispatcherQueue;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    public static class WindowsCompositionHelper
    {
        private static WinCompositor? compositor;
        private static WinDispatcherQueueController? dispatcherQueueController;
        private static object locker = new object();

        public static WinCompositor Compositor => EnsureCompositor();

        public static WinDispatcherQueue DispatcherQueue
        {
            get
            {
                EnsureCompositor();
                return dispatcherQueueController!.DispatcherQueue;
            }
        }

        private static WinCompositor EnsureCompositor()
        {
            if (compositor == null)
            {
                lock (locker)
                {
                    if (compositor == null)
                    {
                        var handle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);

                        // 在子线程创建Compositor
                        dispatcherQueueController = CreateDispatcherQueueController(false);
                        dispatcherQueueController.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.High, () =>
                        {
                            compositor = new WinCompositor();
                            handle.Set();
                        });

                        handle.WaitOne();
                    }
                }
            }

            return compositor!;
        }

        public static DesktopWindowTarget CreateDesktopWindowTarget(WindowId windowId, bool topMost)
        {
            if (windowId.Value == 0) throw new ArgumentNullException(nameof(windowId));

            var hWnd = new HWND((nint)windowId.Value);

            var interop = WindowsCompositionHelper.Compositor.As<Windows.Win32.System.WinRT.Composition.ICompositorDesktopInterop>();

            interop.CreateDesktopWindowTarget(hWnd, topMost, out var target);

            return target;
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
