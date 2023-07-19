using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace BlueFire.Toolkit.WinUI3.Icons.InternalIcons
{
    internal class ProcessDefaultIcon : ComposedIcon
    {
        private static Win32Icon? win32Icon;
        private static object staticLocker = new object();

        protected internal override nint GetIconCore(SizeInt32 size)
        {
            if (win32Icon == null)
            {
                lock (staticLocker)
                {
                    if (win32Icon == null)
                    {
                        win32Icon = new Win32Icon(Process.GetCurrentProcess().MainModule!.FileName!);
                    }
                }
            }

            return win32Icon.GetIconCore(size);
        }
    }
}
