namespace Veldrid.MTL
{
    internal sealed class MTLResourceLayout : ResourceLayout
    {
        private readonly ResourceBindingInfo[] _bindingInfosByVdIndex;
        private bool _disposed;

        public uint ResourceCount { get; }
        public uint LastBufferIndex { get; }

#if !VALIDATE_USAGE
        public ResourceKind[] ResourceKinds { get; }
#endif
        public ResourceBindingInfo GetBindingInfo(int index) => _bindingInfosByVdIndex[index];

#if !VALIDATE_USAGE
        public ResourceLayoutDescription Description { get; }
#endif

        public MTLResourceLayout(in ResourceLayoutDescription description, MTLGraphicsDevice gd)
            : base(description)
        {
#if !VALIDATE_USAGE
            Description = description;
#endif

            ResourceLayoutElementDescription[] elements = description.Elements;
#if !VALIDATE_USAGE
            ResourceKinds = new ResourceKind[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                ResourceKinds[i] = elements[i].Kind;
            }
#endif

            uint bufferIndex = 0;

            _bindingInfosByVdIndex = new ResourceBindingInfo[elements.Length];

            for (int i = 0; i < _bindingInfosByVdIndex.Length; i++)
            {
                bufferIndex = elements[i].Kind switch
                {
                    ResourceKind.UniformBuffer => (uint)i,
                    ResourceKind.StructuredBufferReadOnly => (uint)i,
                    ResourceKind.StructuredBufferReadWrite => (uint)i,
                    _ => bufferIndex
                };

                _bindingInfosByVdIndex[i] = new ResourceBindingInfo(
                    (uint)i,
                    elements[i].Stages,
                    elements[i].Kind,
                    (elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0);
            }

            ResourceCount = (uint)elements.Length;
            LastBufferIndex = bufferIndex;
        }

        public override string? Name { get; set; }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            _disposed = true;
        }

        internal struct ResourceBindingInfo
        {
            public uint Slot;
            public ShaderStages Stages;
            public ResourceKind Kind;
            public bool DynamicBuffer;

            public ResourceBindingInfo(uint slot, ShaderStages stages, ResourceKind kind, bool dynamicBuffer)
            {
                Slot = slot;
                Stages = stages;
                Kind = kind;
                DynamicBuffer = dynamicBuffer;
            }
        }
    }
}
