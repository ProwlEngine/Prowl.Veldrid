using System;

namespace Veldrid
{
    /// <summary>
    /// Describes a set of output attachments and their formats.
    /// </summary>
    public struct OutputDescription : IEquatable<OutputDescription>
    {
        /// <summary>
        /// A description of the depth attachment, or null if none exists.
        /// </summary>
        public OutputAttachmentDescription? DepthAttachment;

        /// <summary>
        /// An array of attachment descriptions, one for each color attachment. May be null or empty.
        /// </summary>
        public OutputAttachmentDescription[]? ColorAttachments;

        /// <summary>
        /// The number of samples in each target attachment.
        /// </summary>
        public TextureSampleCount SampleCount;

        /// <summary>
        /// Constructs a new <see cref="OutputDescription"/>.
        /// </summary>
        /// <param name="depthAttachment">A description of the depth attachment.</param>
        /// <param name="colorAttachments">An array of descriptions of each color attachment.</param>
        public OutputDescription(OutputAttachmentDescription? depthAttachment, params OutputAttachmentDescription[]? colorAttachments)
        {
            DepthAttachment = depthAttachment;
            ColorAttachments = colorAttachments;
            SampleCount = TextureSampleCount.Count1;
        }

        /// <summary>
        /// Constructs a new <see cref="OutputDescription"/>.
        /// </summary>
        /// <param name="depthAttachment">A description of the depth attachment.</param>
        /// <param name="colorAttachments">An array of descriptions of each color attachment.</param>
        /// <param name="sampleCount">The number of samples in each target attachment.</param>
        public OutputDescription(
            OutputAttachmentDescription? depthAttachment,
            OutputAttachmentDescription[]? colorAttachments,
            TextureSampleCount sampleCount)
        {
            DepthAttachment = depthAttachment;
            ColorAttachments = colorAttachments;
            SampleCount = sampleCount;
        }

        internal static OutputDescription CreateFromFramebuffer(Framebuffer fb)
        {
            TextureSampleCount sampleCount = 0;

            FramebufferAttachment? fbDepthAttachment = fb.DepthTarget;
            OutputAttachmentDescription? depthAttachment = null;
            if (fbDepthAttachment != null)
            {
                depthAttachment = new OutputAttachmentDescription(fbDepthAttachment.GetValueOrDefault().Target.Format);
                sampleCount = fbDepthAttachment.GetValueOrDefault().Target.SampleCount;
            }

            ReadOnlySpan<FramebufferAttachment> fbColorAttachments = fb.ColorTargets.ToArray();
            OutputAttachmentDescription[] colorAttachments = new OutputAttachmentDescription[fbColorAttachments.Length];
            for (int i = 0; i < colorAttachments.Length; i++)
            {
                colorAttachments[i] = new OutputAttachmentDescription(fbColorAttachments[i].Target.Format);
                sampleCount = fbColorAttachments[i].Target.SampleCount;
            }

            return new OutputDescription(depthAttachment, colorAttachments, sampleCount);
        }

        /// <summary>
        /// Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
        public bool Equals(OutputDescription other)
        {
            // Handle case where one is null and other isn't
            if (DepthAttachment.HasValue != other.DepthAttachment.HasValue)
                return false;

            // Both null or both have value
            bool depthEqual = !DepthAttachment.HasValue ||
                             DepthAttachment.Value.Equals(other.DepthAttachment.Value);

            // Handle null ColorAttachments
            if (ColorAttachments == null && other.ColorAttachments == null)
                return depthEqual && SampleCount == other.SampleCount;

            if (ColorAttachments == null || other.ColorAttachments == null)
                return false;

            return depthEqual
                && Util.ArrayEqualsEquatable(ColorAttachments, other.ColorAttachments)
                && SampleCount == other.SampleCount;
        }

        /// <summary>
        /// Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements and all array elements are equal; false otherswise.</returns>
        public override bool Equals(object? obj)
        {
            return obj is OutputDescription other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            if (DepthAttachment.HasValue)
                hash.Add(DepthAttachment.Value);

            if (ColorAttachments != null)
                hash.Add(HashHelper.Array(ColorAttachments));

            hash.Add(SampleCount);

            return hash.ToHashCode();
        }

        public static bool operator ==(OutputDescription left, OutputDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OutputDescription left, OutputDescription right)
        {
            return !(left == right);
        }
    }
}
