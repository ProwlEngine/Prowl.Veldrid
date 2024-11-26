using System;
using System.Diagnostics;
using System.Collections.Generic;

using Veldrid.MetalBindings;

namespace Veldrid.MTL
{
    internal sealed unsafe class MTLCommandList : CommandList
    {
        // public MTLCommandBuffer CommandBuffer => _cb;

        // private readonly MTLGraphicsDevice _gd;

        // public override bool IsDisposed => _disposed;
        // public override string? Name { get; set; }

        // private MTLFramebufferBase? _mtlFramebuffer;
        // private uint _viewportCount;
        // private bool _currentFramebufferEverActive;
        // private MTLRenderCommandEncoder _rce;
        // private MTLBlitCommandEncoder _bce;
        // private MTLComputeCommandEncoder _cce;
        // private RgbaFloat?[] _clearColors = Array.Empty<RgbaFloat?>();
        // private (float depth, byte stencil)? _clearDepth;
        // private MTLBuffer _indexBuffer;
        // private uint _ibOffset;
        // private MTLIndexType _indexType;
        // private new MTLPipeline _graphicsPipeline;
        // private bool _graphicsPipelineChanged;
        // private new MTLPipeline _computePipeline;
        // private bool _computePipelineChanged;
        // private MTLViewport[] _viewports = Array.Empty<MTLViewport>();
        // private bool _viewportsChanged;
        // private MTLScissorRect[] _scissorRects = Array.Empty<MTLScissorRect>();
        // private bool _scissorRectsChanged;
        // private uint _graphicsResourceSetCount;
        // private BoundResourceSetInfo[] _graphicsResourceSets;
        // private bool[] _graphicsResourceSetsActive;
        // private uint _computeResourceSetCount;
        // private BoundResourceSetInfo[] _computeResourceSets;
        // private bool[] _computeResourceSetsActive;
        // private uint _vertexBufferCount;
        // private uint _nonVertexBufferCount;
        // private MTLBuffer[] _vertexBuffers;
        // private uint[] _vbOffsets;
        // private bool[] _vertexBuffersActive;
        // private bool _disposed;


        public MTLCommandBuffer CommandBuffer => _cb;
        private readonly MTLGraphicsDevice _gd;

        public override bool IsDisposed => disposed;
        public override string Name { get; set; }

        private readonly List<MTLBuffer> availableStagingBuffers = new List<MTLBuffer>();
        private readonly CommandBufferUsageList<MTLBuffer> submittedStagingBuffers = new CommandBufferUsageList<MTLBuffer>();
        private readonly object submittedCommandsLock = new object();
        private readonly CommandBufferUsageList<MTLFence> completionFences = new CommandBufferUsageList<MTLFence>();

        private readonly Dictionary<UIntPtr, DeviceBufferRange> boundVertexBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();
        private readonly Dictionary<UIntPtr, DeviceBufferRange> boundFragmentBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();
        private readonly Dictionary<UIntPtr, DeviceBufferRange> boundComputeBuffers = new Dictionary<UIntPtr, DeviceBufferRange>();

        private readonly Dictionary<UIntPtr, MetalBindings.MTLTexture> boundVertexTextures = new Dictionary<UIntPtr, MetalBindings.MTLTexture>();
        private readonly Dictionary<UIntPtr, MetalBindings.MTLTexture> boundFragmentTextures = new Dictionary<UIntPtr, MetalBindings.MTLTexture>();
        private readonly Dictionary<UIntPtr, MetalBindings.MTLTexture> boundComputeTextures = new Dictionary<UIntPtr, MetalBindings.MTLTexture>();

        private readonly Dictionary<UIntPtr, MTLSamplerState> boundVertexSamplers = new Dictionary<UIntPtr, MTLSamplerState>();
        private readonly Dictionary<UIntPtr, MTLSamplerState> boundFragmentSamplers = new Dictionary<UIntPtr, MTLSamplerState>();
        private readonly Dictionary<UIntPtr, MTLSamplerState> boundComputeSamplers = new Dictionary<UIntPtr, MTLSamplerState>();

        private bool renderEncoderActive => !rce.IsNull;
        private bool blitEncoderActive => !bce.IsNull;
        private bool computeEncoderActive => !cce.IsNull;
        private MTLCommandBuffer _cb;
        private MTLFramebuffer mtlFramebuffer;
        private uint viewportCount;
        private bool currentFramebufferEverActive;
        private MTLRenderCommandEncoder rce;
        private MTLBlitCommandEncoder bce;
        private MTLComputeCommandEncoder cce;
        private RgbaFloat?[] clearColors = Array.Empty<RgbaFloat?>();
        private (float depth, byte stencil)? clearDepth;
        private MTLBuffer indexBuffer;
        private uint ibOffset;
        private MTLIndexType indexType;
        private MTLPipeline lastGraphicsPipeline;
        private MTLPipeline graphicsPipeline;
        private MTLPipeline lastComputePipeline;
        private MTLPipeline computePipeline;
        private MTLViewport[] viewports = Array.Empty<MTLViewport>();
        private bool viewportsChanged;
        private MTLScissorRect[] activeScissorRects = Array.Empty<MTLScissorRect>();
        private MTLScissorRect[] scissorRects = Array.Empty<MTLScissorRect>();
        private uint graphicsResourceSetCount;
        private BoundResourceSetInfo[] graphicsResourceSets;
        private bool[] graphicsResourceSetsActive;
        private uint computeResourceSetCount;
        private BoundResourceSetInfo[] computeResourceSets;
        private bool[] computeResourceSetsActive;
        private uint vertexBufferCount;
        private uint nonVertexBufferCount;
        private MTLBuffer[] vertexBuffers;
        private bool[] vertexBuffersActive;
        private uint[] vbOffsets;
        private bool[] vbOffsetsActive;
        private bool disposed;

        public MTLCommandList(in CommandListDescription description, MTLGraphicsDevice gd)
            : base(description, gd.Features, gd.UniformBufferMinOffsetAlignment, gd.StructuredBufferMinOffsetAlignment)
        {
            _gd = gd;
        }

        #region Disposal

        // public override void Dispose()
        // {
        //     if (!_disposed)
        //     {
        //         _disposed = true;
        //         EnsureNoRenderPass();
        //         if (_cb.NativePtr != IntPtr.Zero)
        //         {
        //             ObjectiveCRuntime.release(_cb.NativePtr);
        //         }
        //     }
        // }

        public override void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                EnsureNoRenderPass();

                lock (submittedStagingBuffers)
                {
                    foreach (var buffer in availableStagingBuffers)
                        buffer.Dispose();

                    foreach (var buffer in submittedStagingBuffers.EnumerateItems())
                        buffer.Dispose();

                    submittedStagingBuffers.Clear();
                }

