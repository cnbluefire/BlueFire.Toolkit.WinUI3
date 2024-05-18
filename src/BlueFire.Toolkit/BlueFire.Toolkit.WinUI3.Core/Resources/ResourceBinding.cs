using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal class ResourceBinding
    {
        private WeakReference weakObj;
        private readonly IPropertyAdapter propertyAdapter;

        internal ResourceBinding(object targetObject, IPropertyAdapter propertyAdapter)
        {
            weakObj = new WeakReference(targetObject);
            this.propertyAdapter = propertyAdapter;
        }

        public string? ResourceUri { get; set; }

        public void SetValue(string? value)
        {
            if (weakObj.Target is object targetObj)
            {
                if (propertyAdapter.TryCreateValueFromString(value, out var value2))
                {
                    propertyAdapter.SetValue(targetObj, value2);
                }
            }
        }

        public static bool TryHandleKnownTypeValue(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            string? text,
            out object? value)
        {
            if (string.IsNullOrEmpty(text))
            {
                value = DefaultValueFactory.GetDefaultValue(type);
                return true;
            }

            if (type == typeof(string))
            {
                value = text;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryChangeType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type,
            string? text,
            out object? value)
        {
            if (TryHandleKnownTypeValue(type, text, out value))
            {
                return true;
            }

            try
            {
                value = XamlBindingHelper.ConvertValue(type, text);
                return true;
            }
            catch { }

            value = null;
            return false;
        }

    }
}
