using System;
using System.Diagnostics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Veldrid.Vulkan
{
    internal unsafe static class VulkanUtil
    {
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                ThrowResult(result);
            }
        }

        public static void ThrowResult(VkResult result)
        {
            if (result == VkResult.ErrorOutOfDeviceMemory ||
                result == VkResult.ErrorOutOfHostMemory)
            {
                throw new VeldridOutOfMemoryException(GetExceptionMessage(result));
            }
            else
            {
                throw new VeldridException(GetExceptionMessage(result));
            }
        }

        private static string GetExceptionMessage(VkResult result)
        {
            return "Unsuccessful VkResult: " + result;
        }

        public static bool TryFindMemoryType(
            VkPhysicalDeviceMemoryProperties memProperties, uint typeFilter, VkMemoryPropertyFlags properties, out uint typeIndex)
        {
            for (uint i = 0; i < memProperties.memoryTypeCount; i++)
            {
                if (((typeFilter & (1u << (int)i)) != 0)
                    && (memProperties.memoryTypes[(int)i].propertyFlags & properties) == properties)
                {
                    typeIndex = i;
                    return true;
                }
            }

            typeIndex = 0;
            return false;
        }

        public static string[] EnumerateInstanceLayers()
        {
            uint propCount = 0;
            VkResult result = vkEnumerateInstanceLayerProperties(&propCount, null);
            CheckResult(result);
            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            VkLayerProperties[] props = new VkLayerProperties[propCount];
            string[] ret = new string[propCount];

            fixed (VkLayerProperties* propPtr = props)
            {
                vkEnumerateInstanceLayerProperties(&propCount, propPtr);

                for (int i = 0; i < propCount; i++)
                {
                    ret[i] = Util.GetString(propPtr[i].layerName);
                }
            }

            return ret;
        }

        public static string[] EnumerateInstanceExtensions()
        {
            uint propCount = 0;
            VkResult result = vkEnumerateInstanceExtensionProperties((byte*)null, &propCount, null);
            if (result != VkResult.Success)
            {
                return Array.Empty<string>();
            }

            if (propCount == 0)
            {
                return Array.Empty<string>();
            }

            VkExtensionProperties[] props = new VkExtensionProperties[propCount];
            string[] ret = new string[propCount];

            fixed (VkExtensionProperties* propPtr = props)
            {
                vkEnumerateInstanceExtensionProperties((byte*)null, &propCount, propPtr);

                for (int i = 0; i < propCount; i++)
                {
                    ret[i] = Util.GetString(propPtr[i].extensionName);
                }
            }

            return ret;
        }

        public static IntPtr GetInstanceProcAddr(VkInstance instance, string name)
        {
            Span<byte> byteBuffer = stackalloc byte[1024];

            Util.GetNullTerminatedUtf8(name, ref byteBuffer);
            fixed (byte* utf8Ptr = byteBuffer)
            {
                return (IntPtr)vkGetInstanceProcAddr(instance, (byte*)utf8Ptr);
            }
        }

        public static void TransitionImageLayout(
            VkCommandBuffer cb,
            VkImage image,
            uint baseMipLevel,
            uint levelCount,
            uint baseArrayLayer,
            uint layerCount,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldLayout,
            VkImageLayout newLayout)
        {
            Debug.Assert(oldLayout != newLayout);
            VkImageMemoryBarrier barrier = new()
            {
                sType = VkStructureType.ImageMemoryBarrier,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
                dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
                image = image,
                subresourceRange = new VkImageSubresourceRange()
                {
                    aspectMask = aspectMask,
                    baseMipLevel = baseMipLevel,
                    levelCount = levelCount,
                    baseArrayLayer = baseArrayLayer,
                    layerCount = layerCount
                }
            };

            VkPipelineStageFlags srcStageFlags = VK_PIPELINE_STAGE_NONE_KHR;
            VkPipelineStageFlags dstStageFlags = VK_PIPELINE_STAGE_NONE_KHR;

            switch (oldLayout)
            {
                case VK_IMAGE_LAYOUT_UNDEFINED:
                case VK_IMAGE_LAYOUT_PREINITIALIZED:
                    barrier.srcAccessMask = VK_ACCESS_NONE_KHR;
                    srcStageFlags = VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT;
                    break;

                case VK_IMAGE_LAYOUT_GENERAL:
                    if (newLayout == VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL ||
                        newLayout == VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
                    {
                        barrier.srcAccessMask = VK_ACCESS_SHADER_WRITE_BIT;
                        srcStageFlags = VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT;
                        break;
                    }
                    else if (newLayout == VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
                    }
                    else if (newLayout == VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
                    }
                    else if (newLayout == VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
                    }
                    goto default;

                case VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL:
                    barrier.srcAccessMask = VK_ACCESS_TRANSFER_READ_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_TRANSFER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
                    barrier.srcAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_TRANSFER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:
                    barrier.srcAccessMask = VK_ACCESS_SHADER_READ_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
                    barrier.srcAccessMask = VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
                    break;

                case VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL:
                    barrier.srcAccessMask = VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
                    break;

                case VK_IMAGE_LAYOUT_PRESENT_SRC_KHR:
                    barrier.srcAccessMask = VK_ACCESS_MEMORY_READ_BIT;
                    srcStageFlags = VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
                    break;

                default:
                    Debug.Fail($"Invalid old image layout transition ({oldLayout} -> {newLayout})");
                    break;
            }

            switch (newLayout)
            {
                case VK_IMAGE_LAYOUT_GENERAL:
                    if (oldLayout == VK_IMAGE_LAYOUT_PREINITIALIZED ||
                        oldLayout == VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
                    {
                        barrier.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
                        dstStageFlags = VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT;
                        break;
                    }
                    else if (oldLayout == VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
                    }
                    else if (oldLayout == VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
                    }
                    else if (oldLayout == VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL;
                    }
                    else if (oldLayout == VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL)
                    {
                        goto case VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
                    }
                    goto default;

                case VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL:
                    barrier.dstAccessMask = VK_ACCESS_TRANSFER_READ_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_TRANSFER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
                    barrier.dstAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_TRANSFER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:
                    barrier.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;
                    break;

                case VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
                    barrier.dstAccessMask = VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
                    break;

                case VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL:
                    barrier.dstAccessMask = VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;
                    break;

                case VK_IMAGE_LAYOUT_PRESENT_SRC_KHR:
                    barrier.dstAccessMask = VK_ACCESS_MEMORY_READ_BIT;
                    dstStageFlags = VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
                    break;

                default:
                    Debug.Fail($"Invalid new image layout transition ({oldLayout} -> {newLayout})");
                    break;
            }

            vkCmdPipelineBarrier(
                cb,
                srcStageFlags,
                dstStageFlags,
                0,
                0, null,
                0, null,
                1, &barrier);
        }
    }

    internal unsafe static class VkPhysicalDeviceMemoryPropertiesEx
    {
        public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return memoryProperties.memoryTypes[(int)index];
        }
    }
}
