using System;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static Veldrid.Vulkan.VulkanUtil;
using VulkanPipeline = Vortice.Vulkan.VkPipeline;

namespace Veldrid.Vulkan
{
    internal sealed unsafe class VkPipeline : Pipeline, IResourceRefCountTarget
    {
        private readonly VkGraphicsDevice _gd;
        private readonly VulkanPipeline _devicePipeline;
        private readonly VkPipelineLayout _pipelineLayout;
        private readonly VkRenderPass _renderPass;
        private string? _name;

        public VulkanPipeline DevicePipeline => _devicePipeline;

        public VkPipelineLayout PipelineLayout => _pipelineLayout;

        public uint ResourceSetCount { get; }
        public int DynamicOffsetsCount { get; }
        public uint VertexLayoutCount { get; }
        public bool ScissorTestEnabled { get; }

        public override bool IsComputePipeline { get; }

        public ResourceRefCount RefCount { get; }

        public override bool IsDisposed => RefCount.IsDisposed;

        public VkPipeline(VkGraphicsDevice gd, in GraphicsPipelineDescription description)
            : base(description)
        {
            _gd = gd;
            IsComputePipeline = false;
            RefCount = new ResourceRefCount(this);

            VkGraphicsPipelineCreateInfo pipelineCI = new()
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo
            };

