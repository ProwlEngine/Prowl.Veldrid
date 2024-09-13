namespace Veldrid.Null
{
    internal sealed class NullResourceSet : ResourceSet
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        public NullResourceSet(in ResourceSetDescription description) : base(description)
        {
        }
    }
}
