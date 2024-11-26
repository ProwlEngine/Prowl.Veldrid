// using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal sealed class MTLTextureView : TextureView
    {
        public Veldrid.MetalBindings.MTLTexture TargetDeviceTexture { get; }

        public override bool IsDisposed => _disposed;

        public override string? Name { get; set; }
        private readonly bool _hasTextureView;
        private bool _disposed;

        public MTLTextureView(in TextureViewDescription description, MTLGraphicsDevice gd)
            : base(description)
        {
            MTLTexture targetMTLTexture = Util.AssertSubtype<Texture, MTLTexture>(description.Target);
            if (BaseMipLevel != 0 || MipLevels != Target.MipLevels
                                  || BaseArrayLayer != 0 || ArrayLayers != Target.ArrayLayers
                                  || Format != Target.Format)
            {
                _hasTextureView = true;
                // uint effectiveArrayLayers = (Target.Usage & TextureUsage.Cubemap) != 0 ? ArrayLayers * 6 : ArrayLayers;
                uint effectiveArrayLayers = Target.Usage.HasFlag(TextureUsage.Cubemap) ? ArrayLayers * 6 : ArrayLayers;
                TargetDeviceTexture = targetMTLTexture.DeviceTexture.newTextureView(
                    MTLFormats.VdToMTLPixelFormat(Format, description.Target.Usage),
                    targetMTLTexture.MtlTextureType,
                    new Veldrid.MetalBindings.NSRange(BaseMipLevel, MipLevels),
                    new Veldrid.MetalBindings.NSRange(BaseArrayLayer, effectiveArrayLayers));
            }
            else
            {
                TargetDeviceTexture = targetMTLTexture.DeviceTexture;
            }
        }

        public override void Dispose()
        {
            if (_hasTextureView && !_disposed)
            {
                _disposed = true;
                Veldrid.MetalBindings.ObjectiveCRuntime.release(TargetDeviceTexture.NativePtr);
            }
        }
    }
}
