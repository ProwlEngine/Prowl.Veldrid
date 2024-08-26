﻿using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Veldrid.Vulkan
{
    internal sealed unsafe class VkTextureView : TextureView, IResourceRefCountTarget
    {
        private readonly VkGraphicsDevice _gd;
        private readonly VkImageView _imageView;
        private string? _name;

        public VkImageView ImageView => _imageView;

        public new VkTexture Target => (VkTexture)base.Target;

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => RefCount.IsDisposed;

        public VkTextureView(VkGraphicsDevice gd, in TextureViewDescription description)
            : base(description)
        {
            _gd = gd;
            VkTexture tex = Util.AssertSubtype<Texture, VkTexture>(description.Target);

            VkImageAspectFlags aspectFlags;
            if ((description.Target.Usage & TextureUsage.DepthStencil) == TextureUsage.DepthStencil)
            {
                aspectFlags = VkImageAspectFlags.Depth;
            }
            else
            {
                aspectFlags = VkImageAspectFlags.Color;
            }

            VkImageViewCreateInfo imageViewCI = new()
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = tex.OptimalDeviceImage,
                format = VkFormats.VdToVkPixelFormat(Format, tex.Usage),
                subresourceRange = new VkImageSubresourceRange()
                {
                    aspectMask = aspectFlags,
                    baseMipLevel = description.BaseMipLevel,
                    levelCount = description.MipLevels,
                    baseArrayLayer = description.BaseArrayLayer,
                    layerCount = description.ArrayLayers
                }
            };

            if ((tex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
            {
                imageViewCI.viewType = description.ArrayLayers == 1
                    ? VkImageViewType.ImageCube
                    : VkImageViewType.ImageCubeArray;
                imageViewCI.subresourceRange.layerCount *= 6;
            }
            else
            {
                switch (tex.Type)
                {
                    case TextureType.Texture1D:
                        imageViewCI.viewType = description.ArrayLayers == 1
                            ? VkImageViewType.Image1D
                            : VkImageViewType.Image1DArray;
                        break;
                    case TextureType.Texture2D:
                        imageViewCI.viewType = description.ArrayLayers == 1
                            ? VkImageViewType.Image2D
                            : VkImageViewType.Image2DArray;
                        break;
                    case TextureType.Texture3D:
                        imageViewCI.viewType = VkImageViewType.Image3D;
                        break;
                }
            }

            VkImageView imageView;
            vkCreateImageView(_gd.Device, &imageViewCI, null, &imageView);
            _imageView = imageView;
            RefCount = new ResourceRefCount(this);
        }

        public override string? Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            RefCount.DecrementDispose();
        }

        void IResourceRefCountTarget.RefZeroed()
        {
            vkDestroyImageView(_gd.Device, ImageView, null);
        }
    }
}
