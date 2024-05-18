using BlueFire.Toolkit.WinUI3.Core.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Win32;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    public class Localizer : DependencyObject
    {
        private static bool resetToDefaultLanguage;

        private Localizer()
        {
            Language = ResourceManagerFactory.UserDefaultLocaleName;
        }

        public string? Language
        {
            get { return (string?)GetValue(LanguageProperty); }
            set { SetValue(LanguageProperty, value); }
        }

        public static readonly DependencyProperty LanguageProperty =
            DependencyProperty.Register("Language", typeof(string), typeof(Localizer), new PropertyMetadata("", (s, a) =>
            {
                if (s is Localizer sender && !Equals(a.NewValue, a.OldValue))
                {
                    var newLanguage = (string?)a.NewValue;
                    if (string.IsNullOrEmpty(newLanguage))
                    {
                        try
                        {
                            resetToDefaultLanguage = true;
                            sender.Language = ResourceManagerFactory.UserDefaultLocaleName;
                            return;
                        }
                        finally { resetToDefaultLanguage = false; }
                    }

                    ResourceManagerFactory.QualifierValuesOverride[KnownResourceQualifierName.Language] = resetToDefaultLanguage ? "" : (newLanguage ?? "");
                    ResourceManagerFactory.RaiseQualifierValuesChanged();

                    sender?.LanguageChanged?.Invoke(sender, new LocalizerLanguageChangedEventArgs(newLanguage));
                }
            }));

        public event LocalizerLanguageChangedEventHandler? LanguageChanged;

        public ResourceContext CreateResourceContext()
        {
            return ResourceManagerFactory.CreateResourceContext();
        }

        public ResourceCandidate? GetResource(string resourceUri, ResourceContext? context = null)
        {
            context ??= CreateResourceContext();
            return ResourceManagerFactory.GetResource(resourceUri, context);
        }

        public string? GetLocalizedText(string resourceUri, string? language = null)
        {
            if (string.IsNullOrEmpty(language)) language = ResourceManagerFactory.UserDefaultLocaleName;
            var context = CreateResourceContext();
            context.QualifierValues[KnownResourceQualifierName.Language] = language;
            var candidate = GetResource(resourceUri, context);

            if (candidate != null && candidate.Kind == ResourceCandidateKind.String)
            {
                return candidate.ValueAsString;
            }

            return null;
        }

        public bool TryLocalizedResource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string resourceUri, string? language, out T? value)
        {
            var text = GetLocalizedText(resourceUri, language);
            if (ResourceBinding.TryChangeType(typeof(T), text, out var obj))
            {
                if (obj is not null)
                {
                    value = (T?)obj;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static object locker = new object();
        private static Localizer? instance;

        public static Localizer Default
        {
            get
            {
                ApplicationThreadHelper.VerifyAccess();

                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = new Localizer();
                        }
                    }
                }
                return instance;
            }
        }
    }

    public delegate void LocalizerLanguageChangedEventHandler(Localizer sender, LocalizerLanguageChangedEventArgs args);

    public sealed class LocalizerLanguageChangedEventArgs
    {
        internal LocalizerLanguageChangedEventArgs(string? language)
        {
            Language = language;
        }

        public string? Language { get; set; }
    }
}
