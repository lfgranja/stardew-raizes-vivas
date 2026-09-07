using LivingRoots.Domain.Visualization;
using Newtonsoft.Json;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ColorDTOTests
    {
        [Fact]
        public void Properties_RGBA_CanBeSetAndGet()
        {
            // Arrange
            var color = new ColorDTO { R = 255, G = 128, B = 64, A = 200 };

            // Assert
            Assert.Equal(255, color.R);
            Assert.Equal(128, color.G);
            Assert.Equal(64, color.B);
            Assert.Equal(200, color.A);
        }

        [Fact]
        public void DefaultValue_IsZero()
        {
            // Arrange
            var color = default(ColorDTO);

            // Assert
            Assert.Equal(0, color.R);
            Assert.Equal(0, color.G);
            Assert.Equal(0, color.B);
            Assert.Equal(0, color.A);
        }

        [Fact]
        public void JsonRoundTrip_PreservesValues()
        {
            // Arrange
            var original = new ColorDTO { R = 255, G = 255, B = 0, A = 255 };

            // Act
            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ColorDTO>(json);

            // Assert
            Assert.Equal(original.R, deserialized.R);
            Assert.Equal(original.G, deserialized.G);
            Assert.Equal(original.B, deserialized.B);
            Assert.Equal(original.A, deserialized.A);
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            // Arrange
            var a = new ColorDTO { R = 100, G = 150, B = 200, A = 255 };
            var b = new ColorDTO { R = 100, G = 150, B = 200, A = 255 };

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            // Arrange
            var a = new ColorDTO { R = 100, G = 150, B = 200, A = 255 };
            var b = new ColorDTO { R = 101, G = 150, B = 200, A = 255 };

            // Assert
            Assert.NotEqual(a, b);
        }
    }
}
