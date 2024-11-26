// using System.Diagnostics;

// using Veldrid.MetalBindings;

// namespace Veldrid.MTL
// {
//     internal sealed class MTLSwapchainFramebuffer : MTLFramebuffer
//     {
//         private readonly MTLGraphicsDevice _gd;
//         private readonly MTLPlaceholderTexture _placeholderTexture;
//         private MTLTexture? _depthTexture;
//         private readonly MTLSwapchain _parentSwapchain;
//         private bool _disposed;

//         private readonly PixelFormat? _depthFormat;

//         public override bool IsDisposed => _disposed;

//         public MTLSwapchainFramebuffer(
//             MTLGraphicsDevice gd,
//             MTLSwapchain parent,
//             uint width,
//             uint height,
//             PixelFormat? depthFormat,
//             PixelFormat colorFormat)
//         {
//             _gd = gd;
//             _parentSwapchain = parent;

//             OutputAttachmentDescription? depthAttachment = null;
//             if (depthFormat != null)
//             {
//                 _depthFormat = depthFormat;
//                 depthAttachment = new OutputAttachmentDescription(depthFormat.Value);
//                 RecreateDepthTexture(width, height);
//                 _depthTarget = new FramebufferAttachment(_depthTexture!, 0);
//             }
//             OutputAttachmentDescription colorAttachment = new(colorFormat);

//             OutputDescription = new OutputDescription(depthAttachment, colorAttachment);
//             _placeholderTexture = new MTLPlaceholderTexture(colorFormat);
//             _placeholderTexture.Resize(width, height);
//             _colorTargets = new[] { new FramebufferAttachment(_placeholderTexture, 0) };

//             Width = width;
//             Height = height;
//         }

//         private void RecreateDepthTexture(uint width, uint height)
//         {
//             Debug.Assert(_depthFormat.HasValue);
//             if (_depthTexture != null)
//             {
//                 _depthTexture.Dispose();
//             }

//             _depthTexture = Util.AssertSubtype<Texture, MTLTexture>(
//                 _gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
//                     width, height, 1, 1, _depthFormat.Value, TextureUsage.DepthStencil)));
//         }

//         public void Resize(uint width, uint height)
//         {
//             _placeholderTexture.Resize(width, height);

//             if (_depthFormat.HasValue)
//             {
//                 RecreateDepthTexture(width, height);
//             }

//             Width = width;
//             Height = height;
//         }

//         public override bool IsRenderable => !_parentSwapchain.CurrentDrawable.IsNull;

//         public override MTLRenderPassDescriptor CreateRenderPassDescriptor()
//         {
//             MTLRenderPassDescriptor ret = MTLRenderPassDescriptor.New();
//             MTLRenderPassColorAttachmentDescriptor color0 = ret.colorAttachments[0];
//             color0.texture = _parentSwapchain.CurrentDrawable.texture;
//             color0.loadAction = MTLLoadAction.Load;

//             if (_depthTarget != null)
//             {
//                 MTLRenderPassDepthAttachmentDescriptor depthAttachment = ret.depthAttachment;
//                 depthAttachment.texture = _depthTexture!.DeviceTexture;
//                 depthAttachment.loadAction = MTLLoadAction.Load;
//             }

//             return ret;
//         }

//         public override void Dispose()
//         {
//             _depthTexture?.Dispose();
//             _disposed = true;
//         }
//     }
// }

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MTLSwapchainFramebuffer : MTLFramebufferBase
    {
        public override uint Width => colorTexture == null ? colorTexture!.Width : 0;
        public override uint Height => colorTexture == null ? colorTexture!.Height : 0;

        public override OutputDescription OutputDescription { get; protected set; }

        public override List<FramebufferAttachment> ColorTargets => colorTargets.ToList();
        public override FramebufferAttachment? DepthTarget => depthTarget;

        public override bool IsRenderable => throw new NotImplementedException();

        public override bool IsDisposed => throw new NotImplementedException();

        private readonly MTLGraphicsDevice gd;
        private readonly MTLSwapchain parentSwapchain;
        private readonly PixelFormat colorFormat;

        private readonly PixelFormat? depthFormat;
        private readonly MTLTexture colorTexture = new MTLTexture();
        private MTLTexture depthTexture;

        private readonly FramebufferAttachment[] colorTargets;
        private FramebufferAttachment? depthTarget;

        public MTLSwapchainFramebuffer(
            MTLGraphicsDevice gd,
            MTLSwapchain parent,
            PixelFormat? depthFormat,
            PixelFormat colorFormat)
        {
            // colorTexture = new MTLTexture(new TextureDescription(, Height, 1, 1, 1, colorFormat, TextureUsage.RenderTarget, TextureType.Texture2D), gd);

            this.gd = gd;
            parentSwapchain = parent;
            this.colorFormat = colorFormat;

            OutputAttachmentDescription? depthAttachment = null;

            if (depthFormat != null)
            {
                this.depthFormat = depthFormat;
                depthAttachment = new OutputAttachmentDescription(depthFormat.Value);
            }

            var colorAttachment = new OutputAttachmentDescription(colorFormat);

            colorTargets = new[] { new FramebufferAttachment(colorTexture, 0) };

            OutputDescription = new OutputDescription(depthAttachment, colorAttachment);
        }

        #region Disposal

        public override void Dispose()
        {
            depthTexture?.Dispose();
        }

        #endregion

        public void UpdateTextures(CAMetalDrawable drawable, CGSize size)
        {
            colorTexture.SetDrawable(drawable, size, colorFormat);

            if (depthFormat.HasValue && (size.width != depthTexture?.Width || size.height != depthTexture?.Height))
                recreateDepthTexture((uint)size.width, (uint)size.height);
        }

        public bool EnsureDrawableAvailable()
        {
            return parentSwapchain.EnsureDrawableAvailable();
        }

        private void recreateDepthTexture(uint width, uint height)
        {
            Debug.Assert(depthFormat.HasValue);
            depthTexture?.Dispose();

            depthTexture = Util.AssertSubtype<Texture, MTLTexture>(
                gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    width, height, 1, 1, depthFormat.Value, TextureUsage.DepthStencil)));
            depthTarget = new FramebufferAttachment(depthTexture, 0);
        }

        public override MTLRenderPassDescriptor CreateRenderPassDescriptor()
        {
            throw new NotImplementedException();
        }
    }
}
