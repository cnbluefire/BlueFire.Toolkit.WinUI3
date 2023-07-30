﻿using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
