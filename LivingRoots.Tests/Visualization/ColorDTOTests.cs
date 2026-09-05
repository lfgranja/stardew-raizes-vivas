using System.Text.Json;
using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ColorDTOTests
    {
        // ──────────────────────────────────────────────
        // Property Verification
        // ──────────────────────────────────────────────

        [Fact]
        public void Constructor_ShouldSetRGBAProperties()
        {
            var color = new ColorDTO(10, 20, 30, 40);

            Assert.Equal(10, color.R);
            Assert.Equal(20, color.G);
            Assert.Equal(30, color.B);
            Assert.Equal(40, color.A);
        }

        [Fact]
        public void Properties_ShouldBeByteType()
        {
            var color = new ColorDTO(255, 128, 0, 64);

            Assert.IsType<byte>(color.R);
            Assert.IsType<byte>(color.G);
            Assert.IsType<byte>(color.B);
            Assert.IsType<byte>(color.A);
        }

        // ──────────────────────────────────────────────
        // JSON Serialization Round-Trip
        // ──────────────────────────────────────────────

        [Fact]
        public void Serialize_ShouldProduceJsonWithRGBAKeys()
        {
            var color = new ColorDTO(100, 150, 200, 250);
            var json = JsonSerializer.Serialize(color);

            Assert.Contains("\"r\":100", json);
            Assert.Contains("\"g\":150", json);
            Assert.Contains("\"b\":200", json);
            Assert.Contains("\"a\":250", json);
        }

        [Fact]
        public void Deserialize_ShouldRestoreRGBAValues()
        {
            var json = "{\"r\":100,\"g\":150,\"b\":200,\"a\":250}";
            var color = JsonSerializer.Deserialize<ColorDTO>(json)!;

            Assert.Equal(100, color.R);
            Assert.Equal(150, color.G);
            Assert.Equal(200, color.B);
            Assert.Equal(250, color.A);
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip_ShouldPreserveValues()
        {
            var original = new ColorDTO(1, 2, 3, 4);
            var json = JsonSerializer.Serialize(original);
            var restored = JsonSerializer.Deserialize<ColorDTO>(json)!;

            Assert.Equal(original, restored);
        }

        // ──────────────────────────────────────────────
        // Default Value
        // ──────────────────────────────────────────────

        [Fact]
        public void DefaultValue_ShouldBeZeroZeroZeroZero()
        {
            var color = default(ColorDTO);

            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
            Assert.Equal(0, color.A);
        }

        [Fact]
        public void NewColorDTO_ShouldBeZeroZeroZeroZero()
        {
            var color = new ColorDTO();

            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
            Assert.Equal(0, color.A);
        }

        // ──────────────────────────────────────────────
        // Equality Semantics
        // ──────────────────────────────────────────────

        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(10, 20, 30, 40);

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(99, 20, 30, 40);

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void EqualityOperator_WithSameValues_ShouldReturnTrue()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(10, 20, 30, 40);

            Assert.True(a == b);
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ShouldReturnTrue()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(99, 20, 30, 40);

            Assert.True(a != b);
        }

        [Fact]
        public void InequalityOperator_WithSameValues_ShouldReturnFalse()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(10, 20, 30, 40);

            Assert.False(a != b);
        }

        [Fact]
        public void GetHashCode_SameValues_ShouldProduceSameHashCode()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(10, 20, 30, 40);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentValues_ShouldProduceDifferentHashCode()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(99, 20, 30, 40);

            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            var color = new ColorDTO(10, 20, 30, 40);

            Assert.False(color.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            var color = new ColorDTO(10, 20, 30, 40);

            Assert.False(color.Equals("not a color"));
        }
    }
}
