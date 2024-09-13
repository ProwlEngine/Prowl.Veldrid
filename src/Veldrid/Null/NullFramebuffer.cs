namespace Veldrid.Null
{
    internal sealed class NullFramebuffer : Framebuffer
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;


        public NullFramebuffer(in FramebufferDescription description) : base(description.DepthTarget, description.ColorTargets)
        {
        }


        public NullFramebuffer(in FramebufferAttachmentDescription? depth, in FramebufferAttachmentDescription[]? colors) : base(depth, colors)
        {
        }
    }
}
