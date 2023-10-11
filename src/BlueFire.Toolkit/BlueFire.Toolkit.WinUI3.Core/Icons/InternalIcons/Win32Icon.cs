using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using HICON = Windows.Win32.UI.WindowsAndMessaging.HICON;
using PInvoke = Windows.Win32.PInvoke;
using ICONINFO = Windows.Win32.UI.WindowsAndMessaging.ICONINFO;

namespace BlueFire.Toolkit.WinUI3.Icons.InternalIcons
{
    internal class Win32Icon : ComposedIcon
    {
        private readonly string fileName;
        private Dictionary<SizeInt32, SafeHIconHandle> icons = new Dictionary<SizeInt32, SafeHIconHandle>();
        private bool notSupport;

        internal Win32Icon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            this.fileName = fileName;
        }

        protected internal override nint GetIconCore(SizeInt32 size)
        {
            if (notSupport) return 0;

            if (size.Width == 0 || size.Height == 0) return 0;

            if (icons.TryGetValue(size, out var icon)) return icon.DangerousGetHandle();

            lock (icons)
            {
                var icon2 = GetHIconsFromFile(fileName, size.Width, size.Height);

                if (icon2 == IntPtr.Zero)
                {
                    notSupport = true;
                }
                else
                {
                    icons[size] = new SafeHIconHandle(icon2);
                }

                return icon2;
            }
        }

        protected override void DisposeCore(bool disposing)
        {
            base.DisposeCore(disposing);

            lock (icons)
            {
                foreach (var icon in icons)
                {
                    try { icon.Value.Dispose(); } catch { }
                }
            }
        }

        internal unsafe static nint GetHIconsFromFile(string fileName, int width, int height)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                fixed (char* lpszFileLocal = fileName)
                {
                    var lpszFileLocal2 = new Windows.Win32.Foundation.PCWSTR(lpszFileLocal);
                    var iconCount = PInvoke.ExtractIcon(default, lpszFileLocal2, unchecked((uint)-1)).Value.ToInt64();

                    if (iconCount > 0)
                    {
                        var phicons = stackalloc HICON[1];
                        uint iconid = 0;

                        var ret = PInvoke.PrivateExtractIcons(lpszFileLocal2, 0, width, height, phicons, &iconid, 1, (uint)Windows.Win32.UI.WindowsAndMessaging.IMAGE_FLAGS.LR_LOADFROMFILE);
                        if (ret > 0)
                        {
                            if (!phicons->IsNull) return phicons->Value;
                        }
                    }
                }
            }
            return 0;
        }


        internal static unsafe SizeInt32 GetHIconSize(nint hIcon)
        {
            ICONINFO iconInfo = default;
            if (PInvoke.GetIconInfo(new HICON(hIcon), &iconInfo))
            {
                try
                {
                    Windows.Win32.Graphics.Gdi.BITMAP bm = default;
                    if (PInvoke.GetObject(iconInfo.hbmMask, Marshal.SizeOf<Windows.Win32.Graphics.Gdi.BITMAP>(), &bm) != 0)
                    {
                        return new Windows.Graphics.SizeInt32(
                            bm.bmWidth,
                            iconInfo.hbmColor.IsNull ? bm.bmHeight / 2 : bm.bmWidth);
                    }
                }
                finally
                {
                    if (!iconInfo.hbmMask.IsNull) PInvoke.DeleteObject(iconInfo.hbmMask);
                    if (!iconInfo.hbmColor.IsNull) PInvoke.DeleteObject(iconInfo.hbmColor);
                }
            }

            return default;
        }
    }
}
