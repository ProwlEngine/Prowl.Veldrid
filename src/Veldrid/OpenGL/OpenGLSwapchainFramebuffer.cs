﻿
using System.Linq;

namespace Veldrid.OpenGL
{
    internal sealed class OpenGLSwapchainFramebuffer : Framebuffer
    {
        private readonly PixelFormat? _depthFormat;
        private bool _disposed;

        public override string? Name { get; set; }

        public override bool IsDisposed => _disposed;

        private readonly OpenGLPlaceholderTexture _colorTexture;
        private readonly OpenGLPlaceholderTexture? _depthTexture;

        public bool DisableSrgbConversion { get; }

        internal OpenGLSwapchainFramebuffer(
            uint width,
            uint height,
            PixelFormat colorFormat,
            PixelFormat? depthFormat,
            bool disableSrgbConversion)
        {
            _depthFormat = depthFormat;
            // This is wrong, but it's not really used.
            OutputAttachmentDescription? depthDesc = _depthFormat != null
                ? new OutputAttachmentDescription(_depthFormat.Value)
                : null;
            OutputDescription = new OutputDescription(
                depthDesc,
                new OutputAttachmentDescription(colorFormat));

            _colorTexture = new OpenGLPlaceholderTexture(
                width,
                height,
                colorFormat,
                TextureUsage.RenderTarget,
                TextureSampleCount.Count1);
            ColorTargets = new[] { new FramebufferAttachment(_colorTexture, 0) }.ToList();

            if (_depthFormat != null)
            {
                _depthTexture = new OpenGLPlaceholderTexture(
                    width,
                    height,
                    _depthFormat.Value,
                    TextureUsage.DepthStencil,
                    TextureSampleCount.Count1);
                DepthTarget = new FramebufferAttachment(_depthTexture, 0);
            }

            OutputDescription = OutputDescription.CreateFromFramebuffer(this);
            Width = width;
            Height = height;

            DisableSrgbConversion = disableSrgbConversion;
        }

        public void Resize(uint width, uint height)
        {
            _colorTexture.Resize(width, height);
            _depthTexture?.Resize(width, height);
            Width = width;
            Height = height;
        }

        public override void Dispose()
        {
            _disposed = true;
        }
    }
}
