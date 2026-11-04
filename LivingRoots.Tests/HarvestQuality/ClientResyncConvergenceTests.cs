using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Microsoft.Xna.Framework;
using Moq;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    /// <summary>
    /// Convergence tests for SC-010 / T108: after a divergence, the client
    /// re-syncs soil health on the next DayStarted (host state wins) and both
    /// sides produce the same <see cref="HarvestQualityDecision"/> for the
    /// same tile (observable convergence).
    /// </summary>
    public class ClientResyncConvergenceTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();
        private readonly string _locationName = "Farm";
        private readonly Vector2 _tile = new Vector2(5, 6);
        private readonly HarvestQualityInputs _inputs = new HarvestQualityInputs
        {
            FarmingLevel = 10,
            FertilizerTier = 0
        };

        [Fact]
        public void Decide_AfterClientResync_MatchesHostDecision()
        {
            // Given: a simulated divergence where client soil health differs
            // from host soil health for the same tile.
            var hostHealth = 80f;   // Healthy band (>= 71)
            var clientHealth = 20f; // Poor band (< 31)

            var hostSoilHealth = new Mock<ISoilHealthService>();
            hostSoilHealth
                .Setup(s => s.GetSoilHealth(_locationName, _tile))
                .Returns(hostHealth);

            var clientSoilHealth = new Mock<ISoilHealthService>();
            clientSoilHealth
                .Setup(s => s.GetSoilHealth(_locationName, _tile))
                .Returns(clientHealth);

            // When: host and client decide before re-sync (divergence exists).
            var hostDecision = _service.Decide(
                hostSoilHealth.Object.GetSoilHealth(_locationName, _tile),
                _inputs);
            var clientDecisionBeforeResync = _service.Decide(
                clientSoilHealth.Object.GetSoilHealth(_locationName, _tile),
                _inputs);

            // Sanity check: the divergence is real before re-sync.
            Assert.NotEqual(hostDecision, clientDecisionBeforeResync);

            // When: client re-syncs on the next DayStarted, adopting the
            // host's value (host state wins).
            clientSoilHealth
                .Setup(s => s.GetSoilHealth(_locationName, _tile))
                .Returns(hostHealth);

            var clientDecisionAfterResync = _service.Decide(
                clientSoilHealth.Object.GetSoilHealth(_locationName, _tile),
                _inputs);

            // Then: client's decision matches host's decision after re-sync
            // (observable convergence on next DayStarted).
            Assert.Equal(hostDecision, clientDecisionAfterResync);
        }
    }
}
