using System;

using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal sealed class MTLSwapchain : Swapchain
    {
        public override Framebuffer Framebuffer => framebuffer;

        public override bool IsDisposed => _disposed;

        public CAMetalDrawable CurrentDrawable => _drawable;

        public override bool SyncToVerticalBlank
        {
            get => _syncToVerticalBlank;
            set
            {
                if (_syncToVerticalBlank != value)
                {
                    SetSyncToVerticalBlank(value);
                }
            }
        }

        public override string? Name { get; set; }
        private readonly MTLSwapchainFramebuffer framebuffer;
        private readonly MTLGraphicsDevice _gd;
        private CAMetalLayer _metalLayer;
        private UIView _uiView; // Valid only when a UIViewSwapchainSource is used.
        private bool _syncToVerticalBlank;
        private bool _disposed;

        private CAMetalDrawable _drawable;

        public MTLSwapchain(MTLGraphicsDevice gd, in SwapchainDescription description)
        {
            _gd = gd;
            _syncToVerticalBlank = description.SyncToVerticalBlank;

            uint width;
            uint height;

            SwapchainSource source = description.Source;
            if (source is NSWindowSwapchainSource nsWindowSource)
            {
                NSWindow nswindow = new(nsWindowSource.NSWindow);
                NSView contentView = nswindow.contentView;
                CGSize windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out _metalLayer))
                {
                    _metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = _metalLayer.NativePtr;
                }
            }
            else if (source is NSViewSwapchainSource nsViewSource)
            {
                NSView contentView = new(nsViewSource.NSView);
                CGSize windowContentSize = contentView.frame.size;
                width = (uint)windowContentSize.width;
                height = (uint)windowContentSize.height;

                if (!CAMetalLayer.TryCast(contentView.layer, out _metalLayer))
                {
                    _metalLayer = CAMetalLayer.New();
                    contentView.wantsLayer = true;
                    contentView.layer = _metalLayer.NativePtr;
                }
            }
            else if (source is UIViewSwapchainSource uiViewSource)
            {
                // UIScreen mainScreen = UIScreen.mainScreen;
                // CGFloat nativeScale = mainScreen.nativeScale;

                _uiView = new UIView(uiViewSource.UIView);
                CGSize viewSize = _uiView.frame.size;
                width = (uint)viewSize.width;
                height = (uint)viewSize.height;

                if (!CAMetalLayer.TryCast(_uiView.layer, out _metalLayer))
                {
                    _metalLayer = CAMetalLayer.New();
                    _metalLayer.frame = _uiView.frame;
                    _metalLayer.opaque = true;
                    _uiView.layer.addSublayer(_metalLayer.NativePtr);
                }
            }
            else
            {
                throw new VeldridException($"A Metal Swapchain can only be created from an NSWindow, NSView, or UIView.");
            }

            PixelFormat format = description.ColorSrgb
                ? PixelFormat.B8_G8_R8_A8_UNorm_SRgb
                : PixelFormat.B8_G8_R8_A8_UNorm;

            _metalLayer.device = _gd.Device;
            _metalLayer.pixelFormat = MTLFormats.VdToMTLPixelFormat(format, default);
            _metalLayer.framebufferOnly = true;
            _metalLayer.drawableSize = new CGSize(width, height);

            SetSyncToVerticalBlank(_syncToVerticalBlank);

            framebuffer = new MTLSwapchainFramebuffer(
                gd,
                this,
                description.DepthFormat,
                format);

            GetNextDrawable();
        }

        public override void Dispose()
        {
            if (_drawable.NativePtr != IntPtr.Zero)
            {
                // ObjectiveCRuntime.objc_msgSend(_drawable.NativePtr, "release"u8);
                ObjectiveCRuntime.release(_drawable.NativePtr);
            }
            framebuffer.Dispose();
            ObjectiveCRuntime.release(_metalLayer.NativePtr);

            _disposed = true;
        }

        public override void Resize(uint width, uint height)
        {
            if (_uiView.NativePtr != IntPtr.Zero)
            {
                UIScreen mainScreen = UIScreen.mainScreen;
                CGFloat nativeScale = mainScreen.nativeScale;
                width = (uint)(width * nativeScale);
                height = (uint)(height * nativeScale);

                _metalLayer.frame = _uiView.frame;
            }

            // framebuffer.Resize(width, height);
            _metalLayer.drawableSize = new CGSize(width, height);
            if (_uiView.NativePtr != IntPtr.Zero)
            {
                _metalLayer.frame = _uiView.frame;
            }
            GetNextDrawable();
        }

        public bool EnsureDrawableAvailable()
        {
            return !_drawable.IsNull || GetNextDrawable();
        }

        public bool GetNextDrawable()
        {
            if (!_drawable.IsNull)
            {
                ObjectiveCRuntime.release(_drawable.NativePtr);
            }

            // using (NSAutoreleasePool.Begin())
            // {
            //     _drawable = _metalLayer.nextDrawable();
            //     ObjectiveCRuntime.retain(_drawable.NativePtr);
            // }
            using (NSAutoreleasePool.Begin())
            {
                _drawable = _metalLayer.nextDrawable();

                if (!_drawable.IsNull)
                {
                    ObjectiveCRuntime.retain(_drawable.NativePtr);
                    framebuffer.UpdateTextures(_drawable, _metalLayer.drawableSize);
                    return true;
                }

                return false;
            }
        }

        private void SetSyncToVerticalBlank(bool value)
        {
            _syncToVerticalBlank = value;

            if (_gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v3
                || _gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily1_v4
                || _gd.MetalFeatures.MaxFeatureSet == MTLFeatureSet.macOS_GPUFamily2_v1)
            {
                _metalLayer.displaySyncEnabled = value;
            }
        }

    }
}
