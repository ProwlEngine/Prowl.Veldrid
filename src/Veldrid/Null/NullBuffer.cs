namespace Veldrid.Null
{
    internal sealed class NullBuffer : DeviceBuffer
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        public NullBuffer(in BufferDescription description) : base(description)
        {
        }
    }
}
