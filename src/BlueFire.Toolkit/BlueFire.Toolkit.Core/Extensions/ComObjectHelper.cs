using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;
using WinRT.Interop;

namespace BlueFire.Toolkit.Extensions
{
    internal static class ComObjectHelper
    {
        private static Lock comWrappersLocker = new Lock();
        private static StrategyBasedComWrappers? strategyBasedComWrappers;
        private static readonly ConcurrentDictionary<Type, Type?> DefaultInterfaceTypeCache = new();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, Guid?> GeneratedComInterfaceIdCache = new();

        internal static StrategyBasedComWrappers StrategyBasedComWrappers
        {
            get
            {
                if (strategyBasedComWrappers == null)
                {
                    lock (comWrappersLocker)
                    {
                        if (strategyBasedComWrappers == null)
                        {
                            strategyBasedComWrappers = new StrategyBasedComWrappers();
                        }
                    }
                }
                return strategyBasedComWrappers;
            }
        }

        internal static readonly HRESULT E_POINTER = new HRESULT(unchecked((int)0x80004003));

        internal static readonly HRESULT E_NOINTERFACE = new HRESULT(unchecked((int)0x80004002));

        internal static readonly HRESULT E_NOTIMPL = new HRESULT(unchecked((int)0x80004001));

        internal static unsafe HRESULT QueryInterface(object obj, Guid iid, out nint ppvObject)
        {
            ppvObject = 0;

            if (obj == null) return E_POINTER;

            if (ComWrappersSupport.TryUnwrapObject(obj, out var objRef))
            {
                return new HRESULT(objRef.TryAs(iid, out ppvObject));
            }
            else if (ComWrappers.TryGetComInstance(obj, out var unknown))
            {
                fixed (nint* ppv = &ppvObject)
                {
                    return (HRESULT)(*((IUnknownVftbl**)unknown))->QueryInterface(unknown, &iid, ppv);
                }
            }
            else if (RuntimeFeature.IsDynamicCodeSupported && Marshal.IsComObject(obj))
            {
                IUnknownVftbl** punk = null;
                try
                {
                    punk = (IUnknownVftbl**)Marshal.GetIUnknownForObject(obj);

                    fixed (nint* ppv = &ppvObject)
                    {
                        return (HRESULT)(*punk)->QueryInterface((nint)punk, &iid, ppv);
                    }
                }
                catch (Exception ex)
                {
                    return new HRESULT(ex.HResult);
                }
                finally
                {
                    if (punk != null) (*punk)->Release((nint)punk);
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

        internal static bool TryGetInterfaceId<T>(out Guid iid) where T : class
        {
            iid = Guid.Empty;

            // GeneratedComInterface
            if (GetGeneratedComInterfaceId<T>() is Guid generatedIID)
            {
                iid = generatedIID;
                return true;
            }

            // BuiltInComInterop
            if (IsBuiltInComInteropType<T>())
            {
                iid = typeof(T).GUID;
                return true;
            }

            // CsWinRT
            if (TryGetDefaultInterfaceTypeForRuntimeClassType(typeof(T), out var defaultInterface))
            {
                iid = defaultInterface.GUID;
                return true;
            }

            return false;
        }

        internal static unsafe T? ConvertToManaged<T>(nint unmanaged) where T : class =>
            ConvertToManaged<T>((void*)unmanaged);

        internal static unsafe T? ConvertToManaged<T>(void* unmanaged) where T : class
        {
            if (unmanaged == null) return null;

            // ComWrappers or CsWinRT
            if (ComWrappers.TryGetObject((nint)unmanaged, out var obj))
            {
                return (T)obj;
            }

            // GeneratedComInterface
            if (GetGeneratedComInterfaceId<T>() is not null)
            {
                return ComInterfaceMarshaller<T>.ConvertToManaged(unmanaged);
            }

            // BuiltInComInterop
            if (IsBuiltInComInteropType<T>())
            {
                return (T)Marshal.GetObjectForIUnknown((nint)unmanaged);
            }

            // CsWinRT
            if (TryGetDefaultInterfaceTypeForRuntimeClassType(typeof(T), out var defaultInterface))
            {
                return ComWrappersSupport.CreateRcwForComObject<T>((nint)unmanaged);
            }

            throw new InvalidCastException("Unable to cast the provided COM interface to a managed object.");
        }

        internal static unsafe void* ConvertToUnmanaged<T>(T? managed) where T : class
        {
            if (managed is null) return null;

            // IWinRTObject
            if (ComWrappersSupport.TryUnwrapObject(managed, out var objRef))
            {
                return (void*)objRef.GetRef();
            }

            // ComWrappers or CsWinRT
            if (ComWrappers.TryGetComInstance(objRef, out var unknown))
            {
                Marshal.AddRef(unknown);
                return (void*)unknown;
            }
            // GeneratedComInterface
            else if (GetGeneratedComInterfaceId<T>() is not null)
            {
                return (void*)StrategyBasedComWrappers.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None);
            }

            // BuiltInComInterop
            if (IsBuiltInComInteropType<T>())
            {
                return (void*)Marshal.GetIUnknownForObject(managed);
            }

            throw new InvalidCastException("Unable to cast the provided managed object to a COM interface.");
        }

        private static bool IsBuiltInComInteropType<T>()
        {
            return RuntimeFeature.IsDynamicCodeSupported
                && typeof(T).IsDefined(typeof(ComImportAttribute));
        }

        private static Guid? GetGeneratedComInterfaceId<T>()
        {
            return GeneratedComInterfaceIdCache.GetOrAdd(typeof(T).TypeHandle, static t => StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(t)?.Iid);
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification =
            "The path using reflection to retrieve the default interface property is only used with legacy projections. " +
            "Applications which make use of trimming will make use of updated projections and won't hit that code path.")]
#endif
        internal static bool TryGetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass, [NotNullWhen(true)] out Type? defaultInterface)
        {
            defaultInterface = DefaultInterfaceTypeCache.GetOrAdd(runtimeClass, (runtimeClass) =>
            {
                runtimeClass = runtimeClass.GetRuntimeClassCCWType() ?? runtimeClass;
                var attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();

                if (attr is null)
                {
                    return null;
                }

#if NET
                // Using AOT requires using updated projections, which means we expect the type constructor to be used.
                // The one taking a string for the property is not trim safe and is not used anymore by projections.
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    return attr.DefaultInterface;
                }
#endif

                if (attr.DefaultInterface != null)
                {
                    return attr.DefaultInterface;
                }

                // This path is only ever taken for .NET Standard legacy projections
                if (attr.DefaultInterfaceProperty != null)
                {
                    return runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)?.PropertyType;
                }

                return null;
            });
            return defaultInterface != null;
        }
    }



    internal unsafe struct ComPtr<T> : IDisposable where T : unmanaged
    {
        private nint _ptr;

        private ComPtr(nint ptr)
        {
            _ptr = ptr;
        }

        internal readonly ref T Value
        {
            get
            {
                if (_ptr == 0) ComObjectHelper.E_POINTER.ThrowOnFailure();

                return ref Unsafe.AsRef<T>((void*)_ptr);
            }
        }

        internal readonly bool HasValue => _ptr != 0;

        internal readonly nint Pointer => _ptr;

        internal void** PointerRef => (void**)Unsafe.AsPointer(ref _ptr);

        internal T** TypedPointerRef => (T**)Unsafe.AsPointer(ref _ptr);

        internal void* AsPointer()
        {
            return (void*)Pointer;
        }

        internal T* AsTypedPointer()
        {
            return (T*)Pointer;
        }

        internal U* AsTypedPointer<U>() where U : unmanaged
        {
            return (U*)Pointer;
        }

        internal HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            if (_ptr == 0) return ComObjectHelper.E_POINTER;

            return AsTypedPointer<IUnknown>()->QueryInterface(riid, ppvObject);
        }

        internal uint AddRef()
        {
            if (_ptr == 0) return 0;

            return AsTypedPointer<IUnknown>()->AddRef();
        }

        internal uint Release()
        {
            if (_ptr == 0) return 0;

            return AsTypedPointer<IUnknown>()->Release();
        }

        internal T* Detach()
        {
            T* ptr = null;
            Detach((void**)&ptr);
            return ptr;
        }

        internal void Detach(void** ptr)
        {
            *ptr = (void*)Interlocked.Exchange(ref _ptr, IntPtr.Zero);
        }

        internal void Detach(out void* ptr)
        {
            fixed (void** fixedPtr = &ptr)
            {
                Detach(fixedPtr);
            }
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

        internal static unsafe ComPtr<T> Attach(void** ptr)
        {
            var p = *ptr;
            *ptr = null;
            return new ComPtr<T>((nint)p);
        }

        void IDisposable.Dispose()
        {
            Release();
        }
    }
}
