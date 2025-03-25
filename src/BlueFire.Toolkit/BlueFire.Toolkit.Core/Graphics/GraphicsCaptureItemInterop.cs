using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Capture;

namespace BlueFire.Toolkit.Graphics
{
    public static class GraphicsCaptureItemInterop
    {
        public static readonly nint AllMonitors = 0;

        private static ref readonly Guid IID_IGraphicsCaptureItemInterop
        {
            get
            {
                ReadOnlySpan<byte> span = new byte[] { 27, 232, 40, 54, 172, 60, 96, 76, 183, 244, 35, 206, 14, 12, 51, 86 };
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(span));
            }
        }

        private static ref readonly Guid IID_IGraphicsCaptureItem
        {
            get
            {
                ReadOnlySpan<byte> span = new byte[] { 91, 249, 195, 121, 247, 49, 194, 78, 164, 100, 99, 46, 245, 211, 7, 96 };
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(span));
            }
        }

        private static Lock locker = new Lock();
        private static WinRT.IObjectReference? graphicsCaptureItemInterop;
        private static bool supported = true;

        private static WinRT.IObjectReference? GetInteropReference()
        {
            if (!supported) return null;

            if (graphicsCaptureItemInterop == null)
            {
                lock (locker)
                {
                    if (graphicsCaptureItemInterop == null)
                    {
                        using var objRef = WinRT.ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
                        if (objRef.TryAs(IID_IGraphicsCaptureItemInterop, out WinRT.ObjectReference<WinRT.Interop.IUnknownVftbl> interop) < 0)
                        {
                            supported = false;
                            return null;
                        }
                        graphicsCaptureItemInterop = interop;
                    }
                }
            }

            return graphicsCaptureItemInterop;
        }

        public unsafe static GraphicsCaptureItem CreateForWindow(nint window)
        {
            var interopReference = GetInteropReference();

            if (interopReference == null) return ThrowNotSupportedException();

            var thisPtr = interopReference.ThisPtr;
            nint ptr = 0;

            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>)(*(void***)thisPtr)[3])(thisPtr, window, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IGraphicsCaptureItem)), &ptr));

            return GraphicsCaptureItem.FromAbi(ptr);
        }

        public unsafe static GraphicsCaptureItem CreateForMonitor(nint monitor)
        {
            var interopReference = GetInteropReference();

            if (interopReference == null) return ThrowNotSupportedException();

            var thisPtr = interopReference.ThisPtr;
            nint ptr = 0;

            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>)(*(void***)thisPtr)[4])(thisPtr, monitor, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IGraphicsCaptureItem)), &ptr));

            return GraphicsCaptureItem.FromAbi(ptr);
        }

        [DoesNotReturn]
        private static GraphicsCaptureItem ThrowNotSupportedException()
        {
            throw new NotSupportedException("IGraphicsCaptureItemInterop");
        }
    }
}
