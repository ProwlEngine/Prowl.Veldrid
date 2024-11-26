using System;

using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MTLTexture : Texture
    {
        /// <summary>
        /// The native MTLTexture object. This property is only valid for non-staging Textures.
        /// </summary>
        public virtual MetalBindings.MTLTexture DeviceTexture { get; protected set; }
        /// <summary>
        /// The staging MTLBuffer object. This property is only valid for staging Textures.
        /// </summary>
        public MetalBindings.MTLBuffer StagingBuffer { get; }

        // public MTLPixelFormat MTLPixelFormat { get; }
        public override PixelFormat Format { get; protected set; }

        public override bool IsDisposed => _disposed;

        public virtual MTLPixelFormat MtlPixelFormat { get; protected set; }
        public virtual MTLTextureType MtlTextureType { get; }

        public MTLStorageMode MtlStorageMode { get; }

        public unsafe void* StagingBufferPointer { get; private set; }
        public override string? Name { get; set; }
        private bool _disposed;

        public MTLTexture(in TextureDescription description, MTLGraphicsDevice _gd)
        {
            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            ArrayLayers = description.ArrayLayers;
            MipLevels = description.MipLevels;
            Format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;

            bool isDepth = (Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil;

            MtlPixelFormat = MTLFormats.VdToMTLPixelFormat(Format, Usage);
            MtlTextureType = MTLFormats.VdToMTLTextureType(
                    Type,
                    ArrayLayers,
                    SampleCount != TextureSampleCount.Count1,
                    (Usage & TextureUsage.Cubemap) != 0);
            if (Usage != TextureUsage.Staging)
            {
                bool depthFormat = FormatHelpers.IsDepthFormatPreferred(Format, Usage);
                MtlStorageMode = isDepth && depthFormat ? MTLStorageMode.Memoryless : MTLStorageMode.Private;

                MTLTextureDescriptor texDescriptor = MTLTextureDescriptor.New();
                texDescriptor.width = (UIntPtr)Width;
                texDescriptor.height = (UIntPtr)Height;
                texDescriptor.depth = (UIntPtr)Depth;
                texDescriptor.mipmapLevelCount = (UIntPtr)MipLevels;
                texDescriptor.arrayLength = (UIntPtr)ArrayLayers;
                texDescriptor.sampleCount = (UIntPtr)FormatHelpers.GetSampleCountUInt32(SampleCount);
                texDescriptor.textureType = MtlTextureType;
                texDescriptor.pixelFormat = MtlPixelFormat;
                texDescriptor.textureUsage = MTLFormats.VdToMTLTextureUsage(Usage);
                texDescriptor.storageMode = MtlStorageMode;

                DeviceTexture = _gd.Device.newTextureWithDescriptor(texDescriptor);
                ObjectiveCRuntime.release(texDescriptor.NativePtr);
            }
            else
            {
                // uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
                uint totalStorageSize = 0;
                for (uint level = 0; level < MipLevels; level++)
                {
                    Util.GetMipDimensions(this, level, out uint levelWidth, out uint levelHeight, out uint levelDepth);
                    // uint storageWidth = Math.Max(levelWidth, blockSize);
                    // uint storageHeight = Math.Max(levelHeight, blockSize);
                    totalStorageSize += levelDepth * FormatHelpers.GetDepthPitch(
                        FormatHelpers.GetRowPitch(levelWidth, Format),
                        levelHeight,
                        Format);
                }
                totalStorageSize *= ArrayLayers;

                StagingBuffer = _gd.Device.newBufferWithLengthOptions(
                    (UIntPtr)totalStorageSize,
                    MTLResourceOptions.StorageModeShared);

                unsafe
                {
                    StagingBufferPointer = StagingBuffer.contents();
                }
            }
        }

        public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format)
        {
            DeviceTexture = drawable.texture;
            Width = (uint)size.width;
            Height = (uint)size.height;
            MtlPixelFormat = MTLFormats.VdToMTLPixelFormat(Format, Usage);
        }

        public MTLTexture(ulong nativeTexture, in TextureDescription description)
        {
            DeviceTexture = new MetalBindings.MTLTexture((IntPtr)nativeTexture);
            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            ArrayLayers = description.ArrayLayers;
            MipLevels = description.MipLevels;
            Format = description.Format;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;

            MtlPixelFormat = MTLFormats.VdToMTLPixelFormat(Format, Usage);
            MtlTextureType = MTLFormats.VdToMTLTextureType(
                    Type,
                    ArrayLayers,
                    SampleCount != TextureSampleCount.Count1,
                    (Usage & TextureUsage.Cubemap) != 0);
        }

        internal uint GetSubresourceSize(uint mipLevel, uint arrayLayer)
        {
            uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
            Util.GetMipDimensions(this, mipLevel, out uint width, out uint height, out uint depth);
            uint storageWidth = Math.Max(blockSize, width);
            uint storageHeight = Math.Max(blockSize, height);
            return depth * FormatHelpers.GetDepthPitch(
                FormatHelpers.GetRowPitch(storageWidth, Format),
                storageHeight,
                Format);
        }

        // already implemented in parent class
        // internal void GetSubresourceLayout(uint mipLevel, uint arrayLayer, out uint rowPitch, out uint depthPitch)
        // {
        //     uint blockSize = FormatHelpers.IsCompressedFormat(Format) ? 4u : 1u;
        //     Util.GetMipDimensions(this, mipLevel, out uint mipWidth, out uint mipHeight, out uint _);
        //     uint storageWidth = Math.Max(blockSize, mipWidth);
        //     uint storageHeight = Math.Max(blockSize, mipHeight);
        //     rowPitch = FormatHelpers.GetRowPitch(storageWidth, Format);
        //     depthPitch = FormatHelpers.GetDepthPitch(rowPitch, storageHeight, Format);
        // }

        private protected override void DisposeCore()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (!StagingBuffer.IsNull)
                {
                    ObjectiveCRuntime.release(StagingBuffer.NativePtr);
                }
                else
                {
                    ObjectiveCRuntime.release(DeviceTexture.NativePtr);
                }
            }
        }
    }
}
