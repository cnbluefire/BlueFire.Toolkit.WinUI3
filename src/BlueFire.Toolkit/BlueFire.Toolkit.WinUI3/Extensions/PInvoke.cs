using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private static bool? isWindows10OrGreater;

        internal static bool IsWindows10OrGreater() =>
            isWindows10OrGreater ??
            (isWindows10OrGreater = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0)).Value;

        internal static nint SetWindowLongAuto(HWND hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }
            else
            {
                return SetWindowLong(hWnd, nIndex, unchecked((int)dwNewLong));
            }
        }

        internal static nint GetWindowLongAuto(HWND hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr(hWnd, nIndex);
            }
            else
            {
                return GetWindowLong(hWnd, nIndex);
            }
        }

        private static nint SetWindowLongPtr(nint hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
        {
            return 0;
        }

        private static nint GetWindowLongPtr(nint hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex)
        {
            return 0;
        }

    }
}
