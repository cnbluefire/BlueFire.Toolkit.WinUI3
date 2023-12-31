﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Text
{
    public class CanvasFontFamilyCollection : IReadOnlyList<CanvasFontFamily>
    {
        private List<CanvasFontFamily> internalList;

        public CanvasFontFamilyCollection(string source)
        {
            Source = source ?? "";
            internalList = new List<CanvasFontFamily>();
            Canonicalize();
        }

        public CanvasFontFamily this[int index] => ((IReadOnlyList<CanvasFontFamily>)internalList)[index];

        public int Count => ((IReadOnlyCollection<CanvasFontFamily>)internalList).Count;

        public string Source { get; }

        public IEnumerator<CanvasFontFamily> GetEnumerator()
        {
            return ((IEnumerable<CanvasFontFamily>)internalList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)internalList).GetEnumerator();
        }

        private void Canonicalize()
        {
            if (Source.Length > 0)
            {
                var identifiers = Source.Split(',')
                    .Select(c => c.Split('#'))
                    .Select(c => c.Length switch
                    {
                        1 => (location: "", familyName: c[0]),
                        2 => (location: c[0], familyName: c[1]),
                        _ => default
                    })
                    .Where(c => !string.IsNullOrEmpty(c.familyName));

                foreach (var (location, familyName) in identifiers)
                {
                    Uri? locationUri = null;
                    string? unescapeName = null;

                    var location2 = location?.Trim();

                    if (!string.IsNullOrEmpty(location2))
                    {
                        if (location2.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase)
                            || location2.StartsWith("ms-appx-web://", StringComparison.OrdinalIgnoreCase)
                            || location2.StartsWith("ms-appdata://", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Uri.TryCreate(location2, UriKind.Absolute, out locationUri))
                            {
                                continue;
                            }
                        }
                        else if (location2.StartsWith('/'))
                        {
                            if (!Uri.TryCreate($"ms-appx://{location2}", UriKind.Absolute, out locationUri))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var familyName2 = familyName.Trim();

                    if ((familyName2.StartsWith('"') && familyName2.EndsWith('"'))
                        || (familyName2.StartsWith('\'') && familyName2.EndsWith('\'')))
                    {
                        familyName2 = familyName2[1..^1];
                    }

                    unescapeName = Uri.UnescapeDataString(familyName2).ToUpperInvariant();

                    internalList.Add(new CanvasFontFamily(unescapeName, locationUri));
                }
            }
        }
    }
}
