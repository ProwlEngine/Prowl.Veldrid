using System;

using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

using static Veldrid.Vulkan.VulkanUtil;

using VulkanBuffer = Vortice.Vulkan.VkBuffer;

namespace Veldrid.Vulkan
{
    internal sealed unsafe class VkBuffer : DeviceBuffer, IResourceRefCountTarget
    {
        private readonly VkGraphicsDevice _gd;
        private readonly VulkanBuffer _deviceBuffer;
        private readonly VkMemoryBlock _memory;
        private readonly VkMemoryRequirements _bufferMemoryRequirements;
        public ResourceRefCount RefCount { get; }
        private string? _name;
        public override bool IsDisposed => RefCount.IsDisposed;

        public VulkanBuffer DeviceBuffer => _deviceBuffer;
        public VkMemoryBlock Memory => _memory;

        public VkMemoryRequirements BufferMemoryRequirements => _bufferMemoryRequirements;

        public VkBuffer(VkGraphicsDevice gd, in BufferDescription bd) : base(bd)
        {
            _gd = gd;

            VkBufferUsageFlags vkUsage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;
            if ((bd.Usage & BufferUsage.VertexBuffer) == BufferUsage.VertexBuffer)
            {
                vkUsage |= VkBufferUsageFlags.VertexBuffer;
            }
            if ((bd.Usage & BufferUsage.IndexBuffer) == BufferUsage.IndexBuffer)
            {
                vkUsage |= VkBufferUsageFlags.IndexBuffer;
            }
            if ((bd.Usage & BufferUsage.UniformBuffer) == BufferUsage.UniformBuffer)
            {
                vkUsage |= VkBufferUsageFlags.UniformBuffer;
            }
            if ((bd.Usage & BufferUsage.StructuredBufferReadWrite) == BufferUsage.StructuredBufferReadWrite
                || (bd.Usage & BufferUsage.StructuredBufferReadOnly) == BufferUsage.StructuredBufferReadOnly)
            {
                vkUsage |= VkBufferUsageFlags.StorageBuffer;
            }
            if ((bd.Usage & BufferUsage.IndirectBuffer) == BufferUsage.IndirectBuffer)
            {
                vkUsage |= VkBufferUsageFlags.IndirectBuffer;
            }

            VkBufferCreateInfo bufferCI = new()
            {
                sType = VkStructureType.BufferCreateInfo,
                size = bd.SizeInBytes,
                usage = vkUsage
            };
            VulkanBuffer deviceBuffer;
            VkResult result = vkCreateBuffer(gd.Device, &bufferCI, null, &deviceBuffer);
            CheckResult(result);
            _deviceBuffer = deviceBuffer;

            VkBool32 prefersDedicatedAllocation;
            if (_gd.GetBufferMemoryRequirements2 != null)
            {
                VkBufferMemoryRequirementsInfo2 memReqInfo2 = new()
                {
                    sType = VkStructureType.BufferMemoryRequirementsInfo2,
                    buffer = _deviceBuffer
                };
                VkMemoryDedicatedRequirements dedicatedReqs = new()
                {
                    sType = VkStructureType.MemoryDedicatedRequirements
                };
                VkMemoryRequirements2 memReqs2 = new()
                {
                    sType = VkStructureType.MemoryRequirements2,
                    pNext = &dedicatedReqs
                };
                _gd.GetBufferMemoryRequirements2(_gd.Device, &memReqInfo2, &memReqs2);
                _bufferMemoryRequirements = memReqs2.memoryRequirements;
                prefersDedicatedAllocation = dedicatedReqs.prefersDedicatedAllocation | dedicatedReqs.requiresDedicatedAllocation;
            }
            else
            {
                VkMemoryRequirements bufferMemoryRequirements;
                vkGetBufferMemoryRequirements(gd.Device, _deviceBuffer, &bufferMemoryRequirements);
                _bufferMemoryRequirements = bufferMemoryRequirements;
                prefersDedicatedAllocation = false;
            }

            bool isStaging = (bd.Usage & BufferUsage.StagingReadWrite) != 0;
            bool isDynamic = (bd.Usage & BufferUsage.DynamicReadWrite) != 0;
            bool hostVisible = isStaging || isDynamic;

            VkMemoryPropertyFlags memoryPropertyFlags = hostVisible
                ? VkMemoryPropertyFlags.HostVisible
                : VkMemoryPropertyFlags.DeviceLocal;

            if (isDynamic)
            {
                memoryPropertyFlags |= VkMemoryPropertyFlags.HostCoherent;
            }

            if ((bd.Usage & BufferUsage.StagingRead) != 0)
            {
                // Use "host cached" memory for staging when available, for better performance of GPU -> CPU transfers
                bool hostCachedAvailable = TryFindMemoryType(
                    gd.PhysicalDeviceMemProperties,
                    _bufferMemoryRequirements.memoryTypeBits,
                    memoryPropertyFlags | VkMemoryPropertyFlags.HostCached,
                    out _);

                if (hostCachedAvailable)
                {
                    memoryPropertyFlags |= VkMemoryPropertyFlags.HostCached;
                }
            }

            VkMemoryBlock memoryToken = gd.MemoryManager.Allocate(
                gd.PhysicalDeviceMemProperties,
                _bufferMemoryRequirements.memoryTypeBits,
                memoryPropertyFlags,
                hostVisible,
                _bufferMemoryRequirements.size,
                _bufferMemoryRequirements.alignment,
                prefersDedicatedAllocation,
                default,
                _deviceBuffer);
            _memory = memoryToken;
            result = vkBindBufferMemory(gd.Device, _deviceBuffer, _memory.DeviceMemory, _memory.Offset);
            CheckResult(result);

            RefCount = new ResourceRefCount(this);

            if (bd.InitialData != IntPtr.Zero)
            {
                gd.UpdateBuffer(this, 0, bd.InitialData, bd.SizeInBytes);
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
            vkDestroyBuffer(_gd.Device, _deviceBuffer, null);
            _gd.MemoryManager.Free(_memory);
        }
    }
}
