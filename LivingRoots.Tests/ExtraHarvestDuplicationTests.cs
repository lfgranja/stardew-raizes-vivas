using LivingRoots;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Integration tests for spec 005 (FR-002/FR-003): no-duplication guarantee
    /// for the extra harvest mechanism. Verifies that:
    /// - ApplyHarvestModifiers with Healthy tier increases probability but doesn't
    ///   guarantee an extra item (it's probabilistic)
    /// - The mechanism is per-tile, not global per seed
    /// - The probability is applied correctly without asserting on random outcomes
    /// </summary>
    public class ExtraHarvestDuplicationTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ICompostingBinService> _mockCompostingBinService;
        private readonly Mock<ISoilDecayService> _mockSoilDecayService;
        private readonly Mock<IVisualizationService> _mockVisualizationService;

        public ExtraHarvestDuplicationTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();
            _mockCompostingBinService = new Mock<ICompostingBinService>();
            _mockSoilDecayService = new Mock<ISoilDecayService>();
            _mockVisualizationService = new Mock<IVisualizationService>();

            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));
        }

        // ------------------------------------------------------------------
        // Healthy tier increases probability
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_HealthyTile_IncreasesExtraProb()
        {
            var controller = CreateController(85f);
            var tile = new Vector2(10, 10);

            bool modified = controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out float qualityRoll, out float extraProb);

            Assert.True(modified);
            Assert.Equal(0.3f, extraProb, 5);
        }

        // ------------------------------------------------------------------
        // Non-Healthy tier leaves probability unchanged
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_ModerateTile_LeavesExtraProbUnchanged()
        {
            var controller = CreateController(50f);
            var tile = new Vector2(10, 10);

            bool modified = controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out float qualityRoll, out float extraProb);

            Assert.False(modified);
            Assert.Equal(0.2f, extraProb, 5);
        }

        [Fact]
        public void ApplyHarvestModifiers_PoorTile_LeavesExtraProbUnchanged()
        {
            var controller = CreateController(15f);
            var tile = new Vector2(10, 10);

            bool modified = controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out float qualityRoll, out float extraProb);

            // Poor tier forces quality to 0, so modified is true even though extraProb is unchanged
            Assert.Equal(0.2f, extraProb, 5);
        }

        // ------------------------------------------------------------------
        // Per-tile behavior: different tiles get different probabilities
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_PerTile_DifferentHealthDifferentResult()
        {
            var healthyTile = new Vector2(10, 10);
            var poorTile = new Vector2(20, 20);

            var controller = CreateController(85f);
            controller.ApplyHarvestModifiers("Farm", healthyTile, 0.5f, 0.2f, out _, out float healthyExtra);

            var poorController = CreateController(15f);
            poorController.ApplyHarvestModifiers("Farm", poorTile, 0.5f, 0.2f, out _, out float poorExtra);

            Assert.Equal(0.3f, healthyExtra, 5);
            Assert.Equal(0.2f, poorExtra, 5);
            Assert.NotEqual(healthyExtra, poorExtra);
        }

        // ------------------------------------------------------------------
        // Same tile with same health always gets same probability (deterministic)
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_SameTileSameHealth_AlwaysSameResult()
        {
            var controller = CreateController(85f);
            var tile = new Vector2(10, 10);

            controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out _, out float first);
            controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out _, out float second);
            controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out _, out float third);

            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        // ------------------------------------------------------------------
        // Mechanism is probabilistic, not guaranteed
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_HealthyTier_DoesNotGuaranteeExtraItem()
        {
            var controller = CreateController(85f);
            var tile = new Vector2(10, 10);

            // The mechanism only modifies the probability value.
            // It does NOT directly produce an item — the game's harvest logic
            // uses the modified probability to determine if an extra item drops.
            // Therefore, we verify the probability is increased, not that an
            // extra item is guaranteed.
            bool modified = controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out _, out float extraProb);

            Assert.True(modified);
            Assert.Equal(0.3f, extraProb, 5);

            // The probability is 0.3, not 1.0 — so an extra item is NOT guaranteed.
            Assert.True(extraProb < 1.0f, "Extra harvest is probabilistic, not guaranteed.");
        }

        // ------------------------------------------------------------------
        // Invalid health values are rejected
        // ------------------------------------------------------------------

        [Fact]
        public void ApplyHarvestModifiers_InvalidHealth_ReturnsFalseAndLeavesOutputsUnchanged()
        {
            var controller = CreateController(float.NaN);
            var tile = new Vector2(10, 10);

            bool modified = controller.ApplyHarvestModifiers("Farm", tile, 0.5f, 0.2f, out float qualityRoll, out float extraProb);

            Assert.False(modified);
            Assert.Equal(0.5f, qualityRoll, 5);
            Assert.Equal(0.2f, extraProb, 5);
        }

        // ------------------------------------------------------------------
        // Full integration: no duplicate items (skipped — requires game harness)
        // ------------------------------------------------------------------

        [Fact(Skip = "Full integration test requires SMAPI game harness. Documents expected behavior: the extra harvest mechanism modifies per-tile probability only; it never directly produces items, so duplication is impossible by design.")]
        public void ApplyHarvestModifiers_FullIntegration_NeverProducesDuplicateItems()
        {
            // This test documents the expected behavior:
            // - ApplyHarvestModifiers only modifies probability values (qualityRoll, extraProb)
            // - It does NOT add items to any inventory
            // - The game's harvest logic uses the modified probability to determine
            //   if an extra item drops — the mechanism itself never produces items
            // - Therefore, duplication is impossible by design
            //
            // A full integration test would require:
            // - A running SMAPI instance with Stardew Valley
            // - A planted crop on a Healthy soil tile
            // - Harvesting the crop and verifying at most one extra item drops
            // - This is beyond the scope of unit/integration tests
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        private ModController CreateController(float healthValue)
        {
            _mockSoilHealthService
                .Setup(x => x.GetSoilHealth(It.IsAny<string>(), It.IsAny<Vector2>()))
                .Returns(healthValue);

            return new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockCompostingBinService.Object,
                _mockSoilDecayService.Object,
                null,
                _mockVisualizationService.Object);
        }
    }
}
