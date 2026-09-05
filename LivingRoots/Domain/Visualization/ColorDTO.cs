using System.Text.Json.Serialization;

namespace LivingRoots.Domain.Visualization
{
    public struct ColorDTO
    {
        [JsonPropertyName("r")]
        public byte R { get; set; }

        [JsonPropertyName("g")]
        public byte G { get; set; }

        [JsonPropertyName("b")]
        public byte B { get; set; }

        [JsonPropertyName("a")]
        public byte A { get; set; }

        public ColorDTO(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public override bool Equals(object? obj) =>
            obj is ColorDTO other && R == other.R && G == other.G && B == other.B && A == other.A;

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(ColorDTO left, ColorDTO right) => left.Equals(right);
        public static bool operator !=(ColorDTO left, ColorDTO right) => !left.Equals(right);
    }
}
