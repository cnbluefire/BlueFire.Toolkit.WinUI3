using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;
using WinRT.Interop;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    internal static class ComObjectHelper
    {
        internal static readonly HRESULT E_POINTER = new HRESULT(unchecked((int)0x80004003));

        internal static readonly HRESULT E_NOINTERFACE = new HRESULT(unchecked((int)0x80004002));

        internal static readonly HRESULT E_NOTIMPL = new HRESULT(unchecked((int)0x80004001));

        internal static HRESULT QueryInterface(object obj, Guid iid, out nint ppvObject)
        {
            ppvObject = 0;

            if (obj == null) return E_POINTER;

            if (ComWrappersSupport.TryUnwrapObject(obj, out var objRef))
            {
                return new HRESULT(objRef.TryAs(iid, out ppvObject));
            }
            else if (Marshal.IsComObject(obj))
            {
                try
                {
                    var punk = Marshal.GetIUnknownForObject(obj);

                    using var objRef2 = ObjectReference<IUnknownVftbl>.Attach(ref punk);

                    if (objRef2 == null)
                    {
                        return E_POINTER;
                    }
                    return new HRESULT(objRef2.TryAs(iid, out ppvObject));
                }
                catch (Exception ex)
                {
                    return new HRESULT(ex.HResult);
                }
            }

            return E_NOINTERFACE;
        }

        internal static unsafe HRESULT QueryInterface<T>(object obj, Guid iid, out ComPtr<T> ppvObject)
            where T : unmanaged
        {
            ppvObject = default;

            var hr = QueryInterface(obj, iid, out nint ppv);
            if (hr.Succeeded)
            {
                ppvObject = ComPtr<T>.Attach(ref ppv);
            }
            return hr;
        }

        internal static unsafe HRESULT QueryInterface(nint punk, ref Guid iid, out nint ppvObject)
        {
            ppvObject = 0;

            if (punk == 0) return E_POINTER;

            fixed (Guid* riid = &iid)
            fixed (nint* result = &ppvObject)
            {
                var hr = ((IUnknownVftbl*)punk)->QueryInterface(punk, riid, result);
                return new HRESULT(hr);
            }
        }
    }


    internal struct ComPtr<T> : IDisposable where T : unmanaged
    {
        private nint _ptr;

        private ComPtr(nint ptr)
        {
            _ptr = ptr;
        }

        internal readonly unsafe ref T Value
        {
            get
            {
                if (_ptr == 0) ComObjectHelper.E_POINTER.ThrowOnFailure();

                return ref Unsafe.AsRef<T>((void*)_ptr);
            }
        }

        internal readonly bool HasValue => _ptr != 0;

        internal readonly nint Pointer => _ptr;

        internal unsafe void** PointerRef => (void**)Unsafe.AsPointer(ref _ptr);

        internal unsafe T** TypedPointerRef => (T**)Unsafe.AsPointer(ref _ptr);

        internal unsafe void* AsPointer()
        {
            return (void*)Pointer;
        }

        internal unsafe T* AsTypedPointer()
        {
            return (T*)Pointer;
        }

        internal unsafe U* AsTypedPointer<U>() where U : unmanaged
        {
            return (U*)Pointer;
        }

        internal unsafe HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            if (_ptr == 0) return ComObjectHelper.E_POINTER;

            return AsTypedPointer<IUnknown>()->QueryInterface(riid, ppvObject);
        }

        internal unsafe uint AddRef()
        {
            if (_ptr == 0) return 0;

            return AsTypedPointer<IUnknown>()->AddRef();
        }

        internal unsafe uint Release()
        {
            if (_ptr == 0) return 0;

            return AsTypedPointer<IUnknown>()->Release();
        }

        internal static ComPtr<T> FromAbi(nint ptr)
        {
            var comPtr = new ComPtr<T>(ptr);
            comPtr.AddRef();
            return comPtr;
        }

        internal static unsafe ComPtr<T> FromAbi(void* ptr) => FromAbi((nint)ptr);

        internal static ComPtr<T> Attach(ref nint ptr)
        {
            var comPtr = new ComPtr<T>(ptr);
            ptr = 0;
            return comPtr;
        }

        internal static ComPtr<T> Attach(nint ptr)
        {
            return new ComPtr<T>(ptr);
        }

        internal static unsafe ComPtr<T> Attach(void* ptr)
        {
            return new ComPtr<T>((nint)ptr);
        }

        void IDisposable.Dispose()
        {
            Release();
        }
    }
}
