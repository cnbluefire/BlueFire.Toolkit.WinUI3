using BlueFire.Toolkit.WinUI3.Extensions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32;
using Windows.Win32.System.WinRT;
using WinRT;
using WinCompositor = Windows.UI.Composition.Compositor;
using Windows.Win32.Graphics.DirectComposition;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    internal unsafe class InteropCompositor
    {
        internal unsafe static ComPtr<IInteropCompositorFactoryPartner> CreateInteropCompositorFactoryPartner()
        {
            var hr = (Windows.Win32.Foundation.HRESULT)Windows.UI.Composition.Compositor.As<IWinRTObject>().NativeObject.TryAs(IInteropCompositorFactoryPartner.IID_Guid, out var ppv);
            if (hr.Succeeded)
            {
                return ComPtr<IInteropCompositorFactoryPartner>.Attach(ref ppv);
            }

            return default;
        }

        internal unsafe static WinCompositor? CreateCompositor()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64
                && RuntimeInformation.ProcessArchitecture != Architecture.Arm64) return null;

            using var factory = CreateInteropCompositorFactoryPartner();

            if (factory.HasValue)
            {
                using (ComPtr<IInteropCompositorPartner> result = default)
                {
                    var hr = factory.Value.CreateInteropCompositor(
                        (void*)0,
                        (void*)0,
                        IInteropCompositorPartner.IID_Guid,
                        result.PointerRef);

                    if (hr.Succeeded && result.HasValue)
                    {
                        return WinCompositor.FromAbi(result.Pointer);
                    }
                }
            }

            return null;
        }


        internal static SIZE QueryWindowThumbnailSourceSize(HWND hwndSource, bool fSourceClientAreaOnly)
        {
            SIZE size = default;

            DwmpQueryWindowThumbnailSourceSize(hwndSource, fSourceClientAreaOnly, &size);

            return size;
        }

        internal static ComPtr<IDCompositionVisual2> CreateSharedThumbnailVisual(HWND hwndDestination, HWND hwndSource, int dwThumbnailFlags, ref Windows.Win32.Graphics.Dwm.DWM_THUMBNAIL_PROPERTIES thumbnailProperties, ComPtr<IDCompositionDevice2> pDCompDevice, out nint hThumbnailId)
        {
            hThumbnailId = 0;
            ComPtr<IDCompositionVisual2> result = default;

            fixed (Windows.Win32.Graphics.Dwm.DWM_THUMBNAIL_PROPERTIES* pThumbnailProperties = &thumbnailProperties)
            fixed (nint* phThumbnailId = &hThumbnailId)
            {
                var hr = DwmpCreateSharedThumbnailVisual(hwndDestination, hwndSource, dwThumbnailFlags, pThumbnailProperties, pDCompDevice.AsTypedPointer(), result.PointerRef, phThumbnailId);
            }

            return result;
        }

        internal static Windows.UI.Composition.Visual? CreateVisualFromHwnd(WinCompositor compositor, HWND hwndDestination, HWND hwndSource, bool sourceClientAreaOnly, out nint hThumbnailId)
        {
            hThumbnailId = 0;

            ComObjectHelper.QueryInterface<IDCompositionDevice2>(compositor, IDCompositionDevice2.IID_Guid, out var dCompDevice)
                .ThrowOnFailure();

            try
            {
                var size = QueryWindowThumbnailSourceSize(hwndSource, false);

                var width = size.Width;
                var height = size.Height;

                var thumbProps = new Windows.Win32.Graphics.Dwm.DWM_THUMBNAIL_PROPERTIES()
                {
                    dwFlags = PInvoke.DWM_TNP_SOURCECLIENTAREAONLY
                        | PInvoke.DWM_TNP_VISIBLE
                        | PInvoke.DWM_TNP_RECTDESTINATION
                        | PInvoke.DWM_TNP_RECTSOURCE
                        | PInvoke.DWM_TNP_OPACITY,
                    opacity = 255,
                    fVisible = true,
                    fSourceClientAreaOnly = sourceClientAreaOnly,
                    rcDestination = new RECT(0, 0, width, height),
                    rcSource = new RECT(0, 0, width, height),
                };

                using var visual = CreateSharedThumbnailVisual(hwndDestination, hwndSource, 2, ref thumbProps, dCompDevice, out hThumbnailId);
                if (visual.HasValue)
                {
                    using ComPtr<IDCompositionVisual2> dCompVisualContainer = default;
                    dCompDevice.Value.CreateVisual(dCompVisualContainer.TypedPointerRef).ThrowOnFailure();

                    dCompVisualContainer.Value.AddVisual(visual.AsTypedPointer<IDCompositionVisual>(), true, null);

                    var visual2 = Windows.UI.Composition.Visual.FromAbi(dCompVisualContainer.Pointer);
                    visual2.Size = new System.Numerics.Vector2(width, height);
                    return visual2;
                }

                hThumbnailId = 0;
                return null;
            }
            finally
            {
                dCompDevice.Release();
            }
        }

