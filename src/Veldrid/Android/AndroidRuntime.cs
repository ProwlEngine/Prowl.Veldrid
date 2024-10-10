using System;
using System.Runtime.InteropServices;

namespace Veldrid.Android
{
    /// <summary>
    /// Function imports from the Android runtime library (android.so).
    /// </summary>
    internal static partial class AndroidRuntime
    {
        private const string LibName = "android.so";

        [LibraryImport(LibName)]
        public static partial IntPtr ANativeWindow_fromSurface(IntPtr jniEnv, IntPtr surface);

        [LibraryImport(LibName)]
        public static partial int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);

        [LibraryImport(LibName)]
        public static partial void ANativeWindow_release(IntPtr aNativeWindow);
    }
}
