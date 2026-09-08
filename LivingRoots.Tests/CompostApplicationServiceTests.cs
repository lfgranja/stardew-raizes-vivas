using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Tests
{
    public class CompostApplicationServiceTests
    {
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly PlayerProviderStub _playerProviderStub;
        private readonly CompostApplicationService _service;

        public CompostApplicationServiceTests()
        {
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockMonitor = new Mock<IMonitor>();
            _playerProviderStub = new PlayerProviderStub();
            _service = new CompostApplicationService(
                _mockSoilHealthService.Object,
                _playerProviderStub,
                _mockMonitor.Object);
        }

        private static Item CreateCompostItem()
        {
            return new StardewValley.Object(ModConstants.CompostItemId, 1);
        }

        [Fact]
        public void TryApplyCompost_ValidTile_IncreasesHealth()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation();
            var tile = new Vector2(5, 5);
            GameLocationFixture.AddHoeDirtTile(location, tile);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("Farm", tile)).Returns(50f);
            _playerProviderStub.CurrentItem = CreateCompostItem();

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.True(result);
            _mockSoilHealthService.Verify(s => s.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
        }

        [Fact]
        public void TryApplyCompost_AtMaxHealth_ReturnsFalse()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation();
            var tile = new Vector2(5, 5);
            GameLocationFixture.AddHoeDirtTile(location, tile);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("Farm", tile)).Returns(ModConstants.MaxSoilHealth);

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.False(result);
            _mockSoilHealthService.Verify(s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void TryApplyCompost_InvalidLocation_ReturnsFalse()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation("Maps\\Town", "Town");
            var tile = new Vector2(5, 5);

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.False(result);
            _mockSoilHealthService.Verify(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()), Times.Never);
            _mockSoilHealthService.Verify(s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void TryApplyCompost_NoCompostHeld_ReturnsFalse()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation();
            var tile = new Vector2(5, 5);
            GameLocationFixture.AddHoeDirtTile(location, tile);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("Farm", tile)).Returns(50f);
            _playerProviderStub.CurrentItem = null;

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.False(result);
            _mockSoilHealthService.Verify(s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void TryApplyCompost_NonHoeDirtTile_ReturnsFalse()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation();
            var tile = new Vector2(5, 5);
            // No hoe dirt added - tile has no terrain feature

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.False(result);
            _mockSoilHealthService.Verify(s => s.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()), Times.Never);
            _mockSoilHealthService.Verify(s => s.UpdateHealth(It.IsAny<string>(), It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
        }

        [Fact]
        public void TryApplyCompost_HealthCappedAtMax()
        {
            // Arrange
            var location = GameLocationFixture.CreateLocation();
            var tile = new Vector2(5, 5);
            GameLocationFixture.AddHoeDirtTile(location, tile);
            _mockSoilHealthService.Setup(s => s.GetSoilHealth("Farm", tile)).Returns(90f);
            _playerProviderStub.CurrentItem = CreateCompostItem();

            // Act
            var result = _service.TryApplyCompost(location, tile);

            // Assert
            Assert.True(result);
            // UpdateHealth is called with RestorationAmount (15); the actual capping to MaxSoilHealth
            // is performed by SoilHealthService.UpdateHealth, not by CompostApplicationService.
            _mockSoilHealthService.Verify(s => s.UpdateHealth("Farm", tile, ModConstants.RestorationAmount), Times.Once);
        }
    }
}
