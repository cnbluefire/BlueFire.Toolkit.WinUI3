using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class ResourceBindingManager
    {
        private static long clearTimeTick;
        private static List<(CacheKey Key, ResourceBinding Value)> allBindings = new();

        public static ResourceBinding GetOrAdd(object obj, IPropertyAdapter propertyAdapter)
        {
            var property = $"{propertyAdapter.DeclaringType.FullName}.{propertyAdapter.PropertyName}";

            lock (allBindings)
            {
                RemoveInvalidBindings(false);

                for (int i = 0; i < allBindings.Count; i++)
                {
                    if (allBindings[i].Key.Equals(obj, property))
                    {
                        return allBindings[i].Value;
                    }
                }

                var binding = new ResourceBinding(obj, propertyAdapter);
                var cacheKey = new CacheKey(obj, property);
                allBindings.Add((cacheKey, binding));

                return binding;
            }
        }

        public static bool Remove(object obj, IPropertyAdapter propertyAdapter)
        {
            var property = $"{propertyAdapter.DeclaringType.FullName}.{propertyAdapter.PropertyName}";

            lock (allBindings)
            {
                RemoveInvalidBindings(false);
                for (int i = 0; i < allBindings.Count; i++)
                {
                    if (allBindings[i].Key.Equals(obj, property))
                    {
                        allBindings.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }
        }

        internal static IReadOnlyCollection<ResourceBinding>? RemoveInvalidBindings(bool tryCreateAliveCollection)
        {
            List<ResourceBinding>? list = null;
            lock (allBindings)
            {
                if (allBindings.Count == 0) return null;

                var curTick = Stopwatch.GetTimestamp();
                var durMillis = (curTick - clearTimeTick) / (Stopwatch.Frequency / 1000);

                if (durMillis < 0 || durMillis > 60 * 1000)
                {
                    // 60 seconds
                    clearTimeTick = curTick;

                    if (tryCreateAliveCollection) list = new List<ResourceBinding>();

                    for (int i = allBindings.Count - 1; i >= 0; i--)
                    {
                        if (!allBindings[i].Key.IsAlive)
                        {
                            allBindings.RemoveAt(i);
                        }
                        else if (tryCreateAliveCollection)
                        {
                            list!.Add(allBindings[i].Value);
                        }
                    }
                }
            }

            return list;
        }

        public static IReadOnlyCollection<ResourceBinding> GetResourceBindings()
        {
            lock (allBindings)
            {
                return RemoveInvalidBindings(true) ??
                    allBindings.Where(c => c.Key.IsAlive).Select(c => c.Value).ToArray();
            }
        }

        private class CacheKey : IEquatable<CacheKey>
        {
            private readonly string property;
            private WeakReference weakReference;
            private int hashCode;

            public CacheKey(object? obj, string property)
            {
                weakReference = new WeakReference(obj);
                hashCode = HashCode.Combine(obj, property);
                this.property = property;
            }

            public bool IsAlive => weakReference.Target != null;

            public bool Equals(object obj, string property)
            {
                return object.Equals(weakReference.Target, obj) && object.Equals(this.property, property);
            }

            public bool Equals(CacheKey? other)
            {
                var target1 = weakReference?.Target;
                var target2 = other?.weakReference?.Target;

                return object.Equals(target1, target2) && object.Equals(property, other?.property);
            }

            public override bool Equals(object? obj)
            {
                return obj is CacheKey obj1 && Equals(obj1);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public static bool operator ==(CacheKey left, CacheKey right)
            {
                if (left.weakReference is null && right.weakReference is null) return true;
                else if (left.weakReference is not null && right.weakReference is not null) return left.Equals(right);
                else return false;
            }
            public static bool operator !=(CacheKey left, CacheKey right)
            {
                return !(left == right);
            }
        }
    }
}
