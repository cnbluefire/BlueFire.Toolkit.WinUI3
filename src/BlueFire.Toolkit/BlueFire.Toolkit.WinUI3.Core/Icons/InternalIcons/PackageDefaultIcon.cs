using BlueFire.Toolkit.WinUI3.Compositions;
using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.Resources;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BlueFire.Toolkit.WinUI3.Icons.InternalIcons
{
    internal class PackageDefaultIcon : ComposedIcon
    {
        private const uint defaultDpi = 288; // 300%

        private static Dictionary<CacheKey, string> cachedInternalIconPaths = new Dictionary<CacheKey, string>();
        private static Dictionary<string, RandomAccessStreamIcon> cachedInternalIcons = new Dictionary<string, RandomAccessStreamIcon>();
        private static Dictionary<CacheKey, PackageDefaultIcon> cachedIcons = new Dictionary<CacheKey, PackageDefaultIcon>();

        private readonly ApplicationTheme requestedTheme;
        private readonly bool highContrast;

        internal PackageDefaultIcon(ApplicationTheme requestedTheme, bool highContrast)
        {
            this.requestedTheme = requestedTheme;
            this.highContrast = highContrast;
        }

        protected internal override nint GetIconCore(SizeInt32 size)
        {
            var key = new CacheKey(requestedTheme, highContrast);
            if (TryGetIcon(key, out var internalIcon))
            {
                return internalIcon.GetIconCore(size);
            }

            if (TryGetIcon(key, out internalIcon))
            {
                return internalIcon.GetIconCore(size);
            }

            string? logoFilePath = null;

            var memoryStream = WindowsCompositionHelper.DispatcherQueue.RunSync(async () =>
            {
                logoFilePath = await ResourceLoader.GetAppLogoFilePathAsync(defaultDpi, requestedTheme, highContrast, default);

                if (!string.IsNullOrEmpty(logoFilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(logoFilePath);
                    using (var stream = await file.OpenReadAsync())
                    {
                        var _memoryStream = new InMemoryRandomAccessStream();
                        await RandomAccessStream.CopyAsync(stream, _memoryStream);
                        return _memoryStream;
                    }
                }
                return null;
            });

            if (memoryStream != null)
            {
                internalIcon = new RandomAccessStreamIcon(memoryStream);
                cachedInternalIconPaths[key] = logoFilePath!;
                cachedInternalIcons[logoFilePath!] = internalIcon;
                return internalIcon.GetIconCore(size);
            }

            return 0;
        }

        private static bool TryGetIcon(CacheKey key, [NotNullWhen(true)] out RandomAccessStreamIcon? result)
        {
            result = null;

            return cachedInternalIconPaths.TryGetValue(key, out var path)
                && cachedInternalIcons.TryGetValue(path, out result);
        }

        internal static PackageDefaultIcon GetPackageDefaultIcon(ApplicationTheme requestedTheme, bool highContrast)
        {
            var key = new CacheKey(requestedTheme, highContrast);
            if (cachedIcons.TryGetValue(key, out var icon)) return icon;

            lock (cachedIcons)
            {
                if (cachedIcons.TryGetValue(key, out icon)) return icon;

                icon = new PackageDefaultIcon(requestedTheme, highContrast);
                cachedIcons[key] = icon;
                return icon;
            }
        }

        private struct CacheKey : IEquatable<CacheKey>
        {
            private int hashCode;

            public CacheKey(ApplicationTheme requestedTheme, bool highContrast)
            {
                RequestedTheme = requestedTheme;
                HighContrast = highContrast;

                hashCode = HashCode.Combine(requestedTheme, highContrast);
            }

            public ApplicationTheme RequestedTheme { get; }

            public bool HighContrast { get; }

            public bool Equals(CacheKey other)
            {
                return hashCode == other.hashCode
                    && RequestedTheme == other.RequestedTheme
                    && HighContrast == other.HighContrast;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj is CacheKey obj1 && Equals(obj1);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }
    }
}
