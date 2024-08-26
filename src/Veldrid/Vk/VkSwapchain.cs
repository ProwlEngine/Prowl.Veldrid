using System;
using System.Linq;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static Veldrid.Vulkan.VulkanUtil;
using VulkanFence = Vortice.Vulkan.VkFence;

namespace Veldrid.Vulkan
{
    internal sealed unsafe class VkSwapchain : Swapchain, IResourceRefCountTarget
    {
        private readonly VkGraphicsDevice _gd;
        private readonly VkSurfaceKHR _surface;
        private VkSwapchainKHR _deviceSwapchain;
        private readonly VkSwapchainFramebuffer _framebuffer;
        private VulkanFence _imageAvailableFence;
        private readonly uint _presentQueueIndex;
        private readonly VkQueue _presentQueue;
        private bool _syncToVBlank;
        private readonly SwapchainSource _swapchainSource;
        private readonly bool _colorSrgb;
        private bool? _newSyncToVBlank;
        private uint _currentImageIndex;
        private string? _name;

        public override string? Name { get => _name; set { _name = value; _gd.SetResourceName(this, value); } }

        public override Framebuffer Framebuffer => _framebuffer;

        public override bool SyncToVerticalBlank
        {
            get => _newSyncToVBlank ?? _syncToVBlank;
            set
            {
                if (_syncToVBlank != value)
                {
                    _newSyncToVBlank = value;
                }
            }
        }

        public override bool IsDisposed => RefCount.IsDisposed;

        public VkSwapchainKHR DeviceSwapchain => _deviceSwapchain;
        public uint ImageIndex => _currentImageIndex;
        public VulkanFence ImageAvailableFence => _imageAvailableFence;
        public VkSurfaceKHR Surface => _surface;
        public VkQueue PresentQueue => _presentQueue;
        public uint PresentQueueIndex => _presentQueueIndex;
        public ResourceRefCount RefCount { get; }
        public object PresentLock { get; }

        public VkSwapchain(VkGraphicsDevice gd, in SwapchainDescription description) : this(gd, description, default)
        {
        }

        public VkSwapchain(VkGraphicsDevice gd, in SwapchainDescription description, VkSurfaceKHR existingSurface)
        {
            _gd = gd;
            _syncToVBlank = description.SyncToVerticalBlank;
            _swapchainSource = description.Source;
            _colorSrgb = description.ColorSrgb;

            if (existingSurface == VkSurfaceKHR.Null)
            {
                _surface = VkSurfaceUtil.CreateSurface(gd.Instance, _swapchainSource);
            }
            else
            {
                _surface = existingSurface;
            }

            if (!GetPresentQueueIndex(out _presentQueueIndex))
            {
                throw new VeldridException($"The system does not support presenting the given Vulkan surface.");
            }
            VkQueue presentQueue;
            vkGetDeviceQueue(_gd.Device, _presentQueueIndex, 0, &presentQueue);
            _presentQueue = presentQueue;

            RefCount = new ResourceRefCount(this);
            PresentLock = new object();

            _framebuffer = new VkSwapchainFramebuffer(gd, this, _surface, description);

            CreateSwapchain(description.Width, description.Height);

            VkFenceCreateInfo fenceCI = new()
            {
                sType = VkStructureType.FenceCreateInfo
            };

            VulkanFence imageAvailableFence;
            vkCreateFence(_gd.Device, &fenceCI, null, &imageAvailableFence);

            AcquireNextImage(_gd.Device, default, imageAvailableFence);
            vkWaitForFences(_gd.Device, 1, &imageAvailableFence, (VkBool32)true, ulong.MaxValue);
            vkResetFences(_gd.Device, 1, &imageAvailableFence);

            _imageAvailableFence = imageAvailableFence;
        }

        public override void Resize(uint width, uint height)
        {
            RecreateAndReacquire(width, height);
        }

