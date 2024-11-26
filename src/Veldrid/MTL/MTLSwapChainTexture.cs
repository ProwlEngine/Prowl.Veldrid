// // Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// // See the LICENCE file in the repository root for full licence text.

// using Veldrid.MetalBindings;

// namespace Veldrid.MTL
// {
//     internal class MTLSwapchainTexture : MTLTexture
//     {
//         public override MetalBindings.MTLTexture DeviceTexture => deviceTexture;

//         public override uint Width => width;

//         public override uint Height => height;

//         public override uint Depth => 1;

//         public override uint ArrayLayers => 1;

//         public override uint MipLevels => 1;

//         public override TextureUsage Usage => TextureUsage.RenderTarget;

//         public override TextureType Type => TextureType.Texture2D;

//         public override TextureSampleCount SampleCount => TextureSampleCount.Count1;

//         public override MTLPixelFormat MtlPixelFormat => mtlPixelFormat;

//         public override MTLTextureType MtlTextureType => MTLTextureType.Type2D;

//         private MetalBindings.MTLTexture deviceTexture;
//         private uint width;
//         private uint height;
//         private MTLPixelFormat mtlPixelFormat;

//         public void SetDrawable(CAMetalDrawable drawable, CGSize size, PixelFormat format)
//         {
//             deviceTexture = drawable.texture;
//             width = (uint)size.width;
//             height = (uint)size.height;
//             mtlPixelFormat = MTLFormats.VdToMTLPixelFormat(Format, Usage);
//         }
//     }
// }