                if (_cb.NativePtr != IntPtr.Zero) ObjectiveCRuntime.release(_cb.NativePtr);
            }
        }

        #endregion

        public MTLCommandBuffer Commit()
        {
            _cb.commit();
            var ret = _cb;
            _cb = default;
            return ret;
        }

        public override void Begin()
        {
            if (_cb.NativePtr != IntPtr.Zero)
            {
                ObjectiveCRuntime.release(_cb.NativePtr);
            }

            using (NSAutoreleasePool.Begin())
            {
                _cb = _gd.CommandQueue.commandBuffer();
                ObjectiveCRuntime.retain(_cb.NativePtr);
            }

            ClearCachedState();
        }

        public override void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            PreComputeCommand();
            cce.dispatchThreadGroups(
                new MTLSize(groupCountX, groupCountY, groupCountZ),
                computePipeline.ThreadsPerThreadgroup);
        }


        public override void End()
        {
            EnsureNoBlitEncoder();
            EnsureNoComputeEncoder();

            if (!currentFramebufferEverActive && mtlFramebuffer != null)
            {
                BeginCurrentRenderPass();
            }
            EnsureNoRenderPass();
        }

        public override void SetScissorRect(uint index, uint x, uint y, uint width, uint height)
        {
            scissorRects[index] = new MTLScissorRect(x, y, width, height);
        }


        public override void SetViewport(uint index, in Viewport viewport)
        {
            viewportsChanged = true;
            viewports[index] = new MTLViewport(
                viewport.X,
                viewport.Y,
                viewport.Width,
                viewport.Height,
                viewport.MinDepth,
                viewport.MaxDepth);
        }

        public void SetCompletionFence(MTLCommandBuffer cb, MTLFence fence)
        {
            lock (submittedCommandsLock)
            {
                Debug.Assert(!completionFences.Contains(cb));
                completionFences.Add(cb, fence);
            }
        }

        public void OnCompleted(MTLCommandBuffer cb)
        {
            lock (submittedCommandsLock)
            {
                foreach (var fence in completionFences.EnumerateAndRemove(cb))
                    fence.Set();

                foreach (var buffer in submittedStagingBuffers.EnumerateAndRemove(cb))
                    availableStagingBuffers.Add(buffer);
            }
        }

        private void CopyBufferCoreUnaligned(MTLBuffer mtlSrc, MTLBuffer mtlDst, ReadOnlySpan<BufferCopyCommand> commands)
        {
            // Unaligned copy -- use special compute shader.
            EnsureComputeEncoder();
            cce.setComputePipelineState(_gd.GetUnalignedBufferCopyPipeline());
            cce.setBuffer(mtlSrc.DeviceBuffer, UIntPtr.Zero, 0);
            cce.setBuffer(mtlDst.DeviceBuffer, UIntPtr.Zero, 1);

            foreach (ref readonly BufferCopyCommand command in commands)
            {
                if (command.Length == 0)
                {
                    continue;
                }

                MTLUnalignedBufferCopyInfo copyInfo;
                copyInfo.SourceOffset = (uint)command.ReadOffset;
                copyInfo.DestinationOffset = (uint)command.WriteOffset;
                copyInfo.CopySize = (uint)command.Length;

                cce.setBytes(&copyInfo, (UIntPtr)sizeof(MTLUnalignedBufferCopyInfo), 2);
                cce.dispatchThreadGroups(new MTLSize(1, 1, 1), new MTLSize(1, 1, 1));
            }
        }

        protected override void CopyBufferCore(
            DeviceBuffer source,
            DeviceBuffer destination,
            ReadOnlySpan<BufferCopyCommand> commands)
        {
            MTLBuffer mtlSrc = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(source);
            MTLBuffer mtlDst = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(destination);

            bool useComputeCopy = false;

            foreach (ref readonly BufferCopyCommand command in commands)
            {
                if (command.ReadOffset % 4 != 0 || command.WriteOffset % 4 != 0 || command.Length % 4 != 0)
                {
                    useComputeCopy = true;
                    break;
                }
            }

            if (useComputeCopy)
            {
                CopyBufferCoreUnaligned(mtlSrc, mtlDst, commands);
            }
            else
            {
                EnsureBlitEncoder();

                foreach (ref readonly BufferCopyCommand command in commands)
                {
                    if (command.Length == 0)
                    {
                        continue;
                    }

                    bce.copy(
                        mtlSrc.DeviceBuffer, (UIntPtr)command.ReadOffset,
                        mtlDst.DeviceBuffer, (UIntPtr)command.WriteOffset,
                        (UIntPtr)command.Length);
                }
            }
        }

        protected override void CopyTextureCore(
            Texture source, uint srcX, uint srcY, uint srcZ, uint srcMipLevel, uint srcBaseArrayLayer,
            Texture destination, uint dstX, uint dstY, uint dstZ, uint dstMipLevel, uint dstBaseArrayLayer,
            uint width, uint height, uint depth, uint layerCount)
        {
            EnsureBlitEncoder();
            MTLTexture srcMTLTexture = Util.AssertSubtype<Texture, MTLTexture>(source);
            MTLTexture dstMTLTexture = Util.AssertSubtype<Texture, MTLTexture>(destination);

            bool srcIsStaging = (source.Usage & TextureUsage.Staging) != 0;
            bool dstIsStaging = (destination.Usage & TextureUsage.Staging) != 0;
            if (srcIsStaging && !dstIsStaging)
            {
                // Staging -> Normal
                MetalBindings.MTLBuffer srcBuffer = srcMTLTexture.StagingBuffer;
                MetalBindings.MTLTexture dstTexture = dstMTLTexture.DeviceTexture;

                Util.GetMipDimensions(srcMTLTexture, srcMipLevel, out uint mipWidth, out uint mipHeight, out _);
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    uint blockSize = FormatHelpers.IsCompressedFormat(srcMTLTexture.Format) ? 4u : 1u;
                    uint compressedSrcX = srcX / blockSize;
                    uint compressedSrcY = srcY / blockSize;
                    uint blockSizeInBytes = blockSize == 1
                        ? FormatSizeHelpers.GetSizeInBytes(srcMTLTexture.Format)
                        : FormatHelpers.GetBlockSizeInBytes(srcMTLTexture.Format);

                    ulong srcSubresourceBase = Util.ComputeSubresourceOffset(
                        srcMTLTexture,
                        srcMipLevel,
                        layer + srcBaseArrayLayer);
                    srcMTLTexture.GetSubresourceLayout(
                        srcMipLevel,
                        srcBaseArrayLayer + layer,
                        out uint srcRowPitch,
                        out uint srcDepthPitch);
                    ulong sourceOffset = srcSubresourceBase
                        + srcDepthPitch * srcZ
                        + srcRowPitch * compressedSrcY
                        + blockSizeInBytes * compressedSrcX;

                    uint copyWidth = width > mipWidth && width <= blockSize
                        ? mipWidth
                        : width;

                    uint copyHeight = height > mipHeight && height <= blockSize
                        ? mipHeight
                        : height;

                    MTLSize sourceSize = new(copyWidth, copyHeight, depth);
                    if (dstMTLTexture.Type != TextureType.Texture3D)
                    {
                        srcDepthPitch = 0;
                    }
                    bce.copyFromBuffer(
                        srcBuffer,
                        (UIntPtr)sourceOffset,
                        srcRowPitch,
                        srcDepthPitch,
                        sourceSize,
                        dstTexture,
                        dstBaseArrayLayer + layer,
                        dstMipLevel,
                        new MTLOrigin(dstX, dstY, dstZ));
                }
            }
            else if (srcIsStaging) // DIFFERENT && dstIsStaging) // this line is slightly different
            {
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    // Staging -> Staging
                    ulong srcSubresourceBase = Util.ComputeSubresourceOffset(
                        srcMTLTexture,
                        srcMipLevel,
                        layer + srcBaseArrayLayer);
                    srcMTLTexture.GetSubresourceLayout(
                        srcMipLevel,
                        srcBaseArrayLayer + layer,
                        out uint srcRowPitch,
                        out uint srcDepthPitch);

                    ulong dstSubresourceBase = Util.ComputeSubresourceOffset(
                        dstMTLTexture,
                        dstMipLevel,
                        layer + dstBaseArrayLayer);
                    dstMTLTexture.GetSubresourceLayout(
                        dstMipLevel,
                        dstBaseArrayLayer + layer,
                        out uint dstRowPitch,
                        out uint dstDepthPitch);

                    uint blockSize = FormatHelpers.IsCompressedFormat(dstMTLTexture.Format) ? 4u : 1u;
                    if (blockSize == 1)
                    {
                        uint pixelSize = FormatSizeHelpers.GetSizeInBytes(dstMTLTexture.Format);
                        uint copySize = width * pixelSize;
                        for (uint zz = 0; zz < depth; zz++)
                            for (uint yy = 0; yy < height; yy++)
                            {
                                ulong srcRowOffset = srcSubresourceBase
                                    + srcDepthPitch * (zz + srcZ)
                                    + srcRowPitch * (yy + srcY)
                                    + pixelSize * srcX;
                                ulong dstRowOffset = dstSubresourceBase
                                    + dstDepthPitch * (zz + dstZ)
                                    + dstRowPitch * (yy + dstY)
                                    + pixelSize * dstX;
                                bce.copy(
                                    srcMTLTexture.StagingBuffer,
                                    (UIntPtr)srcRowOffset,
                                    dstMTLTexture.StagingBuffer,
                                    (UIntPtr)dstRowOffset,
                                    copySize);
                            }
                    }
                    else // blockSize != 1
                    {
                        uint paddedWidth = Math.Max(blockSize, width);
                        uint paddedHeight = Math.Max(blockSize, height);
                        uint numRows = FormatHelpers.GetNumRows(paddedHeight, srcMTLTexture.Format);
                        uint rowPitch = FormatHelpers.GetRowPitch(paddedWidth, srcMTLTexture.Format);

                        uint compressedSrcX = srcX / 4;
                        uint compressedSrcY = srcY / 4;
                        uint compressedDstX = dstX / 4;
                        uint compressedDstY = dstY / 4;
                        uint blockSizeInBytes = FormatHelpers.GetBlockSizeInBytes(srcMTLTexture.Format);

                        for (uint zz = 0; zz < depth; zz++)
                            for (uint row = 0; row < numRows; row++)
                            {
                                ulong srcRowOffset = srcSubresourceBase
                                    + srcDepthPitch * (zz + srcZ)
                                    + srcRowPitch * (row + compressedSrcY)
                                    + blockSizeInBytes * compressedSrcX;
                                ulong dstRowOffset = dstSubresourceBase
                                    + dstDepthPitch * (zz + dstZ)
                                    + dstRowPitch * (row + compressedDstY)
                                    + blockSizeInBytes * compressedDstX;
                                bce.copy(
                                    srcMTLTexture.StagingBuffer,
                                    (UIntPtr)srcRowOffset,
                                    dstMTLTexture.StagingBuffer,
                                    (UIntPtr)dstRowOffset,
                                    rowPitch);
                            }
                    }
                }
            }
            else if (dstIsStaging) //  // DIFFERENT (!srcIsStaging && dstIsStaging)
            {
                // Normal -> Staging
                MTLOrigin srcOrigin = new(srcX, srcY, srcZ);
                MTLSize srcSize = new(width, height, depth);

                for (uint layer = 0; layer < layerCount; layer++)
                {
                    dstMTLTexture.GetSubresourceLayout(
                        dstMipLevel,
                        dstBaseArrayLayer + layer,
                        out uint dstBytesPerRow,
                        out uint dstBytesPerImage);

                    Util.GetMipDimensions(srcMTLTexture, dstMipLevel, out uint mipWidth, out uint mipHeight, out _);
                    uint blockSize = FormatHelpers.IsCompressedFormat(srcMTLTexture.Format) ? 4u : 1u;
                    uint bufferRowLength = Math.Max(mipWidth, blockSize);
                    uint bufferImageHeight = Math.Max(mipHeight, blockSize);
                    uint compressedDstX = dstX / blockSize;
                    uint compressedDstY = dstY / blockSize;
                    uint blockSizeInBytes = blockSize == 1
                        ? FormatSizeHelpers.GetSizeInBytes(srcMTLTexture.Format)
                        : FormatHelpers.GetBlockSizeInBytes(srcMTLTexture.Format);
                    uint rowPitch = FormatHelpers.GetRowPitch(bufferRowLength, srcMTLTexture.Format);
                    uint depthPitch = FormatHelpers.GetDepthPitch(rowPitch, bufferImageHeight, srcMTLTexture.Format);

                    ulong dstOffset = Util.ComputeSubresourceOffset(dstMTLTexture, dstMipLevel, dstBaseArrayLayer + layer)
                        + (dstZ * depthPitch)
                        + (compressedDstY * rowPitch)
                        + (compressedDstX * blockSizeInBytes);

                    bce.copyTextureToBuffer(
                        srcMTLTexture.DeviceTexture,
                        srcBaseArrayLayer + layer,
                        srcMipLevel,
                        srcOrigin,
                        srcSize,
                        dstMTLTexture.StagingBuffer,
                        (UIntPtr)dstOffset,
                        dstBytesPerRow,
                        dstBytesPerImage);
                }
            }
            else
            {
                // Normal -> Normal
                for (uint layer = 0; layer < layerCount; layer++)
                {
                    bce.copyFromTexture(
                        srcMTLTexture.DeviceTexture,
                        srcBaseArrayLayer + layer,
                        srcMipLevel,
                        new MTLOrigin(srcX, srcY, srcZ),
                        new MTLSize(width, height, depth),
                        dstMTLTexture.DeviceTexture,
                        dstBaseArrayLayer + layer,
                        dstMipLevel,
                        new MTLOrigin(dstX, dstY, dstZ));
                }
            }
        }

        protected override void DispatchIndirectCore(DeviceBuffer indirectBuffer, uint offset)
        {
            MTLBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(indirectBuffer);
            PreComputeCommand();
            cce.dispatchThreadgroupsWithIndirectBuffer(
                mtlBuffer.DeviceBuffer,
                offset,
                computePipeline.ThreadsPerThreadgroup);
        }

        protected override void DrawIndexedIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (PreDrawCommand())
            {
                MTLBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(indirectBuffer);
                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    rce.drawIndexedPrimitives(
                        graphicsPipeline.PrimitiveType,
                        indexType,
                        indexBuffer.DeviceBuffer,
                        ibOffset,
                        mtlBuffer.DeviceBuffer,
                        currentOffset);
                }
            }
        }

        protected override void DrawIndirectCore(DeviceBuffer indirectBuffer, uint offset, uint drawCount, uint stride)
        {
            if (PreDrawCommand())
            {
                MTLBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(indirectBuffer);
                for (uint i = 0; i < drawCount; i++)
                {
                    uint currentOffset = i * stride + offset;
                    rce.drawPrimitives(graphicsPipeline.PrimitiveType, mtlBuffer.DeviceBuffer, currentOffset);
                }
            }
        }

        protected override void ResolveTextureCore(Texture source, Texture destination)
        {
            // TODO: This approach destroys the contents of the source Texture (according to the docs).
            EnsureNoBlitEncoder();
            EnsureNoRenderPass();

            MTLTexture mtlSrc = Util.AssertSubtype<Texture, MTLTexture>(source);
            MTLTexture mtlDst = Util.AssertSubtype<Texture, MTLTexture>(destination);

            MTLRenderPassDescriptor rpDesc = MTLRenderPassDescriptor.New();
            MTLRenderPassColorAttachmentDescriptor colorAttachment = rpDesc.colorAttachments[0];
            colorAttachment.texture = mtlSrc.DeviceTexture;
            colorAttachment.loadAction = MTLLoadAction.Load;
            colorAttachment.storeAction = MTLStoreAction.MultisampleResolve;
            colorAttachment.resolveTexture = mtlDst.DeviceTexture;

            using (NSAutoreleasePool.Begin())
            {
                MTLRenderCommandEncoder encoder = _cb.renderCommandEncoderWithDescriptor(rpDesc);
                encoder.endEncoding();
            }

            ObjectiveCRuntime.release(rpDesc.NativePtr);
        }

        protected override void SetComputeResourceSetCore(uint slot, ResourceSet set, ReadOnlySpan<uint> dynamicOffsets)
        {
            // ref BoundResourceSetInfo set = ref _computeResourceSets[slot];
            // if (!set.Equals(rs, dynamicOffsets))
            // {
            //     set.Offsets.Dispose();
            //     set = new BoundResourceSetInfo(rs, dynamicOffsets);
            //     _computeResourceSetsActive[slot] = false;
            // }
            if (!computeResourceSets[slot].Equals(set, dynamicOffsets))
            {
                computeResourceSets[slot].Offsets.Dispose();
                computeResourceSets[slot] = new BoundResourceSetInfo(set, dynamicOffsets);
                computeResourceSetsActive[slot] = false;
            }
        }

        protected override void SetFramebufferCore(Framebuffer fb)
        {
            if (!currentFramebufferEverActive && mtlFramebuffer != null)
            {
                // This ensures that any submitted clear values will be used even if nothing has been drawn.
                if (EnsureRenderPass())
                {
                    EndCurrentRenderPass();
                }
            }

            EnsureNoRenderPass();
            mtlFramebuffer = Util.AssertSubtype<Framebuffer, MTLFramebuffer>(fb);
            viewportCount = Math.Max(1u, (uint)fb.ColorTargets.Count);
            Util.EnsureArrayMinimumSize(ref viewports, viewportCount);
            Util.ClearArray(viewports);
            Util.EnsureArrayMinimumSize(ref scissorRects, viewportCount);
            Util.ClearArray(scissorRects);
            Util.EnsureArrayMinimumSize(ref clearColors, (uint)fb.ColorTargets.Count);
            Util.ClearArray(clearColors);
            currentFramebufferEverActive = false;
        }
        protected override void SetGraphicsResourceSetCore(uint slot, ResourceSet rs, ReadOnlySpan<uint> dynamicOffsets)
        {
            // ref BoundResourceSetInfo set = ref _graphicsResourceSets[slot];
            // if (!set.Equals(rs, dynamicOffsets))
            // {
            //     set.Offsets.Dispose();
            //     set = new BoundResourceSetInfo(rs, dynamicOffsets);
            //     _graphicsResourceSetsActive[slot] = false;
            // }
            if (!graphicsResourceSets[slot].Equals(rs, dynamicOffsets))
            {
                graphicsResourceSets[slot].Offsets.Dispose();
                graphicsResourceSets[slot] = new BoundResourceSetInfo(rs, dynamicOffsets);
                graphicsResourceSetsActive[slot] = false;
            }
        }

        private bool PreDrawCommand()
        {
            if (EnsureRenderPass())
            {
                if (viewportsChanged)
                {
                    FlushViewports();
                    viewportsChanged = false;
                }

                if (graphicsPipeline.ScissorTestEnabled)
                {
                    FlushScissorRects();
                }

                Debug.Assert(graphicsPipeline != null);

                if (graphicsPipeline.RenderPipelineState.NativePtr != lastGraphicsPipeline?.RenderPipelineState.NativePtr)
                    rce.setRenderPipelineState(graphicsPipeline.RenderPipelineState);

                if (graphicsPipeline.CullMode != lastGraphicsPipeline?.CullMode)
                    rce.setCullMode(graphicsPipeline.CullMode);

                if (graphicsPipeline.FrontFace != lastGraphicsPipeline?.FrontFace)
                    rce.setFrontFacing(graphicsPipeline.FrontFace);

                if (graphicsPipeline.FillMode != lastGraphicsPipeline?.FillMode)
                    rce.setTriangleFillMode(graphicsPipeline.FillMode);

                var blendColor = graphicsPipeline.BlendColor;
                if (blendColor != lastGraphicsPipeline?.BlendColor)
                    rce.setBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);

                // if (_graphicsPipelineChanged)
                // {
                //     Debug.Assert(_graphicsPipeline != null);
                //     _rce.setRenderPipelineState(_graphicsPipeline.RenderPipelineState);
                //     _rce.setCullMode(_graphicsPipeline.CullMode);
                //     _rce.setFrontFacing(_graphicsPipeline.FrontFace);
                //     _rce.setTriangleFillMode(_graphicsPipeline.FillMode);
                //     RgbaFloat blendColor = _graphicsPipeline.BlendColor;
                //     _rce.setBlendColor(blendColor.R, blendColor.G, blendColor.B, blendColor.A);
                //     if (_framebuffer!.DepthTarget != null)
                //     {
                //         _rce.setDepthStencilState(_graphicsPipeline.DepthStencilState);
                //         _rce.setDepthClipMode(_graphicsPipeline.DepthClipMode);
                //         _rce.setStencilReferenceValue(_graphicsPipeline.StencilReference);
                //     }
                // }
                if (_framebuffer?.DepthTarget != null)
                {
                    if (graphicsPipeline.DepthStencilState.NativePtr != lastGraphicsPipeline?.DepthStencilState.NativePtr)
                        rce.setDepthStencilState(graphicsPipeline.DepthStencilState);

                    if (graphicsPipeline.DepthClipMode != lastGraphicsPipeline?.DepthClipMode)
                        rce.setDepthClipMode(graphicsPipeline.DepthClipMode);

                    if (graphicsPipeline.StencilReference != lastGraphicsPipeline?.StencilReference)
                        rce.setStencilReferenceValue(graphicsPipeline.StencilReference);
                }

                // int graphicsSetCount = (int)_graphicsResourceSetCount;
                // Span<BoundResourceSetInfo> graphicsSets = _graphicsResourceSets.AsSpan(0, graphicsSetCount);
                // Span<bool> graphicsSetsActive = _graphicsResourceSetsActive.AsSpan(0, graphicsSetCount);
                // for (int i = 0; i < graphicsSetCount; i++)
                // {
                //     if (!graphicsSetsActive[i])
                //     {
                //         ActivateGraphicsResourceSet((uint)i, ref graphicsSets[i]);
                //         graphicsSetsActive[i] = true;
                //     }
                // }
                lastGraphicsPipeline = graphicsPipeline;

                for (uint i = 0; i < graphicsResourceSetCount; i++)
                {
                    if (!graphicsResourceSetsActive[i])
                    {
                        ActivateGraphicsResourceSet(i, ref graphicsResourceSets[i]);
                        graphicsResourceSetsActive[i] = true;
                    }
                }

                for (uint i = 0; i < vertexBufferCount; i++)
                {
                    if (!vertexBuffersActive[i])
                    {
                        UIntPtr index = graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? nonVertexBufferCount + i
                            : i;
                        rce.setVertexBuffer(
                            vertexBuffers[i].DeviceBuffer,
                            vbOffsets[i],
                            index);
                    }

                    if (!vbOffsetsActive[i])
                    {
                        UIntPtr index = graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                            ? nonVertexBufferCount + i
                            : i;

                        rce.setVertexBufferOffset(
                            vbOffsets[i],
                            index);

                        vbOffsetsActive[i] = true;
                    }
                }

                return true;
            }

            return false;
        }

        private void FlushViewports()
        {
            if (_gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                fixed (MTLViewport* viewportsPtr = &viewports[0])
                    rce.setViewports(viewportsPtr, viewportCount);
            }
            else
                rce.setViewport(viewports[0]);
        }

        private void FlushScissorRects()
        {
            if (_gd.MetalFeatures.IsSupported(MTLFeatureSet.macOS_GPUFamily1_v3))
            {
                bool scissorRectsChanged = false;

                for (int i = 0; i < scissorRects.Length; i++)
                {
                    scissorRectsChanged |= !scissorRects[i].Equals(activeScissorRects[i]);
                    activeScissorRects[i] = scissorRects[i];
                }

                if (scissorRectsChanged)
                {
                    fixed (MTLScissorRect* scissorRectsPtr = &scissorRects[0])
                        rce.setScissorRects(scissorRectsPtr, viewportCount);
                }
            }
            else
            {
                if (!scissorRects[0].Equals(activeScissorRects[0]))
                    rce.setScissorRect(scissorRects[0]);

                activeScissorRects[0] = scissorRects[0];
            }
        }

        private void PreComputeCommand()
        {
            EnsureComputeEncoder();

            if (computePipeline.ComputePipelineState.NativePtr != lastComputePipeline?.ComputePipelineState.NativePtr)
                cce.setComputePipelineState(computePipeline.ComputePipelineState);

            // int computeSetCount = (int)_computeResourceSetCount;
            // Span<BoundResourceSetInfo> computeSets = _computeResourceSets.AsSpan(0, computeSetCount);
            // Span<bool> computeSetsActive = _computeResourceSetsActive.AsSpan(0, computeSetCount);
            // for (int i = 0; i < computeSetCount; i++)
            // {
            //     if (!computeSetsActive[i])
            //     {
            //         ActivateComputeResourceSet((uint)i, ref computeSets[i]);
            //         computeSetsActive[i] = true;
            //     }
            // }

            for (uint i = 0; i < computeResourceSetCount; i++)
            {
                if (!computeResourceSetsActive[i])
                {
                    ActivateComputeResourceSet(i, computeResourceSets[i]);
                    computeResourceSetsActive[i] = true;
                }
            }
        }

        private MTLBuffer getFreeStagingBuffer(uint sizeInBytes)
        {
            lock (submittedCommandsLock)
            {
                foreach (var buffer in availableStagingBuffers)
                {
                    if (buffer.SizeInBytes >= sizeInBytes)
                    {
                        availableStagingBuffers.Remove(buffer);
                        return buffer;
                    }
                }
            }

            var staging = _gd.ResourceFactory.CreateBuffer(
                new BufferDescription(sizeInBytes, BufferUsage.StagingReadWrite));

            return Util.AssertSubtype<DeviceBuffer, MTLBuffer>(staging);
        }

        private void ActivateGraphicsResourceSet(uint slot, ref BoundResourceSetInfo brsi)
        {
            Debug.Assert(renderEncoderActive);
            MTLResourceSet mtlRS = Util.AssertSubtype<ResourceSet, MTLResourceSet>(brsi.Set);
            MTLResourceLayout layout = mtlRS.Layout;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < mtlRS.Resources.Length; i++)
            {
                MTLResourceLayout.ResourceBindingInfo bindingInfo = layout.GetBindingInfo(i);
                BindableResource resource = mtlRS.Resources[i];
                uint bufferOffset = 0;

                if (bindingInfo.DynamicBuffer)
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                switch (bindingInfo.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    case ResourceKind.TextureReadOnly:
                        TextureView texView = Util.GetTextureView(_gd, resource);
                        MTLTextureView mtlTexView = Util.AssertSubtype<TextureView, MTLTextureView>(texView);
                        BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        TextureView texViewRW = Util.GetTextureView(_gd, resource);
                        MTLTextureView mtlTexViewRW = Util.AssertSubtype<TextureView, MTLTextureView>(texViewRW);
                        BindTexture(mtlTexViewRW, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        MTLSampler mtlSampler = Util.AssertSubtype<Sampler, MTLSampler>(resource.GetSampler());
                        BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    case ResourceKind.StructuredBufferReadWrite:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void ActivateComputeResourceSet(uint slot, BoundResourceSetInfo brsi)
        {
            Debug.Assert(computeEncoderActive);
            MTLResourceSet mtlRS = Util.AssertSubtype<ResourceSet, MTLResourceSet>(brsi.Set);
            MTLResourceLayout layout = mtlRS.Layout;
            uint dynamicOffsetIndex = 0;

            for (int i = 0; i < mtlRS.Resources.Length; i++)
            {
                MTLResourceLayout.ResourceBindingInfo bindingInfo = layout.GetBindingInfo(i);
                BindableResource resource = mtlRS.Resources[i];
                uint bufferOffset = 0;

                if (bindingInfo.DynamicBuffer)
                {
                    bufferOffset = brsi.Offsets.Get(dynamicOffsetIndex);
                    dynamicOffsetIndex += 1;
                }

                switch (bindingInfo.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    case ResourceKind.TextureReadOnly:
                        TextureView texView = Util.GetTextureView(_gd, resource);
                        MTLTextureView mtlTexView = Util.AssertSubtype<TextureView, MTLTextureView>(texView);
                        BindTexture(mtlTexView, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.TextureReadWrite:
                        TextureView texViewRW = Util.GetTextureView(_gd, resource);
                        MTLTextureView mtlTexViewRW = Util.AssertSubtype<TextureView, MTLTextureView>(texViewRW);
                        BindTexture(mtlTexViewRW, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.Sampler:
                        MTLSampler mtlSampler = Util.AssertSubtype<Sampler, MTLSampler>(resource.GetSampler());
                        BindSampler(mtlSampler, slot, bindingInfo.Slot, bindingInfo.Stages);
                        break;

                    case ResourceKind.StructuredBufferReadOnly:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    case ResourceKind.StructuredBufferReadWrite:
                        {
                            DeviceBufferRange range = Util.GetBufferRange(resource, bufferOffset);
                            BindBuffer(range, slot, bindingInfo.Slot, bindingInfo.Stages);
                            break;
                        }

                    default:
                        throw Illegal.Value<ResourceKind>();
                }
            }
        }

        private void BindBuffer(DeviceBufferRange range, uint set, uint slot, ShaderStages stages)
        {
            MTLBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(range.Buffer);
            uint baseBuffer = getBufferBase(set, stages != ShaderStages.Compute);

            if (stages == ShaderStages.Compute)
            {
                UIntPtr index = slot + baseBuffer;

                // _cce.setBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);

                if (!boundComputeBuffers.TryGetValue(index, out var boundBuffer) || !range.Equals(boundBuffer))
                {
                    cce.setBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                    boundComputeBuffers[index] = range;
                }
            }
            else
            {
                // if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
                // {
                //     UIntPtr index = _graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                //         ? slot + baseBuffer
                //         : slot + _vertexBufferCount + baseBuffer;
                //     _rce.setVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                // }
                // if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
                // {
                //     _rce.setFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                // }

                if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
                {
                    UIntPtr index = graphicsPipeline.ResourceBindingModel == ResourceBindingModel.Improved
                        ? slot + baseBuffer
                        : slot + vertexBufferCount + baseBuffer;

                    if (!boundVertexBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        rce.setVertexBuffer(mtlBuffer.DeviceBuffer, range.Offset, index);
                        boundVertexBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        rce.setVertexBufferOffset(range.Offset, index);
                        boundVertexBuffers[index] = range;
                    }
                }

                if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
                {
                    UIntPtr index = slot + baseBuffer;

                    if (!boundFragmentBuffers.TryGetValue(index, out var boundBuffer) || boundBuffer.Buffer != range.Buffer)
                    {
                        rce.setFragmentBuffer(mtlBuffer.DeviceBuffer, range.Offset, slot + baseBuffer);
                        boundFragmentBuffers[index] = range;
                    }
                    else if (!range.Equals(boundBuffer))
                    {
                        rce.setFragmentBufferOffset(range.Offset, slot + baseBuffer);
                        boundFragmentBuffers[index] = range;
                    }
                }
            }
        }

        private void BindTexture(MTLTextureView mtlTexView, uint set, uint slot, ShaderStages stages)
        {
            // uint baseTexture = GetResourceBase(set, stages != ShaderStages.Compute);
            // if (stages == ShaderStages.Compute)
            // {
            //     _cce.setTexture(mtlTexView.TargetDeviceTexture, slot + baseTexture);
            // }
            // if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            // {
            //     _rce.setVertexTexture(mtlTexView.TargetDeviceTexture, slot + baseTexture);
            // }
            // if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            // {
            //     _rce.setFragmentTexture(mtlTexView.TargetDeviceTexture, slot + baseTexture);
            // }

            uint baseTexture = getTextureBase(set, stages != ShaderStages.Compute);
            UIntPtr index = slot + baseTexture;

            if (stages == ShaderStages.Compute && (!boundComputeTextures.TryGetValue(index, out var computeTexture) || computeTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                cce.setTexture(mtlTexView.TargetDeviceTexture, index);
                boundComputeTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!boundVertexTextures.TryGetValue(index, out var vertexTexture) || vertexTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                rce.setVertexTexture(mtlTexView.TargetDeviceTexture, index);
                boundVertexTextures[index] = mtlTexView.TargetDeviceTexture;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!boundFragmentTextures.TryGetValue(index, out var fragmentTexture) || fragmentTexture.NativePtr != mtlTexView.TargetDeviceTexture.NativePtr))
            {
                rce.setFragmentTexture(mtlTexView.TargetDeviceTexture, index);
                boundFragmentTextures[index] = mtlTexView.TargetDeviceTexture;
            }
        }

        private void BindSampler(MTLSampler mtlSampler, uint set, uint slot, ShaderStages stages)
        {
            uint baseSampler = getSamplerBase(set, stages != ShaderStages.Compute);
            // if (stages == ShaderStages.Compute)
            // {
            //     _cce.setSamplerState(mtlSampler.DeviceSampler, slot + baseSampler);
            // }
            // if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            // {
            //     _rce.setVertexSamplerState(mtlSampler.DeviceSampler, slot + baseSampler);
            // }
            // if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            // {
            //     _rce.setFragmentSamplerState(mtlSampler.DeviceSampler, slot + baseSampler);
            // }
            UIntPtr index = slot + baseSampler;

            if (stages == ShaderStages.Compute && (!boundComputeSamplers.TryGetValue(index, out var computeSampler) || computeSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                cce.setSamplerState(mtlSampler.DeviceSampler, index);
                boundComputeSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex
                && (!boundVertexSamplers.TryGetValue(index, out var vertexSampler) || vertexSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                rce.setVertexSamplerState(mtlSampler.DeviceSampler, index);
                boundVertexSamplers[index] = mtlSampler.DeviceSampler;
            }

            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment
                && (!boundFragmentSamplers.TryGetValue(index, out var fragmentSampler) || fragmentSampler.NativePtr != mtlSampler.DeviceSampler.NativePtr))
            {
                rce.setFragmentSamplerState(mtlSampler.DeviceSampler, index);
                boundFragmentSamplers[index] = mtlSampler.DeviceSampler;
            }
        }

        private uint getBufferBase(uint set, bool graphics)
        {
            MTLResourceLayout[] layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].BufferCount;
            }

            return ret;
        }

        private uint getTextureBase(uint set, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].TextureCount;
            }

            return ret;
        }

        private uint getSamplerBase(uint set, bool graphics)
        {
            var layouts = graphics ? graphicsPipeline.ResourceLayouts : computePipeline.ResourceLayouts;
            uint ret = 0;

            for (int i = 0; i < set; i++)
            {
                Debug.Assert(layouts[i] != null);
                ret += layouts[i].SamplerCount;
            }

            return ret;
        }

        private bool EnsureRenderPass()
        {
            Debug.Assert(mtlFramebuffer != null);
            EnsureNoBlitEncoder();
            EnsureNoComputeEncoder();
            return renderEncoderActive || BeginCurrentRenderPass();
        }


        private bool BeginCurrentRenderPass()
        {
            // Debug.Assert(_mtlFramebuffer != null);
            // if (!_mtlFramebuffer.IsRenderable)
            // {
            //     return false;
            // }
            if (mtlFramebuffer is MTLSwapchainFramebuffer swapchainFramebuffer && !swapchainFramebuffer.EnsureDrawableAvailable())
                return false;

            MTLRenderPassDescriptor rpDesc = mtlFramebuffer.CreateRenderPassDescriptor();
            for (uint i = 0; i < clearColors.Length; i++)
            {
                RgbaFloat? clearColor = clearColors[i];
                if (clearColor.HasValue)
                {
                    MTLRenderPassColorAttachmentDescriptor attachment = rpDesc.colorAttachments[0];
                    attachment.loadAction = MTLLoadAction.Clear;
                    RgbaFloat c = clearColor.GetValueOrDefault();
                    attachment.clearColor = new MTLClearColor(c.R, c.G, c.B, c.A);
                    clearColors[i] = null;
                }
            }

            if (clearDepth != null)
            {
                MTLRenderPassDepthAttachmentDescriptor depthAttachment = rpDesc.depthAttachment;
                depthAttachment.loadAction = MTLLoadAction.Clear;
                depthAttachment.clearDepth = clearDepth.GetValueOrDefault().depth;

                if (FormatHelpers.IsStencilFormat(mtlFramebuffer.DepthTarget!.Value.Target.Format))
                {
                    MTLRenderPassStencilAttachmentDescriptor stencilAttachment = rpDesc.stencilAttachment;
                    stencilAttachment.loadAction = MTLLoadAction.Clear;
                    stencilAttachment.clearStencil = clearDepth.GetValueOrDefault().stencil;
                }

                clearDepth = null;
            }

            using (NSAutoreleasePool.Begin())
            {
                rce = _cb.renderCommandEncoderWithDescriptor(rpDesc);
                ObjectiveCRuntime.retain(rce.NativePtr);
            }

            ObjectiveCRuntime.release(rpDesc.NativePtr);
            currentFramebufferEverActive = true;

            return true;
        }

        private void EnsureNoRenderPass()
        {
            if (renderEncoderActive)
            {
                EndCurrentRenderPass();
            }

            Debug.Assert(!renderEncoderActive);
        }

        private void EndCurrentRenderPass()
        {
            rce.endEncoding();
            ObjectiveCRuntime.release(rce.NativePtr);
            rce = default;

            // _graphicsPipelineChanged = true;
            // Util.ClearArray(_graphicsResourceSetsActive);
            // _viewportsChanged = true;
            // _scissorRectsChanged = true;

            lastGraphicsPipeline = null;
            boundVertexBuffers.Clear();
            boundVertexTextures.Clear();
            boundVertexSamplers.Clear();
            boundFragmentBuffers.Clear();
            boundFragmentTextures.Clear();
            boundFragmentSamplers.Clear();
            Util.ClearArray(graphicsResourceSetsActive);
            Util.ClearArray(vertexBuffersActive);
            Util.ClearArray(vbOffsetsActive);

            Util.ClearArray(activeScissorRects);

            viewportsChanged = true;
        }

        private void EnsureBlitEncoder()
        {
            if (!blitEncoderActive)
            {
                EnsureNoRenderPass();
                EnsureNoComputeEncoder();
                using (NSAutoreleasePool.Begin())
                {
                    bce = _cb.blitCommandEncoder();
                    ObjectiveCRuntime.retain(bce.NativePtr);
                }
            }

            Debug.Assert(blitEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!computeEncoderActive);
        }

        private void EnsureNoBlitEncoder()
        {
            if (blitEncoderActive)
            {
                bce.endEncoding();
                ObjectiveCRuntime.release(bce.NativePtr);
                bce = default;
            }

            Debug.Assert(!blitEncoderActive);
        }


        private void EnsureComputeEncoder()
        {
            if (!computeEncoderActive)
            {
                EnsureNoBlitEncoder();
                EnsureNoRenderPass();

                using (NSAutoreleasePool.Begin())
                {
                    cce = _cb.computeCommandEncoder();
                    ObjectiveCRuntime.retain(cce.NativePtr);
                }
            }

            Debug.Assert(computeEncoderActive);
            Debug.Assert(!renderEncoderActive);
            Debug.Assert(!blitEncoderActive);
        }

        private void EnsureNoComputeEncoder()
        {
            if (computeEncoderActive)
            {
                cce.endEncoding();
                ObjectiveCRuntime.release(cce.NativePtr);
                cce = default;

                // computePipelineChanged = true;
                // Util.ClearArray(_computeResourceSetsActive);

                boundComputeBuffers.Clear();
                boundComputeTextures.Clear();
                boundComputeSamplers.Clear();
                lastComputePipeline = null;

                Util.ClearArray(computeResourceSetsActive);
            }

            Debug.Assert(!computeEncoderActive);
        }

        private protected override void ClearColorTargetCore(uint index, RgbaFloat clearColor)
        {
            EnsureNoRenderPass();
            clearColors[index] = clearColor;
        }

        private protected override void ClearDepthStencilCore(float depth, byte stencil)
        {
            EnsureNoRenderPass();
            clearDepth = (depth, stencil);
        }


        private protected override void DrawCore(uint vertexCount, uint instanceCount, uint vertexStart, uint instanceStart)
        {
            if (PreDrawCommand())
            {
                if (instanceStart == 0)
                {
                    rce.drawPrimitives(
                        graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount);
                }
                else
                {
                    rce.drawPrimitives(
                        graphicsPipeline.PrimitiveType,
                        vertexStart,
                        vertexCount,
                        instanceCount,
                        instanceStart);

                }
            }
        }

        private protected override void DrawIndexedCore(uint indexCount, uint instanceCount, uint indexStart, int vertexOffset, uint instanceStart)
        {
            if (PreDrawCommand())
            {
                uint indexSize = indexType == MTLIndexType.UInt16 ? 2u : 4u;
                uint indexBufferOffset = (indexSize * indexStart) + ibOffset;

                if (vertexOffset == 0 && instanceStart == 0)
                {
                    rce.drawIndexedPrimitives(
                        graphicsPipeline.PrimitiveType,
                        indexCount,
                        indexType,
                        indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount);
                }
                else
                {
                    rce.drawIndexedPrimitives(
                        graphicsPipeline.PrimitiveType,
                        indexCount,
                        indexType,
                        indexBuffer.DeviceBuffer,
                        indexBufferOffset,
                        instanceCount,
                        vertexOffset,
                        instanceStart);
                }
            }
        }

        private protected override void SetPipelineCore(Pipeline pipeline)
        {
            if (pipeline.IsComputePipeline && computePipeline != pipeline)
            {
                computePipeline = Util.AssertSubtype<Pipeline, MTLPipeline>(pipeline);
                computeResourceSetCount = (uint)computePipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref computeResourceSets, computeResourceSetCount);
                Util.EnsureArrayMinimumSize(ref computeResourceSetsActive, computeResourceSetCount);
                Util.ClearArray(computeResourceSetsActive);
            }
            else if (!pipeline.IsComputePipeline && graphicsPipeline != pipeline)
            {
                graphicsPipeline = Util.AssertSubtype<Pipeline, MTLPipeline>(pipeline);
                graphicsResourceSetCount = (uint)graphicsPipeline.ResourceLayouts.Length;
                Util.EnsureArrayMinimumSize(ref graphicsResourceSets, graphicsResourceSetCount);
                Util.EnsureArrayMinimumSize(ref graphicsResourceSetsActive, graphicsResourceSetCount);
                Util.ClearArray(graphicsResourceSetsActive);

                nonVertexBufferCount = graphicsPipeline.NonVertexBufferCount;

                vertexBufferCount = graphicsPipeline.VertexBufferCount;
                Util.EnsureArrayMinimumSize(ref vertexBuffers, vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref vbOffsets, vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref vertexBuffersActive, vertexBufferCount);
                Util.EnsureArrayMinimumSize(ref vbOffsetsActive, vertexBufferCount);
                Util.ClearArray(vertexBuffersActive);
                Util.ClearArray(vbOffsetsActive);
            }
        }

        private protected override void UpdateBufferCore(DeviceBuffer buffer, uint bufferOffsetInBytes, IntPtr source, uint sizeInBytes)
        {
            bool useComputeCopy = (bufferOffsetInBytes % 4 != 0)
                || (sizeInBytes % 4 != 0 && bufferOffsetInBytes != 0 && sizeInBytes != buffer.SizeInBytes);

            var dstMTLBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(buffer);
            // TODO: Cache these, and rely on the command buffer's completion callback to add them back to a shared pool.
            // using MTLBuffer copySrc = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(
            //     _gd.ResourceFactory.CreateBuffer(new BufferDescription(sizeInBytes, BufferUsage.StagingWrite)));
            var staging = getFreeStagingBuffer(sizeInBytes);

            _gd.UpdateBuffer(staging, 0, source, sizeInBytes);

            if (useComputeCopy)
            {
                BufferCopyCommand command = new(0, bufferOffsetInBytes, sizeInBytes);
                CopyBufferCoreUnaligned(staging, dstMTLBuffer, [command]);
            }
            else
            {
                Debug.Assert(bufferOffsetInBytes % 4 == 0);
                uint sizeRoundFactor = (4 - (sizeInBytes % 4)) % 4;
                EnsureBlitEncoder();
                bce.copy(
                    staging.DeviceBuffer, UIntPtr.Zero,
                    dstMTLBuffer.DeviceBuffer, bufferOffsetInBytes,
                    sizeInBytes + sizeRoundFactor);
            }
        }

        private protected override void GenerateMipmapsCore(Texture texture)
        {
            Debug.Assert(texture.MipLevels > 1);
            EnsureBlitEncoder();
            MTLTexture mtlTex = Util.AssertSubtype<Texture, MTLTexture>(texture);
            bce.generateMipmapsForTexture(mtlTex.DeviceTexture);
        }

        private protected override void SetIndexBufferCore(DeviceBuffer buffer, IndexFormat format, uint offset)
        {
            indexBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(buffer);
            ibOffset = offset;
            indexType = MTLFormats.VdToMTLIndexFormat(format);
        }

        private protected override void SetVertexBufferCore(uint index, DeviceBuffer buffer, uint offset)
        {
            // Util.EnsureArrayMinimumSize(ref vertexBuffers, index + 1);
            // Util.EnsureArrayMinimumSize(ref vbOffsets, index + 1);
            // Util.EnsureArrayMinimumSize(ref vertexBuffersActive, index + 1);
            // if (vertexBuffers[index] != buffer || vbOffsets[index] != offset)
            // {
            //     MTLBuffer mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(buffer);
            //     vertexBuffers[index] = mtlBuffer;
            //     vbOffsets[index] = offset;
            //     vertexBuffersActive[index] = false;
            // }
            Util.EnsureArrayMinimumSize(ref vertexBuffers, index + 1);
            Util.EnsureArrayMinimumSize(ref vbOffsets, index + 1);
            Util.EnsureArrayMinimumSize(ref vertexBuffersActive, index + 1);
            Util.EnsureArrayMinimumSize(ref vbOffsetsActive, index + 1);

            if (vertexBuffers[index] != buffer)
            {
                var mtlBuffer = Util.AssertSubtype<DeviceBuffer, MTLBuffer>(buffer);
                vertexBuffers[index] = mtlBuffer;
                vertexBuffersActive[index] = false;
            }

            if (vbOffsets[index] != offset)
            {
                vbOffsets[index] = offset;
                vbOffsetsActive[index] = false;
            }
        }

        // private uint GetResourceBase(uint set, bool graphics)
        // {
        //     MTLResourceLayout[] layouts = graphics ? _graphicsPipeline.ResourceLayouts : _computePipeline.ResourceLayouts;

        //     uint ret = 0;
        //     for (int i = 0; i < set; i++)
        //     {
        //         Debug.Assert(layouts[i] != null);
        //         ret += layouts[i].BufferCount;
        //     }

        //     return ret;
        // }

        private protected override void PushDebugGroupCore(ReadOnlySpan<char> name)
        {
            NSString nsName = NSString.New(name);
            if (!bce.IsNull)
                bce.pushDebugGroup(nsName);
            else if (!cce.IsNull)
                cce.pushDebugGroup(nsName);
            else if (!rce.IsNull)
                rce.pushDebugGroup(nsName);

            ObjectiveCRuntime.release(nsName);
        }

        private protected override void PopDebugGroupCore()
        {
            if (!bce.IsNull)
                bce.popDebugGroup();
            else if (!cce.IsNull)
                cce.popDebugGroup();
            else if (!rce.IsNull)
                rce.popDebugGroup();
        }

        private protected override void InsertDebugMarkerCore(ReadOnlySpan<char> name)
        {
            NSString nsName = NSString.New(name);
            if (!bce.IsNull)
                bce.insertDebugSignpost(nsName);
            else if (!cce.IsNull)
                cce.insertDebugSignpost(nsName);
            else if (!rce.IsNull)
                rce.insertDebugSignpost(nsName);

            ObjectiveCRuntime.release(nsName);
        }
    }
}