            // Blend State
            int attachmentsCount = description.BlendState.AttachmentStates.Length;
            VkPipelineColorBlendAttachmentState* attachmentsPtr = stackalloc VkPipelineColorBlendAttachmentState[attachmentsCount];
            for (int i = 0; i < attachmentsCount; i++)
            {
                BlendAttachmentDescription vdDesc = description.BlendState.AttachmentStates[i];
                VkPipelineColorBlendAttachmentState attachmentState = new()
                {
                    srcColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceColorFactor),
                    dstColorBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationColorFactor),
                    colorBlendOp = VkFormats.VdToVkBlendOp(vdDesc.ColorFunction),
                    srcAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.SourceAlphaFactor),
                    dstAlphaBlendFactor = VkFormats.VdToVkBlendFactor(vdDesc.DestinationAlphaFactor),
                    alphaBlendOp = VkFormats.VdToVkBlendOp(vdDesc.AlphaFunction),
                    blendEnable = (VkBool32)vdDesc.BlendEnabled,
                    colorWriteMask = VkFormats.VdToVkColorWriteMask(vdDesc.ColorWriteMask.GetOrDefault()),
                };
                attachmentsPtr[i] = attachmentState;
            }

            VkPipelineColorBlendStateCreateInfo blendStateCI = new()
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                attachmentCount = (uint)attachmentsCount,
                pAttachments = attachmentsPtr
            };

            RgbaFloat blendFactor = description.BlendState.BlendFactor;
            blendStateCI.blendConstants[0] = blendFactor.R;
            blendStateCI.blendConstants[1] = blendFactor.G;
            blendStateCI.blendConstants[2] = blendFactor.B;
            blendStateCI.blendConstants[3] = blendFactor.A;

            pipelineCI.pColorBlendState = &blendStateCI;

            // Rasterizer State
            RasterizerStateDescription rsDesc = description.RasterizerState;
            VkPipelineRasterizationStateCreateInfo rsCI = new()
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                cullMode = VkFormats.VdToVkCullMode(rsDesc.CullMode),
                polygonMode = VkFormats.VdToVkPolygonMode(rsDesc.FillMode),
                depthClampEnable = (VkBool32)!rsDesc.DepthClipEnabled,
                frontFace = rsDesc.FrontFace == FrontFace.Clockwise
                    ? VkFrontFace.Clockwise
                    : VkFrontFace.CounterClockwise,
                lineWidth = 1f
            };

            pipelineCI.pRasterizationState = &rsCI;

            ScissorTestEnabled = rsDesc.ScissorTestEnabled;

            // Dynamic State
            VkDynamicState* dynamicStates = stackalloc VkDynamicState[2];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;
            VkPipelineDynamicStateCreateInfo dynamicStateCI = new()
            {
                sType = VkStructureType.PipelineDynamicStateCreateInfo,
                dynamicStateCount = 2,
                pDynamicStates = dynamicStates
            };

            pipelineCI.pDynamicState = &dynamicStateCI;

            // Depth Stencil State
            DepthStencilStateDescription vdDssDesc = description.DepthStencilState;
            VkPipelineDepthStencilStateCreateInfo dssCI = new()
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthWriteEnable = (VkBool32)vdDssDesc.DepthWriteEnabled,
                depthTestEnable = (VkBool32)vdDssDesc.DepthTestEnabled,
                depthCompareOp = VkFormats.VdToVkCompareOp(vdDssDesc.DepthComparison),
                stencilTestEnable = (VkBool32)vdDssDesc.StencilTestEnabled,
                front = new VkStencilOpState()
                {
                    failOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Fail),
                    passOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.Pass),
                    depthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilFront.DepthFail),
                    compareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilFront.Comparison),
                    compareMask = vdDssDesc.StencilReadMask,
                    writeMask = vdDssDesc.StencilWriteMask,
                    reference = vdDssDesc.StencilReference
                },
                back = new VkStencilOpState()
                {
                    failOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Fail),
                    passOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.Pass),
                    depthFailOp = VkFormats.VdToVkStencilOp(vdDssDesc.StencilBack.DepthFail),
                    compareOp = VkFormats.VdToVkCompareOp(vdDssDesc.StencilBack.Comparison),
                    compareMask = vdDssDesc.StencilReadMask,
                    writeMask = vdDssDesc.StencilWriteMask,
                    reference = vdDssDesc.StencilReference
                }
            };

            pipelineCI.pDepthStencilState = &dssCI;

            // Multisample
            VkSampleCountFlags vkSampleCount = VkFormats.VdToVkSampleCount(description.Outputs.SampleCount);
            VkPipelineMultisampleStateCreateInfo multisampleCI = new()
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = vkSampleCount,
                alphaToCoverageEnable = (VkBool32)description.BlendState.AlphaToCoverageEnabled
            };

            pipelineCI.pMultisampleState = &multisampleCI;

            // Input Assembly
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyCI = new()
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = VkFormats.VdToVkPrimitiveTopology(description.PrimitiveTopology)
            };

            pipelineCI.pInputAssemblyState = &inputAssemblyCI;

            // Vertex Input State

            ReadOnlySpan<VertexLayoutDescription> inputDescriptions = description.ShaderSet.VertexLayouts;
            uint bindingCount = (uint)inputDescriptions.Length;
            uint attributeCount = 0;
            for (int i = 0; i < inputDescriptions.Length; i++)
            {
                attributeCount += (uint)inputDescriptions[i].Elements.Length;
            }
            VkVertexInputBindingDescription* bindingDescs = stackalloc VkVertexInputBindingDescription[(int)bindingCount];
            VkVertexInputAttributeDescription* attributeDescs = stackalloc VkVertexInputAttributeDescription[(int)attributeCount];

            int targetIndex = 0;
            int targetLocation = 0;
            for (int binding = 0; binding < inputDescriptions.Length; binding++)
            {
                VertexLayoutDescription inputDesc = inputDescriptions[binding];
                bindingDescs[binding] = new VkVertexInputBindingDescription()
                {
                    binding = (uint)binding,
                    inputRate = (inputDesc.InstanceStepRate != 0)
                                    ? VkVertexInputRate.Instance
                                    : VkVertexInputRate.Vertex,
                    stride = inputDesc.Stride
                };

                uint currentOffset = 0;
                for (int location = 0; location < inputDesc.Elements.Length; location++)
                {
                    VertexElementDescription inputElement = inputDesc.Elements[location];

                    attributeDescs[targetIndex] = new VkVertexInputAttributeDescription()
                    {
                        format = VkFormats.VdToVkVertexElementFormat(inputElement.Format),
                        binding = (uint)binding,
                        location = (uint)(targetLocation + location),
                        offset = inputElement.Offset != 0 ? inputElement.Offset : currentOffset
                    };

                    targetIndex += 1;
                    currentOffset += FormatSizeHelpers.GetSizeInBytes(inputElement.Format);
                }

                targetLocation += inputDesc.Elements.Length;
            }

            VkPipelineVertexInputStateCreateInfo vertexInputCI = new()
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = bindingCount,
                pVertexBindingDescriptions = bindingDescs,
                vertexAttributeDescriptionCount = attributeCount,
                pVertexAttributeDescriptions = attributeDescs
            };

            pipelineCI.pVertexInputState = &vertexInputCI;

            // Shader Stage

            VkSpecializationInfo specializationInfo;
            SpecializationConstant[]? specDescs = description.ShaderSet.Specializations;
            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (SpecializationConstant spec in specDescs)
                {
                    specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                }
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                VkSpecializationMapEntry* mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
                uint specOffset = 0;
                for (int i = 0; i < specializationCount; i++)
                {
                    ulong data = specDescs[i].Data;
                    byte* srcData = (byte*)&data;
                    uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                    Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                    mapEntries[i].constantID = specDescs[i].ID;
                    mapEntries[i].offset = specOffset;
                    mapEntries[i].size = (UIntPtr)dataSize;
                    specOffset += dataSize;
                }
                specializationInfo.dataSize = (UIntPtr)specDataSize;
                specializationInfo.pData = fullSpecData;
                specializationInfo.mapEntryCount = (uint)specializationCount;
                specializationInfo.pMapEntries = mapEntries;
            }

            Shader[] shaders = description.ShaderSet.Shaders;
            StackList<VkPipelineShaderStageCreateInfo> stages = new();
            foreach (Shader shader in shaders)
            {
                VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
                VkPipelineShaderStageCreateInfo stageCI = new()
                {
                    sType = VkStructureType.PipelineShaderStageCreateInfo,
                    module = vkShader.ShaderModule,
                    stage = VkFormats.VdToVkShaderStages(shader.Stage),
                    pName = shader.EntryPoint == "main" ? CommonStrings.main : new FixedUtf8String(shader.EntryPoint),
                    pSpecializationInfo = &specializationInfo
                };
                stages.Add(stageCI);
            }

            pipelineCI.stageCount = stages.Count;
            pipelineCI.pStages = (VkPipelineShaderStageCreateInfo*)stages.Data;

            // ViewportState
            VkPipelineViewportStateCreateInfo viewportStateCI = new()
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                viewportCount = 1,
                scissorCount = 1
            };

            pipelineCI.pViewportState = &viewportStateCI;

            // Pipeline Layout
            ResourceLayout[] resourceLayouts = description.ResourceLayouts;
            VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++)
            {
                dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            }
            VkPipelineLayoutCreateInfo pipelineLayoutCI = new()
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = (uint)resourceLayouts.Length,
                pSetLayouts = dsls
            };

            VkPipelineLayout pipelineLayout;
            vkCreatePipelineLayout(_gd.Device, &pipelineLayoutCI, null, &pipelineLayout);
            _pipelineLayout = pipelineLayout;

            pipelineCI.layout = _pipelineLayout;

            // Create fake RenderPass for compatibility.

            OutputDescription outputDesc = description.Outputs;
            StackList<VkAttachmentDescription, Size512Bytes> attachments = new();

            // TODO: A huge portion of this next part is duplicated in VkFramebuffer.cs.

            StackList<VkAttachmentDescription> colorAttachmentDescs = new();
            StackList<VkAttachmentReference> colorAttachmentRefs = new();

            ReadOnlySpan<OutputAttachmentDescription> outputColorAttachmentDescs = outputDesc.ColorAttachments;
            for (int i = 0; i < outputColorAttachmentDescs.Length; i++)
            {
                ref VkAttachmentDescription desc = ref colorAttachmentDescs[i];
                desc.format = VkFormats.VdToVkPixelFormat(outputColorAttachmentDescs[i].Format, default);
                desc.samples = vkSampleCount;
                desc.loadOp = VkAttachmentLoadOp.DontCare;
                desc.storeOp = VkAttachmentStoreOp.Store;
                desc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
                desc.stencilStoreOp = VkAttachmentStoreOp.DontCare;
                desc.initialLayout = VkImageLayout.Undefined;
                desc.finalLayout = VkImageLayout.ShaderReadOnlyOptimal;
                attachments.Add(desc);

                colorAttachmentRefs[i].attachment = (uint)i;
                colorAttachmentRefs[i].layout = VkImageLayout.ColorAttachmentOptimal;
            }

            VkAttachmentDescription depthAttachmentDesc = new();
            VkAttachmentReference depthAttachmentRef = new();
            if (outputDesc.DepthAttachment != null)
            {
                PixelFormat depthFormat = outputDesc.DepthAttachment.GetValueOrDefault().Format;
                bool hasStencil = FormatHelpers.IsStencilFormat(depthFormat);
                depthAttachmentDesc.format = VkFormats.VdToVkPixelFormat(depthFormat, TextureUsage.DepthStencil);
                depthAttachmentDesc.samples = vkSampleCount;
                depthAttachmentDesc.loadOp = VkAttachmentLoadOp.DontCare;
                depthAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
                depthAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
                depthAttachmentDesc.stencilStoreOp = hasStencil
                    ? VkAttachmentStoreOp.Store
                    : VkAttachmentStoreOp.DontCare;
                depthAttachmentDesc.initialLayout = VkImageLayout.Undefined;
                depthAttachmentDesc.finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

                depthAttachmentRef.attachment = (uint)outputColorAttachmentDescs.Length;
                depthAttachmentRef.layout = VkImageLayout.DepthStencilAttachmentOptimal;
            }

            VkSubpassDescription subpass = new()
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = (uint)outputColorAttachmentDescs.Length,
                pColorAttachments = (VkAttachmentReference*)colorAttachmentRefs.Data
            };

            for (int i = 0; i < colorAttachmentDescs.Count; i++)
            {
                attachments.Add(colorAttachmentDescs[i]);
            }

            if (outputDesc.DepthAttachment != null)
            {
                subpass.pDepthStencilAttachment = &depthAttachmentRef;
                attachments.Add(depthAttachmentDesc);
            }

            VkSubpassDependency subpassDependency = new()
            {
                srcSubpass = VK_SUBPASS_EXTERNAL,
                srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite
            };

            VkRenderPassCreateInfo renderPassCI = new()
            {
                sType = VkStructureType.RenderPassCreateInfo,
                attachmentCount = attachments.Count,
                pAttachments = (VkAttachmentDescription*)attachments.Data,
                subpassCount = 1,
                pSubpasses = &subpass,
                dependencyCount = 1,
                pDependencies = &subpassDependency
            };

            VkRenderPass renderPass;
            VkResult creationResult = vkCreateRenderPass(_gd.Device, &renderPassCI, null, &renderPass);
            CheckResult(creationResult);
            _renderPass = renderPass;

            pipelineCI.renderPass = _renderPass;

            VulkanPipeline devicePipeline;
            VkResult result = vkCreateGraphicsPipelines(_gd.Device, default, 1, &pipelineCI, null, &devicePipeline);
            CheckResult(result);
            _devicePipeline = devicePipeline;

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (VkResourceLayout layout in description.ResourceLayouts)
            {
                DynamicOffsetsCount += layout.DynamicBufferCount;
            }
            VertexLayoutCount = (uint)inputDescriptions.Length;
        }

        public VkPipeline(VkGraphicsDevice gd, in ComputePipelineDescription description)
            : base(description)
        {
            _gd = gd;
            IsComputePipeline = true;
            RefCount = new ResourceRefCount(this);

            // Pipeline Layout
            ResourceLayout[] resourceLayouts = description.ResourceLayouts;
            VkDescriptorSetLayout* dsls = stackalloc VkDescriptorSetLayout[resourceLayouts.Length];
            for (int i = 0; i < resourceLayouts.Length; i++)
            {
                dsls[i] = Util.AssertSubtype<ResourceLayout, VkResourceLayout>(resourceLayouts[i]).DescriptorSetLayout;
            }

            VkPipelineLayoutCreateInfo pipelineLayoutCI = new()
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = (uint)resourceLayouts.Length,
                pSetLayouts = dsls
            };

            VkPipelineLayout pipelineLayout;
            vkCreatePipelineLayout(_gd.Device, &pipelineLayoutCI, null, &pipelineLayout);
            _pipelineLayout = pipelineLayout;

            // Shader Stage

            VkSpecializationInfo specializationInfo;
            SpecializationConstant[]? specDescs = description.Specializations;
            if (specDescs != null)
            {
                uint specDataSize = 0;
                foreach (SpecializationConstant spec in specDescs)
                {
                    specDataSize += VkFormats.GetSpecializationConstantSize(spec.Type);
                }
                byte* fullSpecData = stackalloc byte[(int)specDataSize];
                int specializationCount = specDescs.Length;
                VkSpecializationMapEntry* mapEntries = stackalloc VkSpecializationMapEntry[specializationCount];
                uint specOffset = 0;
                for (int i = 0; i < specializationCount; i++)
                {
                    ulong data = specDescs[i].Data;
                    byte* srcData = (byte*)&data;
                    uint dataSize = VkFormats.GetSpecializationConstantSize(specDescs[i].Type);
                    Unsafe.CopyBlock(fullSpecData + specOffset, srcData, dataSize);
                    mapEntries[i].constantID = specDescs[i].ID;
                    mapEntries[i].offset = specOffset;
                    mapEntries[i].size = (UIntPtr)dataSize;
                    specOffset += dataSize;
                }
                specializationInfo.dataSize = (UIntPtr)specDataSize;
                specializationInfo.pData = fullSpecData;
                specializationInfo.mapEntryCount = (uint)specializationCount;
                specializationInfo.pMapEntries = mapEntries;
            }

            Shader shader = description.ComputeShader;
            VkShader vkShader = Util.AssertSubtype<Shader, VkShader>(shader);
            VkPipelineShaderStageCreateInfo stageCI = new()
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                module = vkShader.ShaderModule,
                stage = VkFormats.VdToVkShaderStages(shader.Stage),
                pName = shader.EntryPoint == "main" ? CommonStrings.main : new FixedUtf8String(shader.EntryPoint),
                pSpecializationInfo = &specializationInfo
            };

            VkComputePipelineCreateInfo pipelineCI = new()
            {
                sType = VkStructureType.ComputePipelineCreateInfo,
                stage = stageCI,
                layout = _pipelineLayout
            };

            VulkanPipeline devicePipeline;
            VkResult result = vkCreateComputePipelines(
                 _gd.Device,
                 default,
                 1,
                 &pipelineCI,
                 null,
                 &devicePipeline);
            CheckResult(result);
            _devicePipeline = devicePipeline;

            ResourceSetCount = (uint)description.ResourceLayouts.Length;
            DynamicOffsetsCount = 0;
            foreach (VkResourceLayout layout in description.ResourceLayouts)
            {
                DynamicOffsetsCount += layout.DynamicBufferCount;
            }
        }

        public override string? Name
        {
            get => _name;
            set
            {
                _name = value;
                _gd.SetResourceName(this, value);
            }
        }

        public override void Dispose()
        {
            RefCount.DecrementDispose();
        }

        void IResourceRefCountTarget.RefZeroed()
        {
            vkDestroyPipelineLayout(_gd.Device, _pipelineLayout, null);
            vkDestroyPipeline(_gd.Device, _devicePipeline, null);
            if (!IsComputePipeline)
            {
                vkDestroyRenderPass(_gd.Device, _renderPass, null);
            }
        }
    }
}
