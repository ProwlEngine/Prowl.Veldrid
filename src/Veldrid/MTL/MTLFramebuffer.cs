using System;

using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal class MTLFramebuffer : Framebuffer
    {
        public override bool IsDisposed => _disposed;
        public override string? Name { get; set; }
        private bool _disposed;

        public MTLFramebuffer(MTLGraphicsDevice gd, in FramebufferDescription description)
            : base(description.DepthTarget, description.ColorTargets)
        {
        }

        public MTLFramebuffer()
        {
        }

        public override void Dispose()
        {
            _disposed = true;
        }

        public MTLRenderPassDescriptor CreateRenderPassDescriptor()
        {
            MTLRenderPassDescriptor ret = MTLRenderPassDescriptor.New();

            for (int i = 0; i < ColorTargets.Count; i++)
            {
                FramebufferAttachment colorTarget = ColorTargets[i];
                MTLTexture mtlTarget = Util.AssertSubtype<Texture, MTLTexture>(colorTarget.Target);
                MTLRenderPassColorAttachmentDescriptor colorDescriptor = ret.colorAttachments[(uint)i];
                colorDescriptor.texture = mtlTarget.DeviceTexture;
                colorDescriptor.loadAction = MTLLoadAction.Load;
                colorDescriptor.slice = (UIntPtr)colorTarget.ArrayLayer;
                colorDescriptor.level = (UIntPtr)colorTarget.MipLevel;
            }

            if (DepthTarget != null)
            {
                // FramebufferAttachment depthTarget = DepthTarget.GetValueOrDefault();

                var mtlDepthTarget = Util.AssertSubtype<Texture, MTLTexture>(DepthTarget.Value.Target);
                var depthDescriptor = ret.depthAttachment;
                depthDescriptor.loadAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLLoadAction.DontCare : MTLLoadAction.Load;
                depthDescriptor.storeAction = mtlDepthTarget.MtlStorageMode == MTLStorageMode.Memoryless ? MTLStoreAction.DontCare : MTLStoreAction.Store;
                depthDescriptor.loadAction = MTLLoadAction.Load;
                depthDescriptor.storeAction = MTLStoreAction.Store;
                depthDescriptor.texture = mtlDepthTarget.DeviceTexture;
                // depthDescriptor.slice = (UIntPtr)depthTarget.ArrayLayer;
                // depthDescriptor.level = (UIntPtr)depthTarget.MipLevel;
                depthDescriptor.slice = mtlDepthTarget.ArrayLayers;
                depthDescriptor.level = mtlDepthTarget.MipLevels;

                if (FormatHelpers.IsStencilFormat(mtlDepthTarget.Format))
                {
                    MTLRenderPassStencilAttachmentDescriptor stencilDescriptor = ret.stencilAttachment;
                    stencilDescriptor.loadAction = MTLLoadAction.Load;
                    stencilDescriptor.storeAction = MTLStoreAction.Store;
                    stencilDescriptor.texture = mtlDepthTarget.DeviceTexture;
                    // stencilDescriptor.slice = (UIntPtr)depthTarget.ArrayLayer;
                    stencilDescriptor.slice = DepthTarget.Value.ArrayLayer;
                }
            }

            return ret;
        }
    }
}
