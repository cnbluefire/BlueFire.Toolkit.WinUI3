using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Sample.WinUI3.Utils
{
    public static class BindingUtils
    {
        public static bool IsEmpty(ICollection? collection) => collection == null || collection.Count == 0;
    }
}
