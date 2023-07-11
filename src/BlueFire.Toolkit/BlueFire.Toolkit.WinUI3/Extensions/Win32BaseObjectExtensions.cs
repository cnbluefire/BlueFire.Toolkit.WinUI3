using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    internal static class Win32BaseObjectExtensions
    {
        internal static void ThrowIfFalse(this BOOL value)
        {
            if (!value)
            {
                var lastError = Marshal.GetLastWin32Error();

                if (lastError != 0)
                {
                    throw new Win32Exception(lastError);
                }
            }
        }

        internal static HWND ToHWND(this WindowId windowId)
        {
            return new HWND((nint)windowId.Value);
        }

        internal static IntPtr ToPtr(this WindowId windowId)
        {
            return (nint)windowId.Value;
        }

        internal static WindowId ToWindowId(this HWND hWnd)
        {
            return ToWindowId(hWnd.Value);
        }

        internal static WindowId ToWindowId(nint hWnd)
        {
            return new WindowId(unchecked((ulong)hWnd));
        }
    }
}
