using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public class RuntimeEnabledFeatureDetector
    {
        // https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6-stable/src/dxaml/xcp/inc/IXamlTestHooks-win.h

        private const string DxamlCoreTestHooks_ClassName = "Microsoft.UI.Xaml.DxamlCoreTestHooks";
        private static readonly Guid IID_IDxamlCoreTestHooksStatics = new Guid("84118843-ff81-55aa-9153-bca77f03a774");
        private static readonly Guid IID_IXamlTestHooks = new Guid("43d4bcbd-4f02-4651-9ecc-dcfec9f786a7");

        private static Lock locker = new Lock();
        private static nint ptr_IXamlTestHooks;

        private RuntimeEnabledFeatureDetector() { }

        private unsafe static void EnsureIXamlTestHooks()
        {
            if (ptr_IXamlTestHooks == 0)
            {
                lock (locker)
                {
                    if (ptr_IXamlTestHooks == 0)
                    {
                        using var factory = WinRT.ActivationFactory.Get(DxamlCoreTestHooks_ClassName, IID_IDxamlCoreTestHooksStatics);

                        var thisPtr = factory.ThisPtr;
                        nint _ptr_IDxamlCoreTestHooks = 0;
                        nint _ptr_IXamlTestHooks = 0;

                        try
                        {
                            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint*, int>)(*(void***)thisPtr)[6])(thisPtr, &_ptr_IDxamlCoreTestHooks));
                            WinRT.ExceptionHelpers.ThrowExceptionForHR((*(WinRT.Interop.IUnknownVftbl**)_ptr_IDxamlCoreTestHooks)->QueryInterface(_ptr_IDxamlCoreTestHooks, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_IXamlTestHooks)), &_ptr_IXamlTestHooks));
                            ptr_IXamlTestHooks = _ptr_IXamlTestHooks;
                        }
                        finally
                        {
                            if (_ptr_IDxamlCoreTestHooks != 0) (*(WinRT.Interop.IUnknownVftbl**)_ptr_IDxamlCoreTestHooks)->Release(_ptr_IDxamlCoreTestHooks);
                        }
                    }
                }
            }
        }

        public unsafe static bool IsFeatureEnabled(RuntimeEnabledFeature feature)
        {
            EnsureIXamlTestHooks();

            var thisPtr = ptr_IXamlTestHooks;
            ArgumentNullException.ThrowIfNull((void*)thisPtr, "");

            byte b = 0;
            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, RuntimeEnabledFeature, byte*, int>)(*(void***)thisPtr)[23])(thisPtr, feature, &b));
            return b != 0;
        }

        public unsafe static void SetFeatureOverride(RuntimeEnabledFeature feature, bool enabled, out bool wasPreviouslyEnabled)
        {
            wasPreviouslyEnabled = false;

            EnsureIXamlTestHooks();

            var thisPtr = ptr_IXamlTestHooks;
            ArgumentNullException.ThrowIfNull((void*)thisPtr, "");

            byte value = enabled ? (byte)1 : (byte)0;
            byte b = 0;
            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, RuntimeEnabledFeature, byte, byte*, int>)(*(void***)thisPtr)[24])(thisPtr, feature, value, &b));
            wasPreviouslyEnabled = b != 0;
        }

        public unsafe static void ClearAllFeatureOverrides()
        {
            EnsureIXamlTestHooks();

            var thisPtr = ptr_IXamlTestHooks;
            ArgumentNullException.ThrowIfNull((void*)thisPtr, "");

            WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int>)(*(void***)thisPtr)[25])(thisPtr));
        }


        // The set of possible RuntimeFeatures kept in sync with RuntimeFeatureData.cpp
        public enum RuntimeEnabledFeature
        {
            TestDisabledByDefaultFeature = 0,
            TestEnabledByDefaultFeature,
            TestGetFeatureValue,
            ParserCustomWriter,
            XamlDiagnostics,
            EnforceXbfV2Stream,
            UsePrivateHeap,
            DrawDWriteTextLayoutInGreen, // Turns on a debugging mode where all text layouted by IDWriteTextLayout are displayed in green color.
            BackgroundThreadImageLoading,
            DisableTransitionsForTest, // Disables state change transitions during testing to reduce execution time.
            EnableStyleOptimization,    // Turns on style flyweighting
            DisableGlobalAnimations, // Causes the framework to report that animations are globally disabled.
            DisableTextBoxCaret,
            EnableAnimatedListViewBaseScrolling,
            ShrinkApplicationViewVisibleBounds,
            EnableCulledFromRenderWalkOptimization,
            EnableFullCompNodeTree,     // Creates 1 CompNode for every UIElement in the core tree.
            EnableVisualDebugTags,   // Adds extra debugging info to WinRT visuals
            EnableTracingCookies,    // Adds ETW logging of Expressions/Property Sets in the DWM
            ForceWarp,
            AllowFailFastOnAnyFailure,  // If enabled, this allows some processes (like Shell) to immediately FailFast on any stowed exception failure.
            ReleaseGraphicsDeviceOnSuspend,
            ReleaseGraphicsAndDCompDevicesOnSuspend,
            EnableOnFocusMouseMode,        // Indicates that WhenFocused should be honored for controls.
            EnableApplicationMouseMode,    // Indicates that apps should be in mouse mode unless requested not to be.
            EnableAutoFocusOverride,
            AutoFocus_Shadow,
            AutoFocus_ManifoldShadow,
            AutoFocus_Distance,
            AutoFocus_SecondaryDistance,
            SlowDownAnimations,
            AnimationFailFast,
            EnableCoreShutdown,
            TraceDCompSurfaces,
            TraceDCompSurfaceUpdates,
            RenderTargetBitmapUsingSpriteVisualsTestMode,
            EnableGlobalAnimations, // Causes the framework to report that animations are enabled globally by the system
            InsertDManipHitTestVisual,
            DumpMockDManipHitTestVisual,
            DoNotSetRootScrollViewerBackground,
            DisableDefaultConnectedAnimationConfiguration,
            EnableWUCShapes,
            EnableDebugD3DDevice,
            SynchronousCompTreeUpdates,         // Skip CompositorTreeHost and update the comp node synchronously
            WindowedPopupActualBoundsMode,      // Use ActualBounds of Popup's child to determine size of windowed popup HWND
            EnableWindowedPopupDebugVisual,
            ForceRoundedCalendarViewBaseItemChrome,
            DenyRoundedCalendarViewBaseItemChrome,
            EnableUWPWindow,                   // if enabled, it will allow creating an UWP window and thus launching UWP apps // task 31614767 : UWP window creation has been disabled
            EnableReentrancyChecks, // If enabled, we will FailFast when reentrancy is detected
                                    // add entries before here to be safe as C# test code uses fixed numeric values instead of this enum so if added after DBG entry, the enum value can change
#if DBG
        CaptureTrackerPtrCallStack, // Capture the stack that calls TrackerPtr.Set
#endif
            ForceProjectedShadowsOnByDefault,
            EnableDropShadowDebugVisual,
            ForceRoundedListViewBaseItemChrome,
            DenyRoundedListViewBaseItemChrome,
            ForceSelectionIndicatorVisualEnabled,
            DenySelectionIndicatorVisualEnabled,
            ForceSelectionIndicatorModeInline,
            ForceSelectionIndicatorModeOverlay,
            DisableDWriteTypographicModel,
            ForceDWriteTypographicModel,        // overides DisableDWriteTypographicModel

            // Insert new enum values before this one.
            // This is used to initialize the lengths of the
            // feature data and state arrays.
            //LastValue
        };

    }
}
