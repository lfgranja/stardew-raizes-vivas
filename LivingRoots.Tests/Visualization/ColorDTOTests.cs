using LivingRoots.Domain.Visualization;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ColorDTOTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var dto = new ColorDTO(100, 150, 200, 255);
            Assert.Equal(100, dto.R);
            Assert.Equal(150, dto.G);
            Assert.Equal(200, dto.B);
            Assert.Equal(255, dto.A);
        }

        [Fact]
        public void DefaultValue_IsZero()
        {
            var dto = new ColorDTO();
            Assert.Equal(0, dto.R);
            Assert.Equal(0, dto.G);
            Assert.Equal(0, dto.B);
            Assert.Equal(0, dto.A);
        }

        [Fact]
        public void Equality_SameValues_ReturnsTrue()
        {
            var a = new ColorDTO(10, 20, 30, 40);
            var b = new ColorDTO(10, 20, 30, 40);
            Assert.True(a.Equals(b));
        }
    }
}
