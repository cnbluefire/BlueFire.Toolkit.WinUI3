using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Input
{
    internal class InputManager
    {
        private const string InputManagerClassName = "Microsoft.UI.Xaml.Input.InputManager";
        private static readonly Guid IID_IInputManagerStatics = new Guid("77f40f44-9f61-54a0-a820-c7912d0782c0");
        private static Lock locker = new Lock();
        private static nint ptr_IInputManagerStatics = 0;

        private InputManager() { }

        private static void EnsureInputManagerStatics()
        {
            if (ptr_IInputManagerStatics == 0)
            {
                lock (locker)
                {
                    if (ptr_IInputManagerStatics == 0)
                    {
                        using var factory = WinRT.ActivationFactory.Get(InputManagerClassName, IID_IInputManagerStatics);
                        ptr_IInputManagerStatics = factory.GetRef();
                    }
                }
            }
        }

        public unsafe static LastInputDeviceType GetLastInputDeviceType()
        {
            EnsureInputManagerStatics();

            var thisPtr = ptr_IInputManagerStatics;
            if (thisPtr != 0)
            {
                var name = WinRT.IInspectable.FromAbi(thisPtr).GetRuntimeClassName();
                LastInputDeviceType result = LastInputDeviceType.None;
                WinRT.ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, LastInputDeviceType*, int>)((*(void***)thisPtr)[6]))(thisPtr, &result));
                return result;
            }

            return LastInputDeviceType.None;
        }

        internal unsafe static Guid[] GetIids(nint thisPtr)
        {
            int count = 0;
            Guid* pGuid = null;
            var hr = (*(WinRT.IInspectable.Vftbl**)thisPtr)->GetIids(thisPtr, &count, (nint*)&pGuid);
            if (hr == 0 && count > 0)
            {
                return new Span<Guid>(pGuid, count).ToArray();
            }
            return [];
        }
    }

    public enum LastInputDeviceType
    {
        None,
        Mouse,
        Touch,
        Pen,
        Keyboard,
        GamepadOrRemote,
    }
}
