using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.Concurrent;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal static class SystemQualifierValues
    {
        private static Lock locker = new Lock();
        private static ConcurrentDictionary<string, string>? values;
        private static SystemSettings? systemSettings;

        public static IReadOnlyDictionary<string, string> Values
        {
            get
            {
                if (values == null)
                {
                    lock (locker)
                    {
                        if (values == null)
                        {
                            values = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            EnsureSystemSettings();
                            UpdateQualifierValues(null);
                        }
                    }
                }
                return values;
            }
        }

        private static SystemSettings EnsureSystemSettings()
        {
            if (systemSettings == null)
            {
                lock (locker)
                {
                    if (systemSettings == null)
                    {
                        systemSettings = new SystemSettings();
                        systemSettings.HighContrastChanged += (s, a) => RaiseQualifierValuesChanged(KnownResourceQualifierName.Contrast);
                        systemSettings.HomeRegionChanged += (s, a) => RaiseQualifierValuesChanged(KnownResourceQualifierName.HomeRegion);
                    }
                }
            }

            return systemSettings;
        }

        private static void UpdateQualifierValues(string? qualifierName)
        {
            var values = SystemQualifierValues.values;

            if (values != null)
            {
                if (string.IsNullOrEmpty(qualifierName) || string.Equals(KnownResourceQualifierName.Contrast, qualifierName))
                {
                    values![KnownResourceQualifierName.Contrast] = EnsureSystemSettings().SystemHighContrastTheme switch
                    {
                        SystemHighContrastTheme.HighContrastNone => "standard",
                        SystemHighContrastTheme.HighContrast or SystemHighContrastTheme.HighContrastCustom => "high",
                        SystemHighContrastTheme.HighContrastBlack => "black",
                        SystemHighContrastTheme.HighContrastWhite => "white",
                        _ => throw new NotSupportedException()
                    };
                }
                if (string.IsNullOrEmpty(qualifierName) || string.Equals(KnownResourceQualifierName.HomeRegion, qualifierName))
                {
                    values[KnownResourceQualifierName.HomeRegion] = EnsureSystemSettings().HomeRegion;
                }
                if (string.IsNullOrEmpty(qualifierName))
                {
                    values[KnownResourceQualifierName.DeviceFamily] = GetDeviceFamilyQualifier();
                }
            }
        }

        private static string GetDeviceFamilyQualifier()
        {
            var deviceFamilyInfo = DeviceFamilyInfo.GetDeviceFamilyInfo();

            string qualifierValue = deviceFamilyInfo.DeviceFamily;

            if (qualifierValue.StartsWith("Windows."))
            {
                qualifierValue = qualifierValue[8..].Replace('.', '_');
            }

            return qualifierValue;
        }

        private static void RaiseQualifierValuesChanged(string? qualifierName)
        {
            UpdateQualifierValues(qualifierName);
            ResourceManagerFactory.RaiseQualifierValuesChanged();
        }
    }
}