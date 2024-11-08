using System;

namespace Veldrid
{
    /// <summary>
    /// Describes an individual output attachment and its format.
    /// </summary>
    public struct OutputAttachmentDescription : IEquatable<OutputAttachmentDescription>
    {
        /// <summary>
        /// The format of the <see cref="Texture"/> attachment.
        /// </summary>
        public PixelFormat Format;

        /// <summary>
        /// Constructs a new <see cref="OutputAttachmentDescription"/>.
        /// </summary>
        /// <param name="format">THe format of the <see cref="Texture"/> attachment.</param>
        public OutputAttachmentDescription(PixelFormat format)
        {
            Format = format;
        }

        /// <summary>
        /// Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements are equal; false otherswise.</returns>
        public bool Equals(OutputAttachmentDescription other)
        {
            return Format == other.Format;
        }

        /// <summary>
        /// Element-wise equality.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if all elements are equal; false otherswise.</returns>
        public override readonly bool Equals(object? obj) => obj is OutputAttachmentDescription other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return (int)Format;
        }

        public static bool operator ==(OutputAttachmentDescription left, OutputAttachmentDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OutputAttachmentDescription left, OutputAttachmentDescription right)
        {
            return !(left == right);
        }
    }
}
