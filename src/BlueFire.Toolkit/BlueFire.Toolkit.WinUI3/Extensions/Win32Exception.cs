using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Extensions
{
    public class Win32Exception : Exception
    {
        internal Win32Exception(int errorCode) : this(errorCode, null) { }

        internal Win32Exception(int errorCode, string? message)
            : base(ConvertMessage(errorCode, message))
        {
            ErrorCode = errorCode;
            HResult = Win32ErrorCodeToHResult(errorCode);
        }

        public int ErrorCode { get; }

        private static int Win32ErrorCodeToHResult(int errorCode)
        {
            if ((errorCode & 0x80000000) == 0x80000000)
                return errorCode;
            else
                return (errorCode & 0x0000FFFF) | unchecked((int)0x80070000);
        }

        private static string? ConvertMessage(int errorCode, string? message)
        {
            var errorName = Win32ErrorHelper.GetErrorName(errorCode);

            if (string.IsNullOrEmpty(errorName)) return message;

            if (string.IsNullOrEmpty(message)) return errorName;

            return $"{errorName} {message}";
        }
    }
}
