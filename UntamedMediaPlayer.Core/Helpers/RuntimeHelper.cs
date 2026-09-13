using System.Runtime.InteropServices;

namespace UntamedMediaPlayer.Core.Helpers;

internal sealed partial class RuntimeHelper
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        nint packageFullName
    );

    internal static bool IsMSIX
    {
        get
        {
            int length = 0;
            return GetCurrentPackageFullName(ref length, nint.Zero) != 15700;
        }
    }
}
