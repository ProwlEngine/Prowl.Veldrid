using System;

namespace Veldrid.Null
{
    internal sealed class NullCommandList : CommandList
    {
        public override string? Name { get; set; }

        private bool _isDisposed = false;
        public override bool IsDisposed => _isDisposed;
        public override void Dispose() => _isDisposed = true;

        public NullCommandList(in CommandListDescription description, GraphicsDeviceFeatures features, uint uniformAlignment, uint structuredAlignment) : base(description, features, uniformAlignment, structuredAlignment)
        {
        }

        public override void Begin()
        {
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
        }

        public override void End()
        {
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
        }

        public override void SetViewport(uint index, in Viewport viewport)
        {
        }

        protected override void CopyBufferCore(DeviceBuffer source, DeviceBuffer destination, ReadOnlySpan<BufferCopyCommand> commands)
        {
        }

        protected override void CopyTextureCore(Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer, Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer, uint width, uint height, uint depth, uint layerCount)
        {
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, ReadOnlySpan<uint> dynamicOffsets)
        {
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
        }

        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, ReadOnlySpan<uint> dynamicOffsets)
        {
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
        }

        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
        }

        private protected override void InsertDebugMarkerCore(ReadOnlySpan<char> name)
        {
        }

        private protected override void PopDebugGroupCore()
        {
        }

        private protected override void PushDebugGroupCore(ReadOnlySpan<char> name)
        {
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, nint source, uint sizeInBytes)
        {
        }
    }
}
