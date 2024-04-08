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

        internal static HWND[]? EnumThreadWindows(Func<HWND, nint, bool> predicate, nint lParam)
        {
            var list = new List<HWND>();
            var handler = new WNDENUMPROC((_hWnd, _lParam) =>
            {
                try
                {
                    if (predicate((HWND)_hWnd, _lParam)) list.Add((HWND)_hWnd);
                }
                catch { }

                return true;
            });

            EnumThreadWindows(PInvoke.GetCurrentThreadId(), handler, new LPARAM(lParam));
            return list.Count != 0 ? list.Distinct().ToArray() : Array.Empty<HWND>();
        }

        internal static ushort LOWORD(uint value)
        {
            return (ushort)(value & 0xFFFF);
        }

        internal static ushort HIWORD(uint value)
        {
            return (ushort)(value >> 16);
        }

        internal static byte LOWBYTE(ushort value)
        {
            return (byte)(value & 0xFF);
        }

        internal static byte HIGHBYTE(ushort value)
        {
            return (byte)(value >> 8);
        }

        private static nint SetWindowLongPtr(nint hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
        {
            return 0;
        }

        private static nint GetWindowLongPtr(nint hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex)
        {
            return 0;
        }

        [DllImport("USER32.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern bool EnumThreadWindows([In] uint dwThreadId, [In] WNDENUMPROC lpfn, [In] nint lParam);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool WNDENUMPROC([In] nint param0, [In] nint param1);
    }
}
