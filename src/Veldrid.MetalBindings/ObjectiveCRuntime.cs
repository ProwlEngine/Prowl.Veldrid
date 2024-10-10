using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    public static unsafe partial class ObjectiveCRuntime
    {
        private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, float a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, double a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, CGRect a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, uint b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, NSRange b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLSize a, MTLSize b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr c, UIntPtr d, MTLSize e);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLClearColor a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, CGSize a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b, UIntPtr c);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, UIntPtr c);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d, UIntPtr e);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, UIntPtr c, UIntPtr d);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, NSRange a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLViewport a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLScissorRect a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, void* a, uint b, UIntPtr c);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, UIntPtr b, MTLIndexType c, IntPtr d, UIntPtr e, UIntPtr f);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, MTLPrimitiveType a, MTLBuffer b, UIntPtr c);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLPrimitiveType a,
            UIntPtr b,
            MTLIndexType c,
            IntPtr d,
            UIntPtr e,
            UIntPtr f,
            IntPtr g,
            UIntPtr h);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLPrimitiveType a,
            MTLIndexType b,
            MTLBuffer c,
            UIntPtr d,
            MTLBuffer e,
            UIntPtr f);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLBuffer a,
            UIntPtr b,
            MTLBuffer c,
            UIntPtr d,
            UIntPtr e);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            IntPtr a,
            UIntPtr b,
            UIntPtr c,
            UIntPtr d,
            MTLSize e,
            IntPtr f,
            UIntPtr g,
            UIntPtr h,
            MTLOrigin i);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLRegion a,
            UIntPtr b,
            UIntPtr c,
            IntPtr d,
            UIntPtr e,
            UIntPtr f);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLTexture a,
            UIntPtr b,
            UIntPtr c,
            MTLOrigin d,
            MTLSize e,
            MTLBuffer f,
            UIntPtr g,
            UIntPtr h,
            UIntPtr i);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(
            IntPtr receiver,
            Selector selector,
            MTLTexture sourceTexture,
            UIntPtr sourceSlice,
            UIntPtr sourceLevel,
            MTLOrigin sourceOrigin,
            MTLSize sourceSize,
            MTLTexture destinationTexture,
            UIntPtr destinationSlice,
            UIntPtr destinationLevel,
            MTLOrigin destinationOrigin);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial byte* bytePtr_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial CGSize CGSize_objc_msgSend(IntPtr receiver, Selector selector);


        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial byte byte_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a, IntPtr b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial Bool8 bool8_objc_msgSend(IntPtr receiver, Selector selector, uint a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial uint uint_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial float float_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial CGFloat CGFloat_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial double double_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, NSError* error);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a, uint b, NSRange c, NSRange d);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, MTLComputePipelineDescriptor a, uint b, IntPtr c, NSError* error);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, uint a);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr a);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, IntPtr b, NSError* error);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, IntPtr a, UIntPtr b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, UIntPtr b, MTLResourceOptions c);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, Selector selector, void* a, UIntPtr b, MTLResourceOptions c);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial UIntPtr UIntPtr_objc_msgSend(IntPtr receiver, Selector selector);

        public static T objc_msgSend<T>(IntPtr receiver, Selector selector) where T : unmanaged
        {
            IntPtr value = IntPtr_objc_msgSend(receiver, selector);
            return Unsafe.BitCast<IntPtr, T>(value);
        }
        public static T objc_msgSend<T>(IntPtr receiver, Selector selector, IntPtr a) where T : unmanaged
        {
            IntPtr value = IntPtr_objc_msgSend(receiver, selector, a);
            return Unsafe.BitCast<IntPtr, T>(value);
        }
        public static string string_objc_msgSend(IntPtr receiver, Selector selector)
        {
            return objc_msgSend<NSString>(receiver, selector).GetValue();
        }

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, byte b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, Bool8 b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, uint b);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, float a, float b, float c, float d);
        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial void objc_msgSend(IntPtr receiver, Selector selector, IntPtr b);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend_stret")]
        public static partial void objc_msgSend_stret(void* retPtr, IntPtr receiver, Selector selector);
        public static T objc_msgSend_stret<T>(IntPtr receiver, Selector selector) where T : unmanaged
        {
            T ret = default;
            objc_msgSend_stret(&ret, receiver, selector);
            return ret;
        }

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial MTLClearColor MTLClearColor_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial MTLSize MTLSize_objc_msgSend(IntPtr receiver, Selector selector);

        [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        public static partial CGRect CGRect_objc_msgSend(IntPtr receiver, Selector selector);

        // TODO: This should check the current processor type, struct size, etc.
        // At the moment there is no need because all existing occurences of
        // this can safely use the non-stret versions everywhere.
        public static bool UseStret<T>() => false;

        [LibraryImport(ObjCLibrary)]
        public static partial IntPtr sel_registerName(byte* namePtr);

        [LibraryImport(ObjCLibrary)]
        public static partial byte* sel_getName(IntPtr selector);

        [LibraryImport(ObjCLibrary)]
        public static partial IntPtr objc_getClass(byte* namePtr);

        [LibraryImport(ObjCLibrary)]
        public static partial ObjCClass object_getClass(IntPtr obj);

        [LibraryImport(ObjCLibrary)]
        public static partial IntPtr class_getProperty(ObjCClass cls, byte* namePtr);

        [LibraryImport(ObjCLibrary)]
        public static partial byte* class_getName(ObjCClass cls);

        [LibraryImport(ObjCLibrary)]
        public static partial byte* property_copyAttributeValue(IntPtr property, byte* attributeNamePtr);

        [LibraryImport(ObjCLibrary)]
        public static partial Selector method_getName(ObjectiveCMethod method);

        [LibraryImport(ObjCLibrary)]
        public static partial ObjectiveCMethod* class_copyMethodList(ObjCClass cls, uint* outCount);

        [LibraryImport(ObjCLibrary)]
        public static partial void free(IntPtr receiver);

        public static void retain(IntPtr receiver) => objc_msgSend(receiver, "retain"u8);
        public static void release(IntPtr receiver) => objc_msgSend(receiver, "release"u8);
        public static nuint GetRetainCount(IntPtr receiver) => UIntPtr_objc_msgSend(receiver, "retainCount"u8);
    }
}
