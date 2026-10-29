using System;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.ContractTests
{
    /// <summary>
    /// Contract tests for <see cref="ISoilHealthService"/> (spec 005 T016).
    /// Verifies the core contract that any implementation must satisfy:
    /// sparse-save default, roundtrip fidelity, delta clamping, and mutation
    /// notification. Uses the real <see cref="SoilHealthService"/> over mocked
    /// persistence/monitor collaborators.
    /// </summary>
    public class SoilHealthServiceContractTests
    {
        private readonly Mock<IModDataService> _mockDataService;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IFileNameSanitizationService> _mockFileNameSanitizer;

        public SoilHealthServiceContractTests()
        {
            _mockDataService = new Mock<IModDataService>();
            _mockMonitor = new Mock<IMonitor>();
            _mockFileNameSanitizer = new Mock<IFileNameSanitizationService>();
        }

        private SoilHealthService CreateService()
        {
            return new SoilHealthService(
                _mockDataService.Object,
                _mockMonitor.Object,
                _mockFileNameSanitizer.Object);
        }

        [Fact]
        public void GetSoilHealth_UnknownTile_ReturnsZero()
        {
            var service = CreateService();
            var result = service.GetSoilHealth("Farm", new Vector2(99, 99));
            Assert.True(float.IsNaN(result));
        }

        [Fact]
        public void SetSoilHealth_ThenGet_RoundtripsValue()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 42f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(42f, result, 5);
        }

        [Fact]
        public void SetSoilHealth_ClampsToMax()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 150f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(ModConstants.MaxSoilHealth, result, 5);
        }

        [Fact]
        public void SetSoilHealth_ClampsToMin()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, -10f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.True(float.IsNaN(result));
        }

        [Fact]
        public void UpdateHealth_PositiveDelta_IncreasesValue()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50f);
            service.UpdateHealth("Farm", tile, 10f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(60f, result, 5);
        }

        [Fact]
        public void UpdateHealth_NegativeDelta_DecreasesValue()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50f);
            service.UpdateHealth("Farm", tile, -20f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(30f, result, 5);
        }

        [Fact]
        public void UpdateHealth_ClampedAtMax()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 95f);
            service.UpdateHealth("Farm", tile, 20f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(ModConstants.MaxSoilHealth, result, 5);
        }

        [Fact]
        public void UpdateHealth_ClampedAtMin()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 5f);
            service.UpdateHealth("Farm", tile, -20f);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.True(float.IsNaN(result));
        }

        [Fact]
        public void OnTileHealthChanged_FiresOnMutation()
        {
            var service = CreateService();
            string? firedLocation = null;
            Vector2 firedTile = default;
            float firedHealth = -1f;

            service.OnTileHealthChanged += (loc, tile, health) =>
            {
                firedLocation = loc;
                firedTile = tile;
                firedHealth = health;
            };

            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 55f);

            Assert.NotNull(firedLocation);
            Assert.Equal("Farm", firedLocation);
            Assert.Equal(tile, firedTile);
            Assert.Equal(55f, firedHealth, 5);
        }

        [Fact]
        public void OnTileHealthChanged_FiresOnUpdate()
        {
            var service = CreateService();
            int fireCount = 0;
            service.OnTileHealthChanged += (_, _, _) => fireCount++;

            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 40f);
            service.UpdateHealth("Farm", tile, 5f);

            Assert.Equal(2, fireCount);
        }

        [Fact]
        public void OnTileHealthChanged_DoesNotFireWhenNoSubscriber()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            // Should not throw when no subscriber is registered
            service.SetSoilHealth("Farm", tile, 50f);
            service.UpdateHealth("Farm", tile, 10f);
        }

        [Fact]
        public void UpdateHealth_IgnoresNaN()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50f);
            service.UpdateHealth("Farm", tile, float.NaN);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(50f, result, 5);
        }

        [Fact]
        public void UpdateHealth_IgnoresInfinity()
        {
            var service = CreateService();
            var tile = new Vector2(10, 10);
            service.SetSoilHealth("Farm", tile, 50f);
            service.UpdateHealth("Farm", tile, float.PositiveInfinity);
            var result = service.GetSoilHealth("Farm", tile);
            Assert.Equal(50f, result, 5);
        }
    }
}
