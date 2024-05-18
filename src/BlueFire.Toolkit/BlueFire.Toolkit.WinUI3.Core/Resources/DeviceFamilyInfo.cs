using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Resources
{
    internal struct DeviceFamilyInfo
    {
        private DeviceFamilyInfo(string deviceFamily, string deviceClass)
        {
            DeviceFamily = deviceFamily;
            DeviceClass = deviceClass;
        }

        public string DeviceFamily { get; }

        public string DeviceClass { get; }

        public void Deconstruct(out string deviceFamily, out string deviceClass)
        {
            deviceFamily = DeviceFamily;
            deviceClass = DeviceClass;
        }

        public unsafe static DeviceFamilyInfo GetDeviceFamilyInfo()
        {
            const uint STATUS_BUFFER_TOO_SMALL = 0xC0000023;

            uint deviceFamilyBufferSize = 0;
            uint deviceClassBufferSize = 0;

            var status = RtlConvertDeviceFamilyInfoToString(&deviceFamilyBufferSize, &deviceClassBufferSize, null, null);
            if (status != STATUS_BUFFER_TOO_SMALL )
            {
                Marshal.ThrowExceptionForHR(HRESULT_FROM_NT(status));
            }

            char* deviceFamilyBuffer = stackalloc char[(int)(deviceFamilyBufferSize / 2)];
            char* deviceClassBuffer = stackalloc char[(int)(deviceClassBufferSize / 2)];

            status = RtlConvertDeviceFamilyInfoToString(&deviceFamilyBufferSize, &deviceClassBufferSize, deviceFamilyBuffer, deviceClassBuffer);
            if (HRESULT_FROM_NT(status) < 0)
            {
                Marshal.ThrowExceptionForHR(HRESULT_FROM_NT(status));
            }

            return new DeviceFamilyInfo(new string(deviceFamilyBuffer), new string(deviceClassBuffer));
        }

        private static int HRESULT_FROM_NT(uint ntStatus) => unchecked((int)(ntStatus | 0x10000000));

        [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
        private unsafe static extern uint RtlConvertDeviceFamilyInfoToString(
          uint* pulDeviceFamilyBufferSize,
          uint* pulDeviceFormBufferSize,
          char* DeviceFamily,
          char* DeviceForm
        );

    }
}
