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


namespace BlueFire.Toolkit.ApplicationModel
{
    /// <summary>
    /// Application package information.
    /// </summary>
    public class ApplicationInformation
    {
        private static Lock locker = new Lock();
        private static ApplicationInformation? current;
        private static XDocument? appxmanifestDoc;
        private static SemaphoreSlim asyncLocker = new SemaphoreSlim(1, 1);

        private ApplicationInformation(string applicationUserModelId)
        {
            ApplicationUserModelId = applicationUserModelId;
            PackageRelativeApplicationId = "";
            if (TryParseApplicationUserModelId(applicationUserModelId, out var pfn, out var appId))
            {
                PackageRelativeApplicationId = appId;
            }
        }

        public string ApplicationUserModelId { get; }

        public string PackageRelativeApplicationId { get; }

        public static ApplicationInformation? Current
        {
            get
            {
                if (current == null && PackageInformation.Current != null)
                {
                    lock (locker)
                    {
                        if (current == null && PackageInformation.Current != null)
                        {
                            current = CreateCurrentApplicationInformation();
                        }
                    }
                }
                return current;
            }
        }

        private static ApplicationInformation? CreateCurrentApplicationInformation()
        {
            if (PackageInformation.Current == null) return null;

            var err = GetCurrentApplicationUserModelId(out var aumid);

            if (err == WIN32_ERROR.ERROR_SUCCESS
                && !string.IsNullOrEmpty(aumid))
            {
                return new ApplicationInformation(aumid);
            }
            return null;
        }

        internal static async Task<string> GetApplicationVisualElementsAttribute(XName attribute, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(Current?.PackageRelativeApplicationId))
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
                        .FirstOrDefault(c => c.Attribute("Id")?.Value == Current.PackageRelativeApplicationId)?
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
