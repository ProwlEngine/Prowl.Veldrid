namespace Veldrid.Null
{
    internal sealed class NullResourceFactory : ResourceFactory
    {
        public NullResourceFactory(GraphicsDeviceFeatures features) : base(features)
        {
        }

        public override GraphicsBackend BackendType => GraphicsBackend.Null;

        public override DeviceBuffer CreateBuffer(in BufferDescription description)
        {
            return new NullBuffer(description);
        }

        public override CommandList CreateCommandList(in CommandListDescription description)
        {
            return new NullCommandList(description, Features, 0, 0);
        }

        public override Pipeline CreateComputePipeline(in ComputePipelineDescription description)
        {
            return new NullPipeline(description);
        }

        public override Fence CreateFence(bool signaled)
        {
            return new NullFence();
        }

        public override Framebuffer CreateFramebuffer(in FramebufferDescription description)
        {
            return new NullFramebuffer(description);
        }

        public override Pipeline CreateGraphicsPipeline(in GraphicsPipelineDescription description)
        {
            return new NullPipeline(description);
        }

        public override ResourceLayout CreateResourceLayout(in ResourceLayoutDescription description)
        {
            return new NullResourceLayout(description);
        }

        public override ResourceSet CreateResourceSet(in ResourceSetDescription description)
        {
            return new NullResourceSet(description);
        }

        public override Sampler CreateSampler(in SamplerDescription description)
        {
            return new NullSampler();
        }

        public override Shader CreateShader(in ShaderDescription description)
        {
            return new NullShader(default, "");
        }

        public override Swapchain CreateSwapchain(in SwapchainDescription description)
        {
            return new NullSwapchain(description);
        }

        public override Texture CreateTexture(in TextureDescription description)
        {
            return new NullTexture(description);
        }

        public override Texture CreateTexture(ulong nativeTexture, in TextureDescription description)
        {
            return new NullTexture(description);
        }

        public override TextureView CreateTextureView(in TextureViewDescription description)
        {
            return new NullTextureView(description);
        }
    }
}
