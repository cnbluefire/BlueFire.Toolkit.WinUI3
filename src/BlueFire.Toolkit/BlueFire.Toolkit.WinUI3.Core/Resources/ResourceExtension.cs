using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    [MarkupExtensionReturnType(ReturnType = typeof(object))]
    public partial class ResourceExtension : MarkupExtension
    {
        public string? Uri { get; set; }

        protected override object? ProvideValue(IXamlServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provideValueTarget
                && TryUnwrap(
                    provideValueTarget,
                    out var targetObject,
                    out var propertyName,
                    out var propertyDeclaringType,
                    out var propertyType))
            {
                var propAdapter = PropertyAdapterManager.GetOrCreateAdapter(
                    propertyName,
                    propertyDeclaringType,
                    propertyType);

                if (propAdapter != null)
                {
                    var binding = ResourceBindingManager.GetOrAdd(provideValueTarget.TargetObject, propAdapter);
                    binding.ResourceUri = Uri;
                }

                if (ResourceManagerFactory.TryGetResource(propertyType, Uri, out var value))
                {
                    return value;
                }

                if (propAdapter != null)
                {
                    return DefaultValueFactory.GetDefaultValue(propAdapter);
                }
                return DefaultValueFactory.GetDefaultValue(propertyType);
            }

            return null;
        }

        private static bool TryUnwrap(
            IProvideValueTarget provideValueTarget,
            out object? targetObject,
            [NotNullWhen(true)] out string? propertyName,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.NonPublicFields
                | DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.NonPublicProperties), NotNullWhen(true)] out Type? propertyDeclaringType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor), NotNullWhen(true)] out Type? propertyType)

        {
            targetObject = provideValueTarget.TargetObject;
            propertyName = null;
            propertyDeclaringType = null;
            propertyType = null;

            if (provideValueTarget.TargetProperty is ProvideValueTargetProperty targetProperty)
            {
                propertyName = targetProperty.Name;
#pragma warning disable IL2072
                propertyDeclaringType = targetProperty.DeclaringType;
                propertyType = targetProperty.Type;
#pragma warning restore IL2072

                return true;
            }
            return false;
        }
    }
}