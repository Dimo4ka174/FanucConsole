using System.Runtime.InteropServices;

namespace FanucFocasConsole.Interop
{
    public static class FocasNativeFactory
    {
        public static IFocasNative Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new FocasNativeWindows();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new FocasNativeLinux();
            throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
        }
    }
}