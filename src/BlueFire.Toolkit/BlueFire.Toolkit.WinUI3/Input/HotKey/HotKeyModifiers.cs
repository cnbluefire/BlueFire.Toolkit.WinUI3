using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Input
{
    public enum HotKeyModifiers : ushort
    {
        MOD_NONE = 0x0,

        MOD_ALT = 0x1,

        MOD_CONTROL = 0x2,

        MOD_SHIFT = 0x4,

        MOD_WIN = 0x8,

        MOD_NOREPEAT = 0x4000,
    }
}
