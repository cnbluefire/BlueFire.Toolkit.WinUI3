using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace BlueFire.Toolkit.WinUI3.Core.Dispatching
{
    public class WindowMessageListener : WorkerQueueObject
    {
        private const string BroadcastMessageWindowName = "BlueFire.Toolkit.BroadcastMessageListener";

        private MessageWindow? messageWindow;
        private WindowMessageReceivedEventHandler? messageReceived;
        private object eventLocker = new object();

        public WindowMessageListener(nint parentWindow, string? windowName = null) : base(ApartmentState.STA)
        {
            RunOnDispatcherQueueSynchronously(() =>
            {
                messageWindow = new MessageWindow(parentWindow, windowName);
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
        }

        protected nint WindowHandle
        {
            get
            {
                ThrowIfDisposed();
                return messageWindow!.WindowHandle;
            }
        }

        public event WindowMessageReceivedEventHandler? MessageReceived
        {
            add
            {
                ThrowIfDisposed();
                lock (eventLocker)
                {
                    messageReceived += value;
                    if (messageReceived != null)
                    {
                        messageWindow!.MessageReceived += MessageWindow_MessageReceived;
                    }
                }
            }
            remove
            {
                ThrowIfDisposed();
                lock (eventLocker)
                {
                    messageReceived -= value;
                    if (messageReceived == null)
                    {
                        messageWindow!.MessageReceived -= MessageWindow_MessageReceived;
                    }
                }
            }
        }

        private void MessageWindow_MessageReceived(object sender, WindowMessageReceivedEventArgs e)
        {
            var handler = messageReceived;
            if (handler != null)
            {
                handler.Invoke(this, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            RunOnDispatcherQueueSynchronously(() => messageWindow?.Dispose());
            base.Dispose(disposing);
        }

        private static WindowMessageListener? broadcastMessageListener;
        private static object staticLocker = new object();

        internal static WindowMessageListener BroadcastMessageListener
        {
            get
            {
                if (broadcastMessageListener == null)
                {
                    lock (staticLocker)
                    {
                        if (broadcastMessageListener == null)
                        {
                            broadcastMessageListener = new WindowMessageListener(0, BroadcastMessageWindowName);
                        }
                    }
                }

                return broadcastMessageListener;
            }
        }
    }
}
