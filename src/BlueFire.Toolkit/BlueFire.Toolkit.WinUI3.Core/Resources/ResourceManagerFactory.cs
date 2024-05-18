using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Windows.Globalization;
using Windows.UI.ViewManagement;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class ResourceManagerFactory
    {
        private static DispatcherQueue? dispatcherQueue;
        private static object locker = new object();
        private static ResourceManager? resourceManager;
        private static AccessibilitySettings? accessibilitySettings;
        private static ResourceContext? currentContext;
        private static string? userDefaultLocaleName;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (resourceManager == null)
                {
                    lock (locker)
                    {
                        if (resourceManager == null)
                        {
                            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                            resourceManager = new ResourceManager();
                        }
                    }
                }

                return resourceManager;
            }
        }

        internal static IDictionary<string, string> QualifierValuesOverride { get; } = new ConcurrentDictionary<string, string>()
        {
            [KnownResourceQualifierName.Language] = UserDefaultLocaleName
        };

        internal static ResourceContext CreateResourceContext()
        {
            var context = ResourceManager.CreateResourceContext();

            UpdateQualifierValues(context.QualifierValues);

            return context;
        }

        internal static void UpdateQualifierValues(IDictionary<string, string> qualifierValues)
        {
            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.Contrast, out var contrast) && !string.IsNullOrEmpty(contrast))
                qualifierValues[KnownResourceQualifierName.Contrast] = contrast;
            else try { UpdateContrastQualifier(qualifierValues); } catch { }


            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.DeviceFamily, out var deviceFamily) && !string.IsNullOrEmpty(deviceFamily))
                qualifierValues[KnownResourceQualifierName.DeviceFamily] = deviceFamily;
            else try { UpdateDeviceFamilyQualifier(qualifierValues); } catch { }


            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.HomeRegion, out var homeRegion) && !string.IsNullOrEmpty(homeRegion))
                qualifierValues[KnownResourceQualifierName.HomeRegion] = homeRegion;
            else try { UpdateHomeRegionQualifier(qualifierValues); } catch { }

            try { UpdateLanguageAndLayoutDirectionQualifiers(qualifierValues); } catch { }
            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.Language, out var language) && !string.IsNullOrEmpty(language))
                qualifierValues[KnownResourceQualifierName.Language] = language;
            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.LayoutDirection, out var layoutDirection) && !string.IsNullOrEmpty(layoutDirection))
                qualifierValues[KnownResourceQualifierName.LayoutDirection] = layoutDirection;


            qualifierValues[KnownResourceQualifierName.Scale] = "100";
            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.Scale, out var scale) && !string.IsNullOrEmpty(scale))
                qualifierValues[KnownResourceQualifierName.Scale] = scale;


            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.Theme, out var theme) && !string.IsNullOrEmpty(theme))
                qualifierValues[KnownResourceQualifierName.Theme] = theme;
            else try { UpdateThemeQualifier(qualifierValues); } catch { }


            if (QualifierValuesOverride.TryGetValue(KnownResourceQualifierName.Custom, out var custom) && !string.IsNullOrEmpty(custom))
                qualifierValues[KnownResourceQualifierName.Custom] = custom;
        }

        internal static void RaiseQualifierValuesChanged()
        {
            var context = GetCurrentContext(true);
            foreach (var binding in ResourceBindingManager.GetResourceBindings())
            {
                UpdateResourceBindingValue(binding, context);
            }
        }

        internal static ResourceCandidate? GetResource(string? resourceUri, ResourceContext context)
        {
            if (string.IsNullOrEmpty(resourceUri)) return null;

            bool isAbsoluteUri = false;
            if (resourceUri.StartsWith("ms-resource://", StringComparison.OrdinalIgnoreCase))
            {
                isAbsoluteUri = true;
                resourceUri = resourceUri["ms-resource://".Length..];
            }

            var resourceMap = ResourceManager.MainResourceMap;

            if (!isAbsoluteUri)
            {
                var subtreeNameStartIdx = 0;
                if (resourceUri.Length > 0 && resourceUri[0] == '/') subtreeNameStartIdx = 1;

                var subtreeNameEndIdx = resourceUri.IndexOf('/', subtreeNameStartIdx);

                if (subtreeNameEndIdx == -1)
                {
                    resourceMap = resourceMap.TryGetSubtree("Resources");
                }
                else if (subtreeNameEndIdx >= 0 && subtreeNameEndIdx - subtreeNameStartIdx > 0)
                {
                    var subtreeName = resourceUri[subtreeNameStartIdx..subtreeNameEndIdx];
                    var subTreeMap = resourceMap.TryGetSubtree(subtreeName);
                    if (subTreeMap != null)
                    {
                        resourceMap = subTreeMap;
                        resourceUri = resourceUri[subtreeNameEndIdx..];
                    }
                    else
                    {
                        resourceMap = resourceMap.TryGetSubtree("Resources");
                    }
                }
            }

            if (resourceMap != null)
            {
                int lastPointIndex = 0;

                do
                {
                    if (lastPointIndex != 0)
                    {
                        resourceUri = $"{resourceUri[0..lastPointIndex]}/{resourceUri[(lastPointIndex + 1)..]}";
                    }

                    var resourceCandidate = resourceMap.TryGetValue(resourceUri, context);
                    if (resourceCandidate != null)
                    {
                        return resourceCandidate;
                    }
                    lastPointIndex = resourceUri.LastIndexOf('.');

                } while (lastPointIndex > 0 && lastPointIndex < resourceUri.Length - 1);
            }

            return null;
        }

        internal static bool TryGetResource(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type propertyType,
            string? resourceUri,
            out object? value)
        {
            value = null;

            var candidate = GetResource(resourceUri, GetCurrentContext());
            return candidate != null
                && candidate.Kind == ResourceCandidateKind.String
                && ResourceBinding.TryChangeType(propertyType, candidate.ValueAsString, out value);
        }

        private static void UpdateResourceBindingValue(ResourceBinding binding, ResourceContext context)
        {
            if (TryGetResource(binding.ResourceUri, context, out var value))
            {
                binding.SetValue(value);
            }
            else
            {
                binding.SetValue(null);
            }
        }

        private static ResourceContext GetCurrentContext(bool createNew = false)
        {
            if (createNew || currentContext == null)
            {
                lock (locker)
                {
                    if (createNew || currentContext == null)
                    {
                        currentContext = CreateResourceContext();
                    }
                }
            }


            return currentContext;
        }

        private static bool TryGetResource(string? resourceUri, ResourceContext context, out string value)
        {
            value = string.Empty;

            if (string.IsNullOrEmpty(resourceUri)) return false;

            bool isAbsoluteUri = false;
            if (resourceUri.StartsWith("ms-resource://", StringComparison.OrdinalIgnoreCase))
            {
                isAbsoluteUri = true;
                resourceUri = resourceUri["ms-resource://".Length..];
            }

            var resourceMap = ResourceManager.MainResourceMap;

            if (!isAbsoluteUri)
            {
                var subtreeNameStartIdx = 0;
                if (resourceUri.Length > 0 && resourceUri[0] == '/') subtreeNameStartIdx = 1;

                var subtreeNameEndIdx = resourceUri.IndexOf('/', subtreeNameStartIdx);

                if (subtreeNameEndIdx == -1)
                {
                    resourceMap = resourceMap.TryGetSubtree("Resources");
                }
                else if (subtreeNameEndIdx >= 0 && subtreeNameEndIdx - subtreeNameStartIdx > 0)
                {
                    var subtreeName = resourceUri[subtreeNameStartIdx..subtreeNameEndIdx];
                    var subTreeMap = resourceMap.TryGetSubtree(subtreeName);
                    if (subTreeMap != null)
                    {
                        resourceMap = subTreeMap;
                        resourceUri = resourceUri[subtreeNameEndIdx..];
                    }
                    else
                    {
                        resourceMap = resourceMap.TryGetSubtree("Resources");
                    }
                }
            }

            if (resourceMap != null)
            {
                int lastPointIndex = 0;

                do
                {
                    if (lastPointIndex != 0)
                    {
                        resourceUri = $"{resourceUri[0..lastPointIndex]}/{resourceUri[(lastPointIndex + 1)..]}";
                    }

                    var resourceCandidate = resourceMap.TryGetValue(resourceUri, context);
                    if (resourceCandidate != null && resourceCandidate.Kind == ResourceCandidateKind.String)
                    {
                        value = resourceCandidate.ValueAsString;
                        return true;
                    }
                    lastPointIndex = resourceUri.LastIndexOf('.');

                } while (lastPointIndex > 0 && lastPointIndex < resourceUri.Length - 1);
            }

            return false;
        }

        private static void UpdateContrastQualifier(IDictionary<string, string> qualifierValues) =>
            qualifierValues[KnownResourceQualifierName.Contrast] =
                EnsureAccessibilitySettings().SystemHighContrastTheme switch
                {
                    SystemHighContrastTheme.HighContrastNone => "standard",
                    SystemHighContrastTheme.HighContrast or SystemHighContrastTheme.HighContrastCustom => "high",
                    SystemHighContrastTheme.HighContrastBlack => "black",
                    SystemHighContrastTheme.HighContrastWhite => "white",
                    _ => throw new NotSupportedException()
                };

        private static void UpdateDeviceFamilyQualifier(IDictionary<string, string> qualifierValues)
        {
            var deviceFamilyInfo = DeviceFamilyInfo.GetDeviceFamilyInfo();

            string qualifierValue = deviceFamilyInfo.DeviceFamily;

            if (qualifierValue.StartsWith("Windows."))
            {
                qualifierValue = qualifierValue[8..].Replace('.', '_');
            }

            qualifierValues[KnownResourceQualifierName.DeviceFamily] = qualifierValue;
        }

        private static void UpdateHomeRegionQualifier(IDictionary<string, string> qualifierValues)
        {
            var region = new Windows.Globalization.GeographicRegion();

            var qualifierValue = region.CodeTwoLetter;
            if (qualifierValue == "ZZ")
            {
                qualifierValue = region.CodeThreeDigit;
                if (qualifierValue == "999")
                {
                    qualifierValue = "001";
                }
            }

            qualifierValues[KnownResourceQualifierName.HomeRegion] = qualifierValue;
        }

        private static void UpdateLanguageAndLayoutDirectionQualifiers(IDictionary<string, string> qualifierValues)
        {
            var primaryLanguageName = ApplicationLanguages.Languages[0];
            var primaryLanguage = new Language(primaryLanguageName);

            qualifierValues[KnownResourceQualifierName.Language] = primaryLanguage.LanguageTag;
            qualifierValues[KnownResourceQualifierName.LayoutDirection] = primaryLanguage.LayoutDirection switch
            {
                LanguageLayoutDirection.Ltr => "LTR",
                LanguageLayoutDirection.Rtl => "RTL",
                LanguageLayoutDirection.TtbLtr => "TTBLTR",
                LanguageLayoutDirection.TtbRtl => "TTBRTL",
                _ => throw new NotSupportedException()
            };
        }

        private static void UpdateThemeQualifier(IDictionary<string, string> qualifierValues) =>
            qualifierValues[KnownResourceQualifierName.Theme] =
                Application.Current.RequestedTheme switch
                {
                    ApplicationTheme.Light => "light",
                    _ => "dark"
                };


        private static AccessibilitySettings EnsureAccessibilitySettings()
        {
            if (accessibilitySettings == null)
            {
                lock (locker)
                {
                    if (accessibilitySettings == null)
                    {
                        accessibilitySettings = new AccessibilitySettings();
                        accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;
                    }
                }
            }

            return accessibilitySettings;
        }

        private static void AccessibilitySettings_HighContrastChanged(AccessibilitySettings? sender, object? args)
        {
            if (dispatcherQueue!.HasThreadAccess)
            {
                RaiseQualifierValuesChanged();
            }
            else
            {
                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => RaiseQualifierValuesChanged());
            }
        }


        internal static string UserDefaultLocaleName
        {
            get
            {
                if (string.IsNullOrEmpty(userDefaultLocaleName))
                {
                    lock (locker)
                    {
                        if (string.IsNullOrWhiteSpace(userDefaultLocaleName))
                        {
                            userDefaultLocaleName = GetUserDefaultLocaleNameNative();
                        }
                    }
                }

                return userDefaultLocaleName;

                static unsafe string GetUserDefaultLocaleNameNative()
                {
                    const int LOCALE_NAME_MAX_LENGTH = 85;

                    char* lpLocaleName = stackalloc char[LOCALE_NAME_MAX_LENGTH];
                    var length = Windows.Win32.PInvoke.GetUserDefaultLocaleName(lpLocaleName, LOCALE_NAME_MAX_LENGTH);
                    if (length == 0)
                    {
                        return string.Empty;
                    }

                    return new string(lpLocaleName, 0, length - 1);
                }
            }
        }
    }
}