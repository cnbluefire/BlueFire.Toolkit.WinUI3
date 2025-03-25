using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public static class ScrollViewerExtensions
    {
        public static bool IsInDirectManipulation(this ScrollViewer scrollViewer)
        {
            return IScrollViewerPrivate.get_IsInDirectManipulation(scrollViewer);
        }

        #region ArePointerWheelEventsIgnored

        public static bool GetArePointerWheelEventsIgnored(this ScrollViewer obj)
        {
            return (bool)obj.GetValue(ArePointerWheelEventsIgnoredProperty);
        }

        public static void SetArePointerWheelEventsIgnored(this ScrollViewer obj, bool value)
        {
            obj.SetValue(ArePointerWheelEventsIgnoredProperty, value);
        }

        public static readonly DependencyProperty ArePointerWheelEventsIgnoredProperty =
            DependencyProperty.RegisterAttached("ArePointerWheelEventsIgnored", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, (s, a) =>
            {
                if (s is ScrollViewer sender)
                {
                    IScrollViewerPrivate.set_ArePointerWheelEventsIgnored(sender, a.NewValue is true);
                }
            }));

        #endregion ArePointerWheelEventsIgnored


        #region IsRequestBringIntoViewIgnored

        public static bool GetIsRequestBringIntoViewIgnored(this ScrollViewer obj)
        {
            return (bool)obj.GetValue(IsRequestBringIntoViewIgnoredProperty);
        }

        public static void SetIsRequestBringIntoViewIgnored(this ScrollViewer obj, bool value)
        {
            obj.SetValue(IsRequestBringIntoViewIgnoredProperty, value);
        }

        public static readonly DependencyProperty IsRequestBringIntoViewIgnoredProperty =
            DependencyProperty.RegisterAttached("IsRequestBringIntoViewIgnored", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(false, (s, a) =>
            {
                if (s is ScrollViewer sender)
                {
                    IScrollViewerPrivate.set_IsRequestBringIntoViewIgnored(sender, a.NewValue is true);
                }
            }));

        #endregion IsRequestBringIntoViewIgnored


        #region IsOverpanningEnabled

        public static bool GetIsOverpanningEnabled(this ScrollViewer obj)
        {
            return (bool)obj.GetValue(IsOverpanningEnabledProperty);
        }

        public static void SetIsOverpanningEnabled(this ScrollViewer obj, bool value)
        {
            obj.SetValue(IsOverpanningEnabledProperty, value);
        }

        public static readonly DependencyProperty IsOverpanningEnabledProperty =
            DependencyProperty.RegisterAttached("IsOverpanningEnabled", typeof(bool), typeof(ScrollViewerExtensions), new PropertyMetadata(true, (s, a) =>
            {
                if (s is ScrollViewer sender)
                {
                    if (a.NewValue is true) IScrollViewerPrivate.EnableOverpan(sender);
                    else IScrollViewerPrivate.DisableOverpan(sender);
                }
            }));

        #endregion IsOverpanningEnabled

        public static bool IsInActiveDirectManipulation(this ScrollViewer scrollViewer)
        {
            return IScrollViewerPrivate.get_IsInActiveDirectManipulation(scrollViewer);
        }

    }

    internal static class IScrollViewerPrivate
    {
        // https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6-stable/src/dxaml/xcp/dxaml/idl/winrt/main/microsoft.ui.xaml.private.idl#L598

        private static readonly Guid IID_IScrollViewerPrivate = new Guid(IScrollViewerPrivateVftbl.IID_IScrollViewerPrivate);

        [Guid(IID_IScrollViewerPrivate)]
        private struct IScrollViewerPrivateVftbl
        {
            public const string IID_IScrollViewerPrivate = "7cc9bcb8-947e-582a-ae91-6a76597477c9";

            public IInspectable.Vftbl IInspectableVftbl;

            private unsafe void* _get_IsInDirectManipulation;

            private unsafe void* _get_ArePointerWheelEventsIgnored;

            private unsafe void* _set_ArePointerWheelEventsIgnored;

            private unsafe void* _get_IsRequestBringIntoViewIgnored;

            private unsafe void* _set_IsRequestBringIntoViewIgnored;

            private unsafe void* _get_IsInActiveDirectManipulation;

            private unsafe void* _DisableOverpan;

            private unsafe void* _EnableOverpan;

            private unsafe void* _SetIsNearVerticalAlignmentForced;

            public unsafe delegate* unmanaged[Stdcall]<nint, byte*, int> get_IsInDirectManipulation
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte*, int>)_get_IsInDirectManipulation;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte*, int> get_ArePointerWheelEventsIgnored
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte*, int>)_get_ArePointerWheelEventsIgnored;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte, int> set_ArePointerWheelEventsIgnored
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte, int>)_set_ArePointerWheelEventsIgnored;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte*, int> get_IsRequestBringIntoViewIgnored
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte*, int>)_get_IsRequestBringIntoViewIgnored;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte, int> set_IsRequestBringIntoViewIgnored
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte, int>)_set_IsRequestBringIntoViewIgnored;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte*, int> get_IsInActiveDirectManipulation
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte*, int>)_get_IsInActiveDirectManipulation;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, int> DisableOverpan
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, int>)_DisableOverpan;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, int> EnableOverpan
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, int>)_EnableOverpan;
                }
            }

            public unsafe delegate* unmanaged[Stdcall]<nint, byte, int> SetIsNearVerticalAlignmentForced
            {
                get
                {
                    return (delegate* unmanaged[Stdcall]<nint, byte, int>)_SetIsNearVerticalAlignmentForced;
                }
            }
        }

        public unsafe static bool get_IsInDirectManipulation(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = 0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->get_IsInDirectManipulation((nint)ptr, &b));
                    return b != 0;
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }

            return false;
        }

        public unsafe static bool get_ArePointerWheelEventsIgnored(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = 0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->get_ArePointerWheelEventsIgnored((nint)ptr, &b));
                    return b != 0;
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }

            return false;
        }

        public unsafe static void set_ArePointerWheelEventsIgnored(ScrollViewer scrollViewer, bool value)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = value ? (byte)1 : (byte)0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->set_ArePointerWheelEventsIgnored((nint)ptr, b));
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        public unsafe static bool get_IsRequestBringIntoViewIgnored(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = 0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->get_IsRequestBringIntoViewIgnored((nint)ptr, &b));
                    return b != 0;
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }

            return false;
        }

        public unsafe static void set_IsRequestBringIntoViewIgnored(ScrollViewer scrollViewer, bool value)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = value ? (byte)1 : (byte)0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->set_IsRequestBringIntoViewIgnored((nint)ptr, b));
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        public unsafe static bool get_IsInActiveDirectManipulation(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = 0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->get_IsInActiveDirectManipulation((nint)ptr, &b));
                    return b != 0;
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }

            return false;
        }

        public unsafe static void DisableOverpan(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->DisableOverpan((nint)ptr));
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        public unsafe static void EnableOverpan(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->EnableOverpan((nint)ptr));
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        public unsafe static void SetIsNearVerticalAlignmentForced(ScrollViewer scrollViewer, bool value)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            nint ptr = 0;
            try
            {
                ptr = GetIScrollViewerPrivate(scrollViewer);
                if (ptr != 0)
                {
                    byte b = value ? (byte)1 : (byte)0;
                    WinRT.ExceptionHelpers.ThrowExceptionForHR((*(IScrollViewerPrivateVftbl**)ptr)->SetIsNearVerticalAlignmentForced((nint)ptr, b));
                }
            }
            finally
            {
                if (ptr != 0) (*(WinRT.Interop.IUnknownVftbl**)ptr)->Release(ptr);
            }
        }

        private unsafe static nint GetIScrollViewerPrivate(ScrollViewer scrollViewer)
        {
            ArgumentNullException.ThrowIfNull(scrollViewer);

            var thisPtr = ((IWinRTObject)scrollViewer).NativeObject.ThisPtr;

            nint result = 0;
            ExceptionHelpers.ThrowExceptionForHR((*(WinRT.Interop.IUnknownVftbl**)thisPtr)->QueryInterface(thisPtr, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IScrollViewerPrivate)), &result));
            return result;
        }
    }
}
