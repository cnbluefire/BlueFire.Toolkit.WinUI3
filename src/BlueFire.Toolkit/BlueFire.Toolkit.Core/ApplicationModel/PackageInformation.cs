using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.ApplicationModel
{
    public class PackageInformation
    {
        private static bool isUnpackagedApp;
        private static Lock locker = new Lock();
        private static PackageInformation? current;

        private PackageInformation(
            string packageInstallLocation,
            string packageFullName,
            string packageFamilyName,
            PackageId packageId)
        {
            PackageInstallLocation = packageInstallLocation;
            PackageFullName = packageFullName;
            PackageFamilyName = packageFamilyName;
            PackageId = packageId;
        }

        public string PackageInstallLocation { get; }

        public string PackageFullName { get; }

        public string PackageFamilyName { get; }

        public PackageId PackageId { get; }

        public override string ToString()
        {
            return $"PackageInstallLocation: {PackageInstallLocation}, PackageFullName: {PackageFullName}, PackageFamilyName: {PackageFamilyName}, PackageId: {PackageId}";
        }

        public static PackageInformation? Current
        {
            get
            {
                if (current == null && !isUnpackagedApp)
                {
                    lock (locker)
                    {
                        if (current == null && !isUnpackagedApp)
                        {
                            current = CreateCurrentPackageInformation();
                        }
                    }
                }

                return current;
            }
        }

        private unsafe static PackageInformation CreateFromBuffer(byte* pBuffer, uint count)
        {
            var span = new Span<PACKAGE_INFO>(pBuffer, (int)count);

            var version = span[0].packageId.version.Anonymous.Anonymous;

            return new PackageInformation(
                span[0].path.ToString(),
                span[0].packageFullName.ToString(),
                span[0].packageFamilyName.ToString(),
            new PackageId(
                    (PackageId.ProcessorArchitecture)span[0].packageId.processorArchitecture,
                    new Version(version.Major, version.Minor, version.Build, version.Revision),
                    span[0].packageId.name.ToString(),
                    span[0].packageId.publisher.ToString(),
                    span[0].packageId.resourceId.ToString(),
                    span[0].packageId.publisherId.ToString()));
        }

        private unsafe static PackageInformation? CreateCurrentPackageInformation()
        {
            uint size, count;

#pragma warning disable CA1416
            var err = Windows.Win32.PInvoke.GetCurrentPackageInfo(0x00000010, &size, null, &count);
#pragma warning restore CA1416

            if (count > 0)
            {
                var buffer = new byte[size];

                fixed (byte* pBuffer = buffer)
                {
                    err = Windows.Win32.PInvoke.GetCurrentPackageInfo(0x00000010, &size, pBuffer, &count);
                    if (err == Windows.Win32.Foundation.WIN32_ERROR.ERROR_SUCCESS)
                    {
                        return CreateFromBuffer(pBuffer, count);
                    }
                }
            }
            else
            {
                isUnpackagedApp = true;
            }
            return null;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private partial struct PACKAGE_ID
        {
            internal uint reserved;
            internal uint processorArchitecture;
            internal Windows.Win32.Storage.Packaging.Appx.PACKAGE_VERSION version;
            internal Windows.Win32.Foundation.PWSTR name;
            internal Windows.Win32.Foundation.PWSTR publisher;
            internal Windows.Win32.Foundation.PWSTR resourceId;
            internal Windows.Win32.Foundation.PWSTR publisherId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private partial struct PACKAGE_INFO
        {
            internal uint reserved;
            internal uint flags;
            internal Windows.Win32.Foundation.PWSTR path;
            internal Windows.Win32.Foundation.PWSTR packageFullName;
            internal Windows.Win32.Foundation.PWSTR packageFamilyName;
            internal PACKAGE_ID packageId;
        }
    }

    public record PackageId(
        PackageId.ProcessorArchitecture Architecture,
        Version Version,
        string Name,
        string Publisher,
        string? ResourceId,
        string PublisherId)
    {
        public enum ProcessorArchitecture : uint
        {
            /// <summary>
            /// The ARM processor architecture.
            /// </summary>
            Arm = 5,

            /// <summary>
            /// The Arm64 processor architecture.
            /// </summary>
            Arm64 = 12,

            /// <summary>
            /// A neutral processor architecture.
            /// </summary>
            Neutral = 11,

            /// <summary>
            /// An unknown processor architecture.
            /// </summary>
            Unknown = 65535,

            /// <summary>
            /// The x64 processor architecture.
            /// </summary>
            X64 = 9,

            /// <summary>
            /// The x86 processor architecture.
            /// </summary>
            X86 = 0,

            /// <summary>
            /// The Arm64 processor architecture emulating the X86 architecture.
            /// </summary>
            X86OnArm64 = 14,
        }
    }
}
