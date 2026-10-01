using Newtonsoft.Json;

namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Serializable color representation for JSON persistence.
    /// Stores RGBA channels as bytes for efficient serialization.
    /// </summary>
    public struct ColorDTO : IEquatable<ColorDTO>
    {
        [JsonProperty("r")]
        public byte R { get; set; }

        [JsonProperty("g")]
        public byte G { get; set; }

        [JsonProperty("b")]
        public byte B { get; set; }

        [JsonProperty("a")]
        public byte A { get; set; }

        /// <summary>Initializes a new instance of the <see cref="ColorDTO"/> struct.</summary>
        /// <param name="r">Red channel.</param>
        /// <param name="g">Green channel.</param>
        /// <param name="b">Blue channel.</param>
        /// <param name="a">Alpha channel.</param>
        public ColorDTO(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorDTO"/> struct from an XNA color.
        /// </summary>
        /// <param name="color">XNA color to copy.</param>
        public ColorDTO(Microsoft.Xna.Framework.Color color)
            : this(color.R, color.G, color.B, color.A)
        {
        }

        public bool Equals(ColorDTO other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object? obj)
        {
            return obj is ColorDTO other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        public static bool operator ==(ColorDTO left, ColorDTO right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColorDTO left, ColorDTO right)
        {
            return !left.Equals(right);
        }
    }
}
