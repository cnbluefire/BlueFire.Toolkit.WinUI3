using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal class PropertyAdapterManager
    {
        private static ConcurrentDictionary<string, IPropertyAdapter?> cache = new ConcurrentDictionary<string, IPropertyAdapter?>();

        public static IPropertyAdapter? GetOrCreateAdapter(
            string propertyName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.NonPublicFields
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type propertyDeclaringType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type propertyType)
        {
            var key = $"{propertyDeclaringType.FullName}.{propertyName}";

            if (cache.TryGetValue(key, out var adapter)) return adapter;
            lock (cache)
            {
                if (cache.TryGetValue(key, out adapter)) return adapter;

                adapter = CreateAdapter(propertyName, propertyDeclaringType, propertyType);
                cache[key] = adapter;
                return adapter;
            }
        }

        private static IPropertyAdapter? CreateAdapter(
            string propertyName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.NonPublicFields
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type propertyDeclaringType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type propertyType)
        {
            if (propertyName == "Source" && propertyType == typeof(object) && propertyDeclaringType == typeof(Binding))
            {
                return null;
            }
            try
            {
                if (Application.Current is IXamlMetadataProvider xamlMetadataProvider)
                {
                    var propertyDeclaringXamlType = xamlMetadataProvider.GetXamlType(propertyDeclaringType);
                    if (propertyDeclaringXamlType != null && !IsXamlSystemBaseType(propertyDeclaringXamlType))
                    {
                        var member = propertyDeclaringXamlType?.GetMember(propertyName);
                        if (member != null)
                        {
                            return new XamlMemberPropertyAdapter(member, propertyType, propertyDeclaringType);
                        }
                    }
                }
            }
            catch { }

            try
            {
                var propertyInfo = propertyDeclaringType.GetProperty($"{propertyName}Property", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo != null
                    && propertyInfo.DeclaringType == propertyDeclaringType
                    && propertyInfo.PropertyType.IsAssignableTo(typeof(DependencyProperty))
                    && propertyInfo.GetValue(null) is DependencyProperty _dp1)
                {
                    return new DependencyPropertyAdapter(
                        _dp1,
                        propertyName,
                        propertyType,
                        propertyDeclaringType);
                }
            }
            catch { }

            var fieldInfo = propertyDeclaringType.GetField($"{propertyName}Property", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo != null
                && fieldInfo.DeclaringType == propertyDeclaringType
                && fieldInfo.FieldType.IsAssignableTo(typeof(DependencyProperty))
                && fieldInfo.GetValue(null) is DependencyProperty _dp2)
            {
                return new DependencyPropertyAdapter(
                    _dp2,
                    propertyName,
                    propertyType,
                    propertyDeclaringType);
            }

            return null;

            static bool IsXamlSystemBaseType(IXamlType xamlType)
            {
                if (xamlType == null) return false;
                return xamlType.GetType().Name == "XamlSystemBaseType";
            }
        }
    }

    internal interface IPropertyAdapter
    {
        string PropertyName { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type PropertyType { get; }

        Type DeclaringType { get; }

        bool SetValue(object obj, object? value);

        bool TryCreateValueFromString(string? text, out object? value);


        protected static bool HandleKnownTypeValue(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            string? text,
            out object? value)
        {
            if (HandleNullValue(type, text, out value)) return true;

            if (type == typeof(string))
            {
                value = text;
                return true;
            }

            return false;
        }

        private static bool HandleNullValue(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            string? text,
            out object? value)
        {
            value = null;

            if (type.IsValueType)
            {
                if (string.IsNullOrEmpty(text))
                {
                    value = DefaultValueFactory.GetDefaultValue(type);
                    return true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(text))
                {
                    value = null;
                    return true;
                }
            }
            return false;
        }
    }

    internal class XamlMemberPropertyAdapter : IPropertyAdapter
    {
        private Type? declaringType;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        private Type? propertyType;

        public XamlMemberPropertyAdapter(IXamlMember xamlMember)
        {
            XamlMember = xamlMember;
            PropertyName = xamlMember.Name;
        }


        public XamlMemberPropertyAdapter(
            IXamlMember xamlMember,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type propertyType,
            Type declaringType) : this(xamlMember)
        {
            this.propertyType = propertyType;
            this.declaringType = declaringType;
        }

        public IXamlMember XamlMember { get; }

        public Type DeclaringType
        {
            get
            {
                if (declaringType == null)
                {
                    try { declaringType = GetUnderlyingType(XamlMember.TargetType); }
                    catch { declaringType = typeof(void); }
                }
                return declaringType;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type PropertyType
        {
            get
            {
                if (propertyType == null)
                {
                    try { propertyType = GetUnderlyingType(XamlMember.Type); }
                    catch { propertyType = typeof(void); }
                }
                return propertyType;
            }
        }

        public string PropertyName { get; }

        public bool SetValue(object obj, object? value)
        {
            try
            {
                if (XamlMember.IsReadOnly) return false;

                XamlMember.SetValue(obj, value);
                return true;
            }
            catch { }
            return false;
        }

        public bool TryCreateValueFromString(string? text, out object? value)
        {
            try
            {
                if (ResourceBinding.TryHandleKnownTypeValue(PropertyType, text, out value)) return true;

                value = XamlMember.Type.CreateFromString(text);
                return true;
            }
            catch { }

            value = null;
            return false;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        private static unsafe Type GetUnderlyingType(IXamlType xamlType)
        {
            Type? type = null;
            try { type = xamlType.UnderlyingType; }
            catch
            {
                try { type = Projections.FindCustomTypeForAbiTypeName(xamlType.FullName); }
                catch { }
            }

            if (type == null)
            {
                ABI.System.Type abiType = default;
                try
                {
                    ref var tmp = ref Unsafe.AsRef<(nint, int)>(&abiType);

                    tmp.Item1 = MarshalString.FromManaged(xamlType.FullName);
                    tmp.Item2 = 1;

                    type = ABI.System.Type.FromAbi(abiType);
                }
                catch { }
                finally
                {
                    ABI.System.Type.DisposeAbi(abiType);
                }
            }

            if (type == null)
            {
                type = typeof(void);
            }
#pragma warning disable IL2073
            return type;
#pragma warning restore IL2073
        }
    }

    internal class DependencyPropertyAdapter : IPropertyAdapter
    {
        public DependencyPropertyAdapter(
            DependencyProperty dependencyProperty,
            string propertyName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type propertyType,
            Type declaringType)
        {
            DependencyProperty = dependencyProperty;
            PropertyName = propertyName;
            PropertyType = propertyType;
            DeclaringType = declaringType;
        }

        public DependencyProperty DependencyProperty { get; }

        public string PropertyName { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type PropertyType { get; }

        public Type DeclaringType { get; }

        public bool SetValue(object obj, object? value)
        {
            if (obj is DependencyObject dependencyObject)
            {
                try
                {
                    dependencyObject.SetValue(DependencyProperty, value);
                    return true;
                }
                catch { }
            }

            return false;
        }

        public bool TryCreateValueFromString(string? text, out object? value)
        {
            try
            {
                if (IPropertyAdapter.HandleKnownTypeValue(PropertyType, text, out value)) return true;
                value = XamlBindingHelper.ConvertValue(PropertyType, text);
                return true;
            }
            catch { }

            value = null;
            return false;
        }
    }
}
