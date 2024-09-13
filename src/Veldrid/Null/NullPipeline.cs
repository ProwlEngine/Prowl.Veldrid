namespace Veldrid.Null
{
    internal sealed class NullPipeline : Pipeline
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        private bool _isComputePipeline;
        public override bool IsComputePipeline => _isComputePipeline;

        public NullPipeline(in GraphicsPipelineDescription graphicsDescription) : base(graphicsDescription)
        {
            _isComputePipeline = false;
        }

        public NullPipeline(in ComputePipelineDescription computeDescription) : base(computeDescription)
        {
            _isComputePipeline = true;
        }
    }
}
