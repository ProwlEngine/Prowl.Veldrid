namespace Veldrid.Null
{
    internal sealed class NullGraphicsDevice : GraphicsDevice
    {
        public NullGraphicsDevice()
        {
            Features = new(true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true);
            ResourceFactory = new NullResourceFactory(Features);
            MainSwapchain = null;
            PostDeviceCreated();
        }

        public NullGraphicsDevice(in SwapchainDescription description) : this()
        {
            MainSwapchain = new NullSwapchain(description);
        }

        public override TextureSampleCount GetSampleCountLimit(PixelFormat format, bool depthFormat)
        {
            return TextureSampleCount.Count64;
        }

        public override void ResetFence(Fence fence)
        {
        }

        public override bool WaitForFence(Fence fence, ulong nanosecondTimeout)
        {
            return true;
        }

        public override bool WaitForFences(Fence[] fences, bool waitAll, ulong nanosecondTimeout)
        {
            return true;
        }

        private protected override bool GetPixelFormatSupportCore(PixelFormat format, TextureType type, TextureUsage usage, out PixelFormatProperties properties)
        {
            properties = new(uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue);
            return true;
        }

        private protected override MappedResource MapCore(MappableResource resource, uint bufferOffsetInBytes, uint sizeInBytes, MapMode mode, uint subresource)
        {
            return new MappedResource(resource, mode, 0, 0, 0, 0, 0, 0);
        }

        private protected override void SubmitCommandsCore(CommandList commandList, Fence? fence)
        {
        }

        private protected override void SwapBuffersCore(Swapchain swapchain)
        {
        }

        private protected override void UnmapCore(MappableResource resource, uint subresource)
        {
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, nint source, uint sizeInBytes)
        {
        }

        private protected override void UpdateTextureCore(Texture texture, nint source, uint sizeInBytes, uint x, uint y, uint z, uint width, uint height, uint depth, uint mipLevel, uint arrayLayer)
        {
        }

        private protected override void WaitForIdleCore()
        {
        }
    }
}