        public bool AcquireNextImage(VkDevice device, VkSemaphore semaphore, VulkanFence fence)
        {
            if (_newSyncToVBlank != null)
            {
                _syncToVBlank = _newSyncToVBlank.Value;
                _newSyncToVBlank = null;
                RecreateAndReacquire(_framebuffer.Width, _framebuffer.Height);
                return false;
            }

            uint imageIndex = _currentImageIndex;
            VkResult result = vkAcquireNextImageKHR(
                device,
                _deviceSwapchain,
                ulong.MaxValue,
                semaphore,
                fence,
                &imageIndex);
            _framebuffer.SetImageIndex(imageIndex);
            _currentImageIndex = imageIndex;

            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
            {
                CreateSwapchain(_framebuffer.Width, _framebuffer.Height);
                return false;
            }
            else if (result != VkResult.Success)
            {
                throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");
            }

            return true;
        }

        private void RecreateAndReacquire(uint width, uint height)
        {
            if (CreateSwapchain(width, height))
            {
                VulkanFence imageAvailableFence = _imageAvailableFence;
                if (AcquireNextImage(_gd.Device, default, imageAvailableFence))
                {
                    vkWaitForFences(_gd.Device, 1, &imageAvailableFence, (VkBool32)true, ulong.MaxValue);
                    vkResetFences(_gd.Device, 1, &imageAvailableFence);
                }
            }
        }

        private bool CreateSwapchain(uint width, uint height)
        {
            // Obtain the surface capabilities first -- this will indicate whether the surface has been lost.
            VkSurfaceCapabilitiesKHR surfaceCapabilities;
            VkResult result = vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_gd.PhysicalDevice, _surface, &surfaceCapabilities);
            if (result == VkResult.ErrorSurfaceLostKHR)
            {
                throw new VeldridException($"The Swapchain's underlying surface has been lost.");
            }

            if (surfaceCapabilities.minImageExtent.width == 0 && surfaceCapabilities.minImageExtent.height == 0
                && surfaceCapabilities.maxImageExtent.width == 0 && surfaceCapabilities.maxImageExtent.height == 0)
            {
                return false;
            }

            if (_deviceSwapchain != VkSwapchainKHR.Null)
            {
                _gd.WaitForIdle();
            }

            _currentImageIndex = 0;
            uint surfaceFormatCount = 0;
            result = vkGetPhysicalDeviceSurfaceFormatsKHR(_gd.PhysicalDevice, _surface, &surfaceFormatCount, null);
            CheckResult(result);
            VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
            fixed (VkSurfaceFormatKHR* formatsPtr = formats)
            {
                result = vkGetPhysicalDeviceSurfaceFormatsKHR(_gd.PhysicalDevice, _surface, &surfaceFormatCount, formatsPtr);
                CheckResult(result);
            }

            VkFormat desiredFormat = _colorSrgb
                ? VK_FORMAT_B8G8R8A8_SRGB
                : VK_FORMAT_B8G8R8A8_UNORM;

            VkSurfaceFormatKHR surfaceFormat = new();
            if (formats.Length == 1 && formats[0].format == VK_FORMAT_UNDEFINED)
            {
                surfaceFormat.format = desiredFormat;
                surfaceFormat.colorSpace = VkColorSpaceKHR.SrgbNonLinear;
            }
            else
            {
                foreach (VkSurfaceFormatKHR format in formats)
                {
                    if (format.colorSpace == VkColorSpaceKHR.SrgbNonLinear && format.format == desiredFormat)
                    {
                        surfaceFormat = format;
                        break;
                    }
                }
                if (surfaceFormat.format == VK_FORMAT_UNDEFINED)
                {
                    if (_colorSrgb && surfaceFormat.format != VK_FORMAT_R8G8B8A8_SRGB)
                    {
                        throw new VeldridException($"Unable to create an sRGB Swapchain for this surface.");
                    }

                    surfaceFormat = formats[0];
                }
            }

            uint presentModeCount = 0;
            result = vkGetPhysicalDeviceSurfacePresentModesKHR(_gd.PhysicalDevice, _surface, &presentModeCount, null);
            CheckResult(result);
            VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
            fixed (VkPresentModeKHR* presentModesPtr = presentModes)
            {
                result = vkGetPhysicalDeviceSurfacePresentModesKHR(_gd.PhysicalDevice, _surface, &presentModeCount, presentModesPtr);
                CheckResult(result);
            }

            VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;

