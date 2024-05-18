using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class DefaultValueFactory
    {
        private static Type? runtimeTypeType;
        private static object locker = new object();
        private static ConcurrentDictionary<Type, object?> typeDefaultValues = new();
        private static ConcurrentDictionary<IPropertyAdapter, object?> propertyDefaultValues = new();

        public static object? GetDefaultValue(IPropertyAdapter propertyAdapter)
        {
            if (propertyDefaultValues.TryGetValue(propertyAdapter, out var defaultValue)) return defaultValue;

            lock (locker)
            {
                if (propertyDefaultValues.TryGetValue(propertyAdapter, out defaultValue)) return defaultValue;

                defaultValue = CreatePropertyDefaultValue(propertyAdapter);
                propertyDefaultValues[propertyAdapter] = defaultValue;
                return defaultValue;
            }
        }

        public static object? GetDefaultValue([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        {
            if (typeDefaultValues.TryGetValue(type, out var defaultValue)) return defaultValue;

            lock (locker)
            {
                if (typeDefaultValues.TryGetValue(type, out defaultValue)) return defaultValue;

                defaultValue = CreateTypeDefaultValue(type);
                typeDefaultValues[type] = defaultValue;
                return defaultValue;
            }
        }

        private static object? CreatePropertyDefaultValue(IPropertyAdapter propertyAdapter)
        {
            if (propertyAdapter is DependencyPropertyAdapter dependencyPropertyAdapter)
            {
                try
                {
                    var metadata = dependencyPropertyAdapter.DependencyProperty.GetMetadata(dependencyPropertyAdapter.DeclaringType);
                    if (metadata != null)
                    {
                        return metadata.DefaultValue;
                    }
                }
                catch { }
            }

            return CreateTypeDefaultValue(propertyAdapter.PropertyType);
        }

        private static object? CreateTypeDefaultValue([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        {
            if (IsFakeMetadataType(type))
            {
                throw new ArgumentException($"{type.FullName} is not supported.", nameof(type));
            }
            else if (!type.IsValueType)
            {
                return null;
            }
            else
            {
                return GetKnownValueTypeDefaultValue(type)
                    ?? type.GetConstructor(Type.EmptyTypes)!.Invoke(null);
            }
        }

        private static object? GetKnownValueTypeDefaultValue(Type type)
        {
            if (!type.IsValueType) return null;

            else if (type == typeof(int)) return default(int);
            else if (type == typeof(byte)) return default(byte);
            else if (type == typeof(sbyte)) return default(sbyte);
            else if (type == typeof(short)) return default(short);
            else if (type == typeof(ushort)) return default(ushort);
            else if (type == typeof(uint)) return default(uint);
            else if (type == typeof(long)) return default(long);
            else if (type == typeof(ulong)) return default(ulong);
            else if (type == typeof(float)) return default(float);
            else if (type == typeof(double)) return default(double);
            else if (type == typeof(char)) return default(char);
            else if (type == typeof(bool)) return default(bool);
            else if (type == typeof(Guid)) return default(Guid);
            else if (type == typeof(WinRT.EventRegistrationToken)) return default(WinRT.EventRegistrationToken);
            else if (type == typeof(System.DateTime)) return default(System.DateTime);
            else if (type == typeof(System.DateTimeOffset)) return default(System.DateTimeOffset);
            else if (type == typeof(System.TimeSpan)) return default(System.TimeSpan);
            else if (type == typeof(System.Numerics.Matrix3x2)) return default(System.Numerics.Matrix3x2);
            else if (type == typeof(System.Numerics.Matrix4x4)) return default(System.Numerics.Matrix4x4);
            else if (type == typeof(System.Numerics.Plane)) return default(System.Numerics.Plane);
            else if (type == typeof(System.Numerics.Quaternion)) return default(System.Numerics.Quaternion);
            else if (type == typeof(System.Numerics.Vector2)) return default(System.Numerics.Vector2);
            else if (type == typeof(System.Numerics.Vector3)) return default(System.Numerics.Vector3);
            else if (type == typeof(System.Numerics.Vector4)) return default(System.Numerics.Vector4);

            else if (type == typeof(int?)) return default(int?);
            else if (type == typeof(byte?)) return default(byte?);
            else if (type == typeof(sbyte?)) return default(sbyte?);
            else if (type == typeof(short?)) return default(short?);
            else if (type == typeof(ushort?)) return default(ushort?);
            else if (type == typeof(uint?)) return default(uint?);
            else if (type == typeof(long?)) return default(long?);
            else if (type == typeof(ulong?)) return default(ulong?);
            else if (type == typeof(float?)) return default(float?);
            else if (type == typeof(double?)) return default(double?);
            else if (type == typeof(char?)) return default(char?);
            else if (type == typeof(bool?)) return default(bool?);
            else if (type == typeof(Guid?)) return default(Guid?);
            else if (type == typeof(WinRT.EventRegistrationToken?)) return default(WinRT.EventRegistrationToken?);
            else if (type == typeof(System.DateTime?)) return default(System.DateTime?);
            else if (type == typeof(System.DateTimeOffset?)) return default(System.DateTimeOffset?);
            else if (type == typeof(System.TimeSpan?)) return default(System.TimeSpan?);
            else if (type == typeof(System.Numerics.Matrix3x2?)) return default(System.Numerics.Matrix3x2?);
            else if (type == typeof(System.Numerics.Matrix4x4?)) return default(System.Numerics.Matrix4x4?);
            else if (type == typeof(System.Numerics.Plane?)) return default(System.Numerics.Plane?);
            else if (type == typeof(System.Numerics.Quaternion?)) return default(System.Numerics.Quaternion?);
            else if (type == typeof(System.Numerics.Vector2?)) return default(System.Numerics.Vector2?);
            else if (type == typeof(System.Numerics.Vector3?)) return default(System.Numerics.Vector3?);
            else if (type == typeof(System.Numerics.Vector4?)) return default(System.Numerics.Vector4?);

            return null;
        }


        private static bool IsRuntimeType(Type type)
        {
            if (runtimeTypeType == null)
            {
                try
                {
                    runtimeTypeType = typeof(Type).GetType();
                }
                catch
                {
                    runtimeTypeType = typeof(void);
                }
            }

            return type.IsAssignableTo(runtimeTypeType);
        }

        private static bool IsFakeMetadataType(Type type)
        {
            if (IsRuntimeType(type)) return false;
            return type.GetType().FullName == "ABI.System.FakeMetadataType";
        }



    }
}
