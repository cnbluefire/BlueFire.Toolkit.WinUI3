using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class ResourceBindingManager
    {
        private static ConditionalWeakTable<object, ConcurrentDictionary<string, ResourceBinding>>? resourceBindings;
        private static ConditionalWeakTable<object, ConcurrentDictionary<string, ResourceBinding>> ResourceBindings => resourceBindings ??= new();

        public static ResourceBinding GetOrAdd(object obj, IPropertyAdapter propertyAdapter)
        {
            var property = propertyAdapter.GetPropertyKey();

            var bindings = ResourceBindings.GetOrCreateValue(obj);

            lock (bindings)
            {
                return bindings.GetOrAdd(
                    property,
                    (p, arg) => new ResourceBinding(arg.obj, arg.propertyAdapter),
                    (obj, propertyAdapter));
            }
        }

        public static bool Remove(object obj, IPropertyAdapter propertyAdapter)
        {
            var property = propertyAdapter.GetPropertyKey();

            if (ResourceBindings.TryGetValue(obj, out var bindings))
            {
                return bindings.TryRemove(property, out _);
            }

            return false;
        }

        public static IReadOnlyCollection<ResourceBinding> GetResourceBindings()
        {
            return ResourceBindings
                .SelectMany(c => c.Value.Select(x => x.Value))
                .ToArray();
        }
    }
}
