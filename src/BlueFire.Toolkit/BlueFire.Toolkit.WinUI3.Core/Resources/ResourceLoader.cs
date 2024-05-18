using BlueFire.Toolkit.WinUI3.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32.Foundation;
using PInvoke = Windows.Win32.PInvoke;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class ResourceLoader
    {
        internal static async Task<string?> GetAppLogoFilePathAsync(uint dpi, ApplicationTheme requestedTheme, bool highContrast, CancellationToken cancellationToken)
        {
            if (PackageInfo.IsPackagedApp)
            {
                var logoName = await PackageInfo.GetApplicationVisualElementsAttribute("Square44x44Logo", cancellationToken);
                if (!string.IsNullOrEmpty(logoName))
                {
                    return GetFileResourcePath(logoName, dpi, requestedTheme, highContrast);
                }
            }

            return null;
        }


        internal static string? GetFileResourcePath(string resourceName, uint dpi, ApplicationTheme requestedTheme, bool highContrast)
        {
            var context = ResourceManagerFactory.ResourceManager.CreateResourceContext();
            context.QualifierValues[KnownResourceQualifierName.TargetSize] = "128";

            InitializeResourceContext(context, requestedTheme, highContrast);

            var value = ResourceManagerFactory.ResourceManager.MainResourceMap.TryGetValue("Files\\" + resourceName, context);
            if (value == null
                || (value.QualifierValues.TryGetValue(KnownResourceQualifierName.TargetSize, out var resultTargetSize)
                    && int.TryParse(resultTargetSize, out var resultTargetSizeValue)
                    && resultTargetSizeValue < 128))
            {
                context = ResourceManagerFactory.ResourceManager.CreateResourceContext();
                context.QualifierValues[KnownResourceQualifierName.Scale] = (dpi / 96d * 100).ToString("0");

                InitializeResourceContext(context, requestedTheme, highContrast);

                var value2 = ResourceManagerFactory.ResourceManager.MainResourceMap.TryGetValue("Files\\" + resourceName, context);
                if (value2 != null)
                {
                    value = value2;
                }
            }

            if (value != null && value.Kind == ResourceCandidateKind.FilePath)
            {
                return value.ValueAsString;
            }

            return null;
        }

        private static void InitializeResourceContext(ResourceContext context, ApplicationTheme requestedTheme, bool highContrast)
        {
            if (requestedTheme == ApplicationTheme.Light)
            {
                context.QualifierValues[KnownResourceQualifierName.Contrast] = highContrast ? "white" : "standard";
                context.QualifierValues[KnownResourceQualifierName.Theme] = "light";
                context.QualifierValues["AlternateForm"] = "LIGHTUNPLATED";
            }
            else
            {
                context.QualifierValues[KnownResourceQualifierName.Contrast] = highContrast ? "black" : "standard";
                context.QualifierValues[KnownResourceQualifierName.Theme] = "dark";
                context.QualifierValues["AlternateForm"] = "UNPLATED";
            }
        }
    }
}
