using System;
using System.Runtime.InteropServices;

namespace Veldrid.OpenGL.EGL
{
    internal static unsafe partial class EGLNative
    {
        private const string LibName = "libEGL.so";

        public const int EGL_HEIGHT = 0x3056;
        public const int EGL_WIDTH = 0x3057;
        public const int EGL_DRAW = 0x3059;
        public const int EGL_READ = 0x305A;
        public const int EGL_RED_SIZE = 0x3024;
        public const int EGL_GREEN_SIZE = 0x3023;
        public const int EGL_BLUE_SIZE = 0x3022;
        public const int EGL_ALPHA_SIZE = 0x3021;
        public const int EGL_DEPTH_SIZE = 0x3025;
        public const int EGL_SURFACE_TYPE = 0x3033;
        public const int EGL_WINDOW_BIT = 0x0004;
        public const int EGL_PBUFFER_BIT = 0x0001;
        public const int EGL_OPENGL_ES_BIT = 0x0001;
        public const int EGL_OPENGL_ES2_BIT = 0x0004;
        public const int EGL_OPENGL_ES3_BIT = 0x00000040;
        public const int EGL_RENDERABLE_TYPE = 0x3040;
        public const int EGL_NONE = 0x3038;
        public const int EGL_NATIVE_VISUAL_ID = 0x302E;
        public const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
        public const int EGL_CONTEXT_OPENGL_DEBUG = 0x31B0;

        [LibraryImport(LibName)]
        public static partial EGLError eglGetError();
        [LibraryImport(LibName)]
        public static partial IntPtr eglGetCurrentContext();
        [LibraryImport(LibName)]
        public static partial int eglDestroyContext(IntPtr display, IntPtr context);
        [LibraryImport(LibName)]
        public static partial int eglDestroySurface(IntPtr display, IntPtr surface);
        [LibraryImport(LibName)]
        public static partial int eglTerminate(IntPtr display);
        [LibraryImport(LibName)]
        public static partial int eglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
        [LibraryImport(LibName)]
        public static partial int eglChooseConfig(IntPtr display, int* attrib_list, IntPtr* configs, int config_size, int* num_config);
        [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr eglGetProcAddress(string name);
        [LibraryImport(LibName)]
        public static partial IntPtr eglGetCurrentDisplay();
        [LibraryImport(LibName)]
        public static partial IntPtr eglGetDisplay(int native_display);
        [LibraryImport(LibName)]
        public static partial IntPtr eglGetCurrentSurface(int readdraw);
        [LibraryImport(LibName)]
        public static partial int eglInitialize(IntPtr display, int* major, int* minor);

        [LibraryImport(LibName)]
        public static partial IntPtr eglCreateWindowSurface(
            IntPtr display,
            IntPtr config,
            IntPtr native_window,
            int* attrib_list);

        [LibraryImport(LibName)]
        public static partial IntPtr eglCreatePbufferSurface(
            IntPtr display,
            IntPtr config,
            int* attrib_list);

        [LibraryImport(LibName)]
        public static partial IntPtr eglCreateContext(IntPtr display,
            IntPtr config,
            IntPtr share_context,
            int* attrib_list);
        [LibraryImport(LibName)]
        public static partial int eglSwapBuffers(IntPtr display, IntPtr surface);
        [LibraryImport(LibName)]
        public static partial int eglSwapInterval(IntPtr display, int value);
        [LibraryImport(LibName)]
        public static partial int eglGetConfigAttrib(IntPtr display, IntPtr config, int attribute, int* value);

        [LibraryImport(LibName)]
        public static partial int eglQuerySurface(
            IntPtr display,
            IntPtr surface,
            int attribute,
            int* value);
    }

    internal enum EGLError
    {
        Success = 0x3000,
        NotInitialized = 0x3001,
        BadAccess = 0x3002,
        BadAlloc = 0x3003,
        BadAttribute = 0x3004,
        BadConfig = 0x3005,
        BadContext = 0x3006,
        BadCurrentSurface = 0x3007,
        BadDisplay = 0x3008,
        BadMatch = 0x3009,
        BadNativePixmap = 0x300A,
        BadNativeWindow = 0x300B,
        BadParameter = 0x300C,
        BadSurface = 0x300D,
        ContextLost = 0x300E,
    }
}
