namespace Veldrid.Null
{
    internal sealed class NullTexture : Texture
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        private protected override void DisposeCore() => _isDisposed = true;

        public NullTexture(in TextureDescription description)
        {
            Format = description.Format;
            Width = description.Width;
            Height = description.Height;
            Depth = description.Depth;
            ArrayLayers = description.ArrayLayers;
            MipLevels = description.MipLevels;
            Usage = description.Usage;
            Type = description.Type;
            SampleCount = description.SampleCount;
        }
    }
}
