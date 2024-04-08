using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;


namespace BlueFire.Toolkit.WinUI3.Extensions
{
    /// <summary>
    /// Application package information.
    /// </summary>
    public static class PackageInfo
    {
        private static bool initialized;
        private static object staticLocker = new object();
        private static string? applicationUserModelId;
        private static string? packageFamilyName;
        private static string? packageRelativeApplicationId;
        private static bool isPackagedApp;
        private static XDocument? appxmanifestDoc;
        private static SemaphoreSlim asyncLocker = new SemaphoreSlim(1, 1);

        public static bool IsPackagedApp
        {
            get
            {
                EnsureInfo();
                return isPackagedApp;
            }
        }

        public static string ApplicationUserModelId
        {
            get
            {
                EnsureInfo();
                return applicationUserModelId!;
            }
        }

        public static string PackageFamilyName
        {
            get
            {
                EnsureInfo();
                return packageFamilyName!;
            }
        }

        public static string PackageRelativeApplicationId
        {
            get
            {
                EnsureInfo();
                return packageRelativeApplicationId!;
            }
        }

        private static void EnsureInfo()
        {
            if (!initialized)
            {
                lock (staticLocker)
                {
                    if (!initialized)
                    {
                        var err = GetCurrentApplicationUserModelId(out var amuid);

                        isPackagedApp = err != WIN32_ERROR.APPMODEL_ERROR_NO_APPLICATION;

                        if (!string.IsNullOrEmpty(amuid) && TryParseApplicationUserModelId(amuid, out var pfn, out var appId))
                        {
                            applicationUserModelId = amuid;
                            packageFamilyName = pfn;
                            packageRelativeApplicationId = appId;
                        }
                        else
                        {
                            applicationUserModelId = "";
                            packageFamilyName = "";
                            packageRelativeApplicationId = "";
                        }
                        initialized = true;
                    }
                }
            }
        }

        internal static async Task<string> GetApplicationVisualElementsAttribute(XName attribute, CancellationToken cancellationToken)
        {
            if (IsPackagedApp && !string.IsNullOrEmpty(packageRelativeApplicationId))
            {
                if (appxmanifestDoc == null)
                {
                    await asyncLocker.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        if (appxmanifestDoc == null)
                        {
                            var folder = Package.Current.InstalledLocation;

                            var path = Path.Combine(folder.Path, "AppxManifest.xml");
                            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);

                            appxmanifestDoc = XDocument.Parse(content);
                        }
                    }
                    finally
                    {
                        asyncLocker.Release();
                    }
                }

                if (appxmanifestDoc != null)
                {
                    var ns = appxmanifestDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
                    var uapNsValue = appxmanifestDoc.Root?.Attributes().FirstOrDefault(c => c.IsNamespaceDeclaration && string.Equals(c.Value, "http://schemas.microsoft.com/appx/manifest/uap/windows10", StringComparison.OrdinalIgnoreCase))?
                        .Value;

                    var uapNs = !string.IsNullOrEmpty(uapNsValue) ? XNamespace.Get(uapNsValue) : XNamespace.None;

                    return appxmanifestDoc.Root?.Elements(ns + "Applications")
                        .Elements(ns + "Application")
                        .FirstOrDefault(c => c.Attribute("Id")?.Value == packageRelativeApplicationId)?
                        .Element(uapNs + "VisualElements")?
                        .Attribute(attribute)?
                        .Value ?? string.Empty;
                }
            }

            return string.Empty;
        }

        internal unsafe static bool TryParseApplicationUserModelId(
            string applicationUserModelId,
            [NotNullWhen(true)] out string? packageFamilyName,
            [NotNullWhen(true)] out string? packageRelativeApplicationId)
        {
            packageFamilyName = null;
            packageRelativeApplicationId = null;

            if (string.IsNullOrEmpty(applicationUserModelId)) return false;

            uint pfnLength = 0;
            uint appIdLength = 0;

            var err = PInvoke.ParseApplicationUserModelId(applicationUserModelId, ref pfnLength, (char*)0, ref appIdLength, (char*)0);

            if (err == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            {
                var pfnBuffer = stackalloc char[(int)pfnLength];

                var appIdBuffer = stackalloc char[(int)appIdLength];

                err = PInvoke.ParseApplicationUserModelId(applicationUserModelId, ref pfnLength, pfnBuffer, ref appIdLength, appIdBuffer);

                if (err == WIN32_ERROR.ERROR_SUCCESS)
                {
                    packageFamilyName = new string(pfnBuffer, 0, (int)pfnLength - 1);
                    packageRelativeApplicationId = new string(appIdBuffer, 0, (int)appIdLength - 1);

                    return true;
                }
            }

            return false;
        }

        internal unsafe static WIN32_ERROR GetCurrentApplicationUserModelId(out string? applicationUserModelId)
        {
            applicationUserModelId = null;

            uint length = 0;

            var err = PInvoke.GetCurrentApplicationUserModelId(ref length, (char*)0);

            if (err == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
            {
                var amuidBuffer = stackalloc char[(int)length];

                err = PInvoke.GetCurrentApplicationUserModelId(ref length, amuidBuffer);

                if (err == WIN32_ERROR.ERROR_SUCCESS)
                {
                    applicationUserModelId = new string(amuidBuffer, 0, (int)length - 1);
                }
            }

            return err;
        }
    }
}
