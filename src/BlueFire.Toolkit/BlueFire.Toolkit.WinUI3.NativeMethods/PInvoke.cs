using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        private static bool? isWindows11OrGreater;
        private static bool? isWindows10_18362OrGreater;
        private static bool? isWindows10_17134OrGreater;

        internal static bool IsWindows11OrGreater() =>
            isWindows11OrGreater ??
            (isWindows11OrGreater = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0)).Value;

        internal static bool IsWindows10_18362OrGreater() =>
            isWindows10_18362OrGreater ??
            (isWindows10_18362OrGreater = Environment.OSVersion.Version >= new Version(10, 0, 18362, 0)).Value;

        internal static bool IsWindows10_17134OrGreater() =>
            isWindows10_17134OrGreater ??
            (isWindows10_17134OrGreater = Environment.OSVersion.Version >= new Version(10, 0, 17134, 0)).Value;

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

        internal static unsafe HWND[]? EnumThreadWindows(Func<HWND, nint, bool> predicate)
        {
            var list = new List<HWND>();
            var gcHandle = GCHandle.Alloc(new Tuple<Func<HWND, nint, bool>, List<HWND>>(predicate, list));

            try
            {
                EnumThreadWindows(PInvoke.GetCurrentThreadId(), &EnumThreadWindowsProc, new LPARAM((nint)gcHandle));
                return list.Count != 0 ? list.Distinct().ToArray() : Array.Empty<HWND>();
            }
            finally
            {
                gcHandle.Free();
            }

            [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
            static byte EnumThreadWindowsProc(nint _hWnd, nint _lParam)
            {
                var _gcHandle = (GCHandle)_lParam;
                if (_gcHandle.IsAllocated && _gcHandle.Target is Tuple<Func<HWND, nint, bool>, List<HWND>> _tuple)
                {
                    var _predicate = _tuple.Item1;
                    var _list = _tuple.Item2;
                    try
                    {
                        if (_predicate((HWND)_hWnd, _lParam)) _list.Add((HWND)_hWnd);
                    }
                    catch { }

                }
                return 1;
            }
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
        internal static extern unsafe bool EnumThreadWindows(
            [In] uint dwThreadId,
            [In] delegate* unmanaged[Stdcall]<nint, nint, byte> lpfn, [In] nint lParam);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentprocesstoken
        /// </summary>
        public static HANDLE GetCurrentProcessToken() => new(-4);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadtoken
        /// </summary>
        public static HANDLE GetCurrentThreadToken() => new(-5);

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentthreadeffectivetoken
        /// </summary>
        public static HANDLE GetCurrentThreadEffectiveToken() => new(-6);
    }
}
