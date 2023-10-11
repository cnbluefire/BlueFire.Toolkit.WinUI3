using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Win32.Foundation;
using HICON = Windows.Win32.UI.WindowsAndMessaging.HICON;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Icons
{
    internal abstract class ComposedIcon : IDisposable
    {
        private bool disposedValue;

        internal SafeHIconHandle GetIcon(int width, int height)
        {
            var hIcon = GetIconCore(new SizeInt32(width, height));
            if (hIcon != IntPtr.Zero)
            {
                return new SafeHIconHandle(PInvoke.CopyImage(new HANDLE(hIcon), Windows.Win32.UI.WindowsAndMessaging.GDI_IMAGE_TYPE.IMAGE_ICON, width, height, 0));
            }
            return new SafeHIconHandle(IntPtr.Zero);
        }

        internal IconId GetSharedIcon(int width, int height)
        {
            var hIcon = GetIconCore(new SizeInt32(width, height));
            return Win32Interop.GetIconIdFromIcon(hIcon);
        }

        protected internal abstract nint GetIconCore(SizeInt32 size);

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore(disposing);

                disposedValue = true;
            }
        }

        protected virtual void DisposeCore(bool disposing)
        {

        }

        ~ComposedIcon()
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
