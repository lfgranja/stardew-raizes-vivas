using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Tests for <see cref="SeasonalDecayMultiplier"/> — verifies seasonal decay multipliers per FR-5.1 through FR-5.5.
    /// </summary>
    public class SeasonalDecayMultiplierTests
    {
        private readonly SeasonalDecayMultiplier _sut;

        public SeasonalDecayMultiplierTests()
        {
            _sut = new SeasonalDecayMultiplier();
        }

        [Fact]
        public void GetMultiplier_Spring_ReturnsHalf()
        {
            // Act
            float result = _sut.GetMultiplier("spring");

            // Assert
            Assert.Equal(0.5f, result, 5);
        }

        [Fact]
        public void GetMultiplier_Summer_ReturnsOneAndHalf()
        {
            // Act
            float result = _sut.GetMultiplier("summer");

            // Assert
            Assert.Equal(1.5f, result, 5);
        }

        [Fact]
        public void GetMultiplier_Fall_ReturnsHalf()
        {
            // Act
            float result = _sut.GetMultiplier("fall");

            // Assert
            Assert.Equal(0.5f, result, 5);
        }

        [Fact]
        public void GetMultiplier_Winter_ReturnsZero()
        {
            // Act
            float result = _sut.GetMultiplier("winter");

            // Assert
            Assert.Equal(0.0f, result, 5);
        }

        [Fact]
        public void GetMultiplier_InvalidSeason_ReturnsDefault()
        {
            // Act
            float result = _sut.GetMultiplier("invalid");

            // Assert
            Assert.Equal(0.5f, result, 5);
        }
    }
}