#pragma warning disable CS1591,CS1573,CS0465,CS0649,CS8019,CS1570,CS1584,CS1658,CS0436,CS8981

        [Guid("E7894C70-AF56-4F52-B382-4B3CD263DC6F")]
        internal unsafe partial struct IInteropCompositorPartner
        {
            internal unsafe HRESULT QueryInterface(in global::System.Guid riid, out void* ppvObject)
            {
                fixed (void** ppvObjectLocal = &ppvObject)
                {
                    fixed (global::System.Guid* riidLocal = &riid)
                    {
                        HRESULT __result = this.QueryInterface(riidLocal, ppvObjectLocal);
                        return __result;
                    }
                }
            }

            public unsafe HRESULT QueryInterface(global::System.Guid* riid, void** ppvObject)
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, global::System.Guid*, void**, HRESULT>)lpVtbl[0])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this), riid, ppvObject);
            }

            public uint AddRef()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, uint>)lpVtbl[1])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this));
            }

            public uint Release()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, uint>)lpVtbl[2])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this));
            }

            public unsafe HRESULT MarkDirty()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, HRESULT>)lpVtbl[3])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this));
            }

            public unsafe HRESULT ClearCallback()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, HRESULT>)lpVtbl[4])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this));
            }

            public unsafe HRESULT CreateManipulationTransform(void* transform, Guid iid, void** result)
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, void*, Guid, void**, HRESULT>)lpVtbl[5])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this), transform, iid, result);
            }

            public unsafe HRESULT RealClose()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorPartner*, HRESULT>)lpVtbl[6])((IInteropCompositorPartner*)Unsafe.AsPointer(ref this));
            }

            private void** lpVtbl;

            internal static readonly Guid IID_Guid = new Guid("E7894C70-AF56-4F52-B382-4B3CD263DC6F");
        }


        [Guid("22118ADF-23F1-4801-BCFA-66CBF48CC51B")]
        internal unsafe partial struct IInteropCompositorFactoryPartner
        {
            internal unsafe HRESULT QueryInterface(in global::System.Guid riid, out void* ppvObject)
            {
                fixed (void** ppvObjectLocal = &ppvObject)
                {
                    fixed (global::System.Guid* riidLocal = &riid)
                    {
                        HRESULT __result = this.QueryInterface(riidLocal, ppvObjectLocal);
                        return __result;
                    }
                }
            }

            public unsafe HRESULT QueryInterface(global::System.Guid* riid, void** ppvObject)
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorFactoryPartner*, global::System.Guid*, void**, HRESULT>)lpVtbl[0])((IInteropCompositorFactoryPartner*)Unsafe.AsPointer(ref this), riid, ppvObject);
            }

            public uint AddRef()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorFactoryPartner*, uint>)lpVtbl[1])((IInteropCompositorFactoryPartner*)Unsafe.AsPointer(ref this));
            }

            public uint Release()
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorFactoryPartner*, uint>)lpVtbl[2])((IInteropCompositorFactoryPartner*)Unsafe.AsPointer(ref this));
            }

            public unsafe HRESULT CreateInteropCompositor(void* renderingDevice, void* callback, Guid iid, void** instance)
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorFactoryPartner*, void*, void*, Guid, void**, HRESULT>)lpVtbl[6])((IInteropCompositorFactoryPartner*)Unsafe.AsPointer(ref this), renderingDevice, callback, iid, instance);
            }

            public unsafe HRESULT CheckEnabled(bool* enableInteropCompositor, bool* enableExposeVisual)
            {
                return ((delegate* unmanaged[Stdcall]<IInteropCompositorFactoryPartner*, bool*, bool*, HRESULT>)lpVtbl[7])((IInteropCompositorFactoryPartner*)Unsafe.AsPointer(ref this), enableInteropCompositor, enableExposeVisual);
            }

            private void** lpVtbl;

            internal static readonly Guid IID_Guid = new Guid("22118ADF-23F1-4801-BCFA-66CBF48CC51B");
        }


        [DllImport("dwmapi.dll", EntryPoint = "#147")]
        private static extern HRESULT DwmpCreateSharedThumbnailVisual(HWND hwndDestination, HWND hwndSource, int dwThumbnailFlags, Windows.Win32.Graphics.Dwm.DWM_THUMBNAIL_PROPERTIES* pThumbnailProperties, void* pDCompDevice, void** ppVisual, nint* phThumbnailId);

        [DllImport("dwmapi.dll", EntryPoint = "#162")]
        private static extern HRESULT DwmpQueryWindowThumbnailSourceSize(HWND hwndSource, bool fSourceClientAreaOnly, SIZE* pSize);

    }

#pragma warning restore CS1591,CS1573,CS0465,CS0649,CS8019,CS1570,CS1584,CS1658,CS0436,CS8981

}
