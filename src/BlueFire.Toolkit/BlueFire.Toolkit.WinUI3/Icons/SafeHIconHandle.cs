using Microsoft.UI;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HICON = Windows.Win32.UI.WindowsAndMessaging.HICON;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Icons
{
    public class SafeHIconHandle : SafeHandle
    {
        public SafeHIconHandle(IntPtr hIcon) : base(IntPtr.Zero, true)
        {
            this.handle = hIcon;
            IconId = Microsoft.UI.Win32Interop.GetIconIdFromIcon(hIcon);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public IconId IconId { get; }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                return PInvoke.DestroyIcon(new HICON(handle));
            }

            return false;
        }
    }
}
