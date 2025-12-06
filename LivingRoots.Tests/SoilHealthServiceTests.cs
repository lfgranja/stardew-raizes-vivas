using Xunit;
using Moq;
using LivingRoots.Domain;
using LivingRoots.Services;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace LivingRoots.Tests
{
    public class SoilHealthServiceTests
    {
        private readonly Mock<IModDataService> _mockDataService;
        private readonly Mock<IMonitor> _mockMonitor;

        public SoilHealthServiceTests()
        {
            _mockDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
        }

        [Fact]
        public void SetHealth_ValuesAreClampedTo0And100()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(10, 10);

            // Act
            service.SetSoilHealth(location, tile, 150.0f); // Tenta setar 150
            var resultMax = service.GetSoilHealth(location, tile);

            service.SetSoilHealth(location, tile, -50.0f); // Tenta setar -50
            var resultMin = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(100.0f, resultMax);
            Assert.Equal(0.0f, resultMin);
        }

        [Fact]
        public void GetSoilHealth_ReturnsDefaultZeroWhenNoDataExists()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(5, 5);

            // Act
            var result = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(0.0f, result);
        }

        [Fact]
        public void SetAndRetrieveHealth_ReturnsCorrectValue()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(15, 20);
            float expectedHealth = 75.5f;

            // Act
            service.SetSoilHealth(location, tile, expectedHealth);
            var result = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(expectedHealth, result);
        }

        [Fact]
        public void UpdateHealth_ChangesValueByDelta()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(12, 18);
            float initialHealth = 50.0f;
            float delta = 15.0f;
            float expectedHealth = initialHealth + delta;

            // Act
            service.SetSoilHealth(location, tile, initialHealth);
            service.UpdateHealth(location, tile, delta);
            var result = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(expectedHealth, result);
        }

        [Fact]
        public void UpdateHealth_ClampsToMaxWhenExceeds100()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(8, 12);
            float initialHealth = 90.0f;
            float delta = 20.0f;
            float expectedHealth = 100.0f; // Should be clamped

            // Act
            service.SetSoilHealth(location, tile, initialHealth);
            service.UpdateHealth(location, tile, delta);
            var result = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(expectedHealth, result);
        }

        [Fact]
        public void UpdateHealth_ClampsToMinWhenBelow0()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string location = "Farm";
            var tile = new Vector2(3, 7);
            float initialHealth = 10.0f;
            float delta = -20.0f;
            float expectedHealth = 0.0f; // Should be clamped

            // Act
            service.SetSoilHealth(location, tile, initialHealth);
            service.UpdateHealth(location, tile, delta);
            var result = service.GetSoilHealth(location, tile);

            // Assert
            Assert.Equal(expectedHealth, result);
        }

        [Fact]
        public void LoadData_WithExistingData_LoadsCorrectly()
        {
            // Arrange
            var expectedData = new SoilHealthState
            {
                LocationHealthData = new Dictionary<string, Dictionary<string, float>>
                {
                    { "Farm", new Dictionary<string, float> { { "10,10", 85.5f } } }
                }
            };

            _mockDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns(expectedData);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert
            var result = service.GetSoilHealth("Farm", new Vector2(10, 10));
            Assert.Equal(85.5f, result);
        }

        [Fact]
        public void LoadData_WithNoExistingData_InitializesEmptyState()
        {
            // Arrange
            _mockDataService
                .Setup(ds => ds.LoadData<SoilHealthState>(It.IsAny<string>()))
                .Returns((SoilHealthState)null);

            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);

            // Act
            service.LoadData("test_save");

            // Assert
            var result = service.GetSoilHealth("Farm", new Vector2(5, 5));
            Assert.Equal(0.0f, result);
        }

        [Fact]
        public void SaveData_CallsModDataServiceWithCorrectKey()
        {
            // Arrange
            var service = new SoilHealthService(_mockDataService.Object, _mockMonitor.Object);
            string saveId = "test_save_123";

            // Act
            service.SaveData(saveId);

            // Assert
            _mockDataService.Verify(ds => ds.SaveData(It.IsAny<SoilHealthState>(), $"soil_health_data_{saveId}"), Times.Once);
        }
    }
}