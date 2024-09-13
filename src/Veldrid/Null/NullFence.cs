namespace Veldrid.Null
{
    internal sealed class NullFence : Fence
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        public override bool Signaled => true;

        public override void Reset()
        {
        }
    }
}
