using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class XamlExtensions
    {
        /// <summary>
        /// Get the window that hosts the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowId GetWindowId(this UIElement element)
        {
            if (element is WindowEx windowEx)
            {
                return windowEx.AppWindow.Id;
            }
            else if (element?.XamlRoot != null)
            {
                return element.XamlRoot.ContentIslandEnvironment.AppWindowId;
            }
            return default;
        }

        public static Window? GetWindow(this UIElement element)
        {
            if (element is null) return null;

            if (element is WindowEx windowEx)
            {
                return windowEx.XamlWindow;
            }

            var windowId = GetWindowId(element);
            if (windowId.Value == 0) return null;

            var hWnd = Win32Interop.GetWindowFromWindowId(windowId);

            return Application.Current.GetWindows()
                .FirstOrDefault(c => c.GetWindowHandle() == hWnd);
        }

        public static T? GetWindow<T>(this UIElement element) where T : Window
        {
            return GetWindow(element) as T;
        }

        /// <summary>
        /// Try get the manager of the window hosting the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowManager? TryGetWindowManager(this UIElement element)
        {
            return WindowManager.Get(GetWindowId(element));
        }

        /// <summary>
        /// Try get the <see cref="BlueFire.Toolkit.WinUI3.WindowEx"/> that hosts the UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static WindowEx? TryGetWindowEx(this UIElement element)
        {
            return TryGetWindowManager(element)?.WindowExInternal;
        }

        public static T? FindName<T>(this FrameworkElement frameworkElement, string name)
        {
            return (T)frameworkElement.FindName(name);
        }

        public static T? GetTemplateChild<T>(this Control control, string childName) where T : DependencyObject
        {
            return Control_GetTemplateChild(control, childName) as T;

#if NET8_0_OR_GREATER
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetTemplateChild")]
            static extern DependencyObject Control_GetTemplateChild(Control _control, string childName);
#else
            static DependencyObject Control_GetTemplateChild(Control _control, string childName)
            {
                const string IID_IControlProtected = "5c750339-1789-5095-a214-dd9d0564e579";

                using var objRef = ((IWinRTObject)_control).NativeObject.As(new Guid(IID_IControlProtected));

                return GetTemplateChild(objRef, childName);

                unsafe static global::Microsoft.UI.Xaml.DependencyObject GetTemplateChild(IObjectReference _obj, string _childName)
                {
                    IntPtr thisPtr = _obj.ThisPtr;
                    IntPtr intPtr = default(IntPtr);
                    try
                    {
                        MarshalString.Pinnable p = new MarshalString.Pinnable(_childName);
                        fixed (char* ptr = p)
                        {
                            _ = ptr;
                            ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)(*(IntPtr*)((nint)(*(IntPtr*)(void*)thisPtr) + (nint)8 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>))))(thisPtr, MarshalString.GetAbi(ref p), &intPtr));
                            return ABI.Microsoft.UI.Xaml.DependencyObject.FromAbi(intPtr);
                        }
                    }
                    finally
                    {
                        ABI.Microsoft.UI.Xaml.DependencyObject.DisposeAbi(intPtr);
                    }
                }
            }
#endif
        }

        public unsafe static void ScrollIntoView(this ListViewBase listView, object item, ScrollIntoViewAlignment alignment, bool disableAnimation)
        {
            // https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6-stable/src/dxaml/xcp/dxaml/idl/winrt/main/microsoft.ui.xaml.private.idl#L573

            const string IID_IListViewBasePrivate = "31650b1a-c46b-53f3-8ff3-82cbe8708740";

            ArgumentNullException.ThrowIfNull(listView);

            var thisPtr = ((IWinRTObject)listView).NativeObject.ThisPtr;

            nint ptr = 0;
            try
            {
                var hr = Marshal.QueryInterface(thisPtr, new Guid(IID_IListViewBasePrivate), out ptr);
                if (hr == 0)
                {
                    ObjectReferenceValue value = default(ObjectReferenceValue);
                    byte b = disableAnimation ? (byte)1 : (byte)0;
                    try
                    {
                        value = MarshalInspectable<object>.CreateMarshaler2(item);
                        ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, ScrollIntoViewAlignment, byte, int>)(*(void***)ptr)[6])(ptr, MarshalInspectable<object>.GetAbi(value), alignment, b));
                    }
                    finally
                    {
                        MarshalInspectable<object>.DisposeMarshaler(value);
                    }
                }
            }
            finally
            {
                if (ptr != 0) Marshal.Release(ptr);
            }
        }
    }
}
