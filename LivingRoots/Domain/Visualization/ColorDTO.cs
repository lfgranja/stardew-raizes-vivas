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
