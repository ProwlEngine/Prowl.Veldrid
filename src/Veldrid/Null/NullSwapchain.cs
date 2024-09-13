namespace Veldrid.Null
{
    internal sealed class NullSwapchain : Swapchain
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        private Framebuffer _framebuffer;
        public override Framebuffer Framebuffer => _framebuffer;

        public override bool SyncToVerticalBlank { get; set; }

        public override void Resize(uint width, uint height)
        {
        }

        public NullSwapchain(in SwapchainDescription description)
        {
            TextureDescription desc;

            desc.Height = description.Height;
            desc.Width = description.Width;
            desc.ArrayLayers = 1;
            desc.Depth = 1;
            desc.Format = PixelFormat.R8_G8_B8_A8_UNorm;
            desc.MipLevels = 1;
            desc.SampleCount = TextureSampleCount.Count1;
            desc.Type = TextureType.Texture2D;
            desc.Usage = TextureUsage.RenderTarget;

            _framebuffer = new NullFramebuffer(
                new(new NullTexture(desc), 0),
                [new(new NullTexture(desc), 0)]);
        }
    }
}
