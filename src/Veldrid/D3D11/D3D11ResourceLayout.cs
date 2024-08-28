namespace Veldrid.D3D11
{
    internal sealed class D3D11ResourceLayout : ResourceLayout
    {
        private readonly ResourceBindingInfo[] _bindingInfosByVdIndex;
        private string? _name;
        private bool _disposed;

        public int ResourceCount { get; }

        public D3D11ResourceLayout(in ResourceLayoutDescription description)
            : base(description)
        {
            ResourceLayoutElementDescription[] elements = description.Elements;
            _bindingInfosByVdIndex = new ResourceBindingInfo[elements.Length];

            for (int i = 0; i < _bindingInfosByVdIndex.Length; i++)
            {
                _bindingInfosByVdIndex[i] = new ResourceBindingInfo(
                    i,
                    elements[i].Stages,
                    elements[i].Kind,
                    (elements[i].Options & ResourceLayoutElementOptions.DynamicBinding) != 0);
            }

            ResourceCount = elements.Length;
        }

        public ResourceBindingInfo GetDeviceSlotIndex(int resourceLayoutIndex)
        {
            if (resourceLayoutIndex >= _bindingInfosByVdIndex.Length)
            {
                void Throw()
                {
                    throw new VeldridException($"Invalid resource index: {resourceLayoutIndex}. Maximum is: {_bindingInfosByVdIndex.Length - 1}.");
                }
                Throw();
            }

            return _bindingInfosByVdIndex[resourceLayoutIndex];
        }

        public override string? Name
        {
            get => _name;
            set => _name = value;
        }

        public override bool IsDisposed => _disposed;

        public override void Dispose()
        {
            _disposed = true;
        }

        internal readonly struct ResourceBindingInfo
        {
            public readonly int Slot;
            public readonly ShaderStages Stages;
            public readonly ResourceKind Kind;
            public readonly bool DynamicBuffer;

            public ResourceBindingInfo(int slot, ShaderStages stages, ResourceKind kind, bool dynamicBuffer)
            {
                Slot = slot;
                Stages = stages;
                Kind = kind;
                DynamicBuffer = dynamicBuffer;
            }
        }
    }
}