            if (_syncToVBlank)
            {
                if (presentModes.Contains(VkPresentModeKHR.FifoRelaxed))
                {
                    presentMode = VkPresentModeKHR.FifoRelaxed;
                }
            }
            else
            {
                if (presentModes.Contains(VkPresentModeKHR.Mailbox))
                {
                    presentMode = VkPresentModeKHR.Mailbox;
                }
                else if (presentModes.Contains(VkPresentModeKHR.Immediate))
                {
                    presentMode = VkPresentModeKHR.Immediate;
                }
            }

            uint maxImageCount = surfaceCapabilities.maxImageCount == 0 ? uint.MaxValue : surfaceCapabilities.maxImageCount;
            uint imageCount = Math.Min(maxImageCount, surfaceCapabilities.minImageCount + 1);

            uint clampedWidth = Util.Clamp(width, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width);
            uint clampedHeight = Util.Clamp(height, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height);
            VkSwapchainCreateInfoKHR swapchainCI = new()
            {
                sType = VkStructureType.SwapchainCreateInfoKHR,
                surface = _surface,
                presentMode = presentMode,
                imageFormat = surfaceFormat.format,
                imageColorSpace = surfaceFormat.colorSpace,
                imageExtent = new VkExtent2D() { width = clampedWidth, height = clampedHeight },
                minImageCount = imageCount,
                imageArrayLayers = 1,
                imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst
            };

            uint* queueFamilyIndices = stackalloc uint[] { _gd.GraphicsQueueIndex, _gd.PresentQueueIndex };

            if (_gd.GraphicsQueueIndex != _gd.PresentQueueIndex)
            {
                swapchainCI.imageSharingMode = VkSharingMode.Concurrent;
                swapchainCI.queueFamilyIndexCount = 2;
                swapchainCI.pQueueFamilyIndices = queueFamilyIndices;
            }
            else
            {
                swapchainCI.imageSharingMode = VkSharingMode.Exclusive;
                swapchainCI.queueFamilyIndexCount = 0;
            }

            swapchainCI.preTransform = VkSurfaceTransformFlagsKHR.Identity;
            swapchainCI.compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque;
            swapchainCI.clipped = (VkBool32)true;

            VkSwapchainKHR oldSwapchain = _deviceSwapchain;
            swapchainCI.oldSwapchain = oldSwapchain;

            VkSwapchainKHR deviceSwapchain;
            result = vkCreateSwapchainKHR(_gd.Device, &swapchainCI, null, &deviceSwapchain);
            CheckResult(result);
            _deviceSwapchain = deviceSwapchain;

            if (oldSwapchain != VkSwapchainKHR.Null)
            {
                vkDestroySwapchainKHR(_gd.Device, oldSwapchain, null);
            }

            _framebuffer.SetNewSwapchain(_deviceSwapchain, width, height, surfaceFormat, swapchainCI.imageExtent);
            return true;
        }

        private bool GetPresentQueueIndex(out uint queueFamilyIndex)
        {
            uint graphicsQueueIndex = _gd.GraphicsQueueIndex;
            uint presentQueueIndex = _gd.PresentQueueIndex;

            if (QueueSupportsPresent(graphicsQueueIndex, _surface))
            {
                queueFamilyIndex = graphicsQueueIndex;
                return true;
            }
            else if (graphicsQueueIndex != presentQueueIndex && QueueSupportsPresent(presentQueueIndex, _surface))
            {
                queueFamilyIndex = presentQueueIndex;
                return true;
            }

            queueFamilyIndex = 0;
            return false;
        }

        private bool QueueSupportsPresent(uint queueFamilyIndex, VkSurfaceKHR surface)
        {
            VkBool32 supported;
            VkResult result = vkGetPhysicalDeviceSurfaceSupportKHR(
                _gd.PhysicalDevice,
                queueFamilyIndex,
                surface,
                &supported);
            CheckResult(result);
            return supported;
        }

        public override void Dispose()
        {
            _framebuffer.Dispose();
            RefCount.DecrementDispose();
        }

        void IResourceRefCountTarget.RefZeroed()
        {
            vkDestroyFence(_gd.Device, _imageAvailableFence, null);
            vkDestroySwapchainKHR(_gd.Device, _deviceSwapchain, null);
            vkDestroySurfaceKHR(_gd.Instance, _surface, null);
        }
    }
}
