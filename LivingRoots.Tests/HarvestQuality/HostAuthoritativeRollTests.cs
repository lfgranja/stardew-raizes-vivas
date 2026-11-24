using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HostAuthoritativeRollTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void Decide_SameInputs_ReturnsDeterministicDecision()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };

            var first = _service.Decide(75f, inputs);
            var second = _service.Decide(75f, inputs);

            Assert.Equal(first.Tier, second.Tier);
            Assert.Equal(first.QualityBoost, second.QualityBoost);
        }

        [Fact]
        public void HostAndFarmhand_SameInputsAndSoilHealth_ProduceIdenticalDecisions()
        {
            // Given: same soil health and inputs on host and simulated farmhand client
            var soilHealth = 78.0f;
            var inputs = new HarvestQualityInputs
            {
                FarmingLevel = 10,
                FertilizerTier = 1,
                IsPaddyCrop = false,
                RegrowDays = 0,
                HarvestMaxIncreasePerFarmingLevel = 0,
                IsForageCrop = false,
                IsFibreSeed = false,
                IsQiFruit = false,
                PestQualityPenalty = null,
                IsInventoryFull = false
            };

            // When: both call Decide + BoostQualityProbability + BoostExtraHarvestChance
            var hostDecision = _service.Decide(soilHealth, inputs);
            var hostBoostedQuality = _service.BoostQualityProbability(0.20f, hostDecision);
            var hostBoostedExtra = _service.BoostExtraHarvestChance(0.05f, hostDecision);

            var farmhandDecision = _service.Decide(soilHealth, inputs);
            var farmhandBoostedQuality = _service.BoostQualityProbability(0.20f, farmhandDecision);
            var farmhandBoostedExtra = _service.BoostExtraHarvestChance(0.05f, farmhandDecision);

            // Then: both produce identical decisions — deterministic, client-local, no divergence
            Assert.Equal(hostDecision.Tier, farmhandDecision.Tier);
            Assert.Equal(hostDecision.ForceQualityZero, farmhandDecision.ForceQualityZero);
            Assert.Equal(hostDecision.QualityBoost, farmhandDecision.QualityBoost);
            Assert.Equal(hostDecision.ExtraHarvestBoost, farmhandDecision.ExtraHarvestBoost);
            Assert.Equal(hostDecision.IsInScope, farmhandDecision.IsInScope);
            Assert.Equal(hostBoostedQuality, farmhandBoostedQuality);
            Assert.Equal(hostBoostedExtra, farmhandBoostedExtra);
        }

        [Property]
        public Property HostAndFarmhand_ArbitraryValidInputs_ProduceIdenticalDecisions(
            float health,
            int farmingLevel,
            int fertilizerTier,
            bool isPaddyCrop,
            int regrowDays,
            bool isForageCrop,
            bool isFibreSeed,
            bool isQiFruit,
            bool isInventoryFull)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            if (farmingLevel < 0 || farmingLevel > 10)
            {
                return true.ToProperty();
            }

            if (fertilizerTier < 0 || fertilizerTier > 3)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs
            {
                FarmingLevel = farmingLevel,
                FertilizerTier = fertilizerTier,
                IsPaddyCrop = isPaddyCrop,
                RegrowDays = regrowDays,
                HarvestMaxIncreasePerFarmingLevel = 0,
                IsForageCrop = isForageCrop,
                IsFibreSeed = isFibreSeed,
                IsQiFruit = isQiFruit,
                PestQualityPenalty = null,
                IsInventoryFull = isInventoryFull
            };

            var host = _service.Decide(health, inputs);
            var farmhand = _service.Decide(health, inputs);

            var hostQuality = _service.BoostQualityProbability(0.25f, host);
            var farmhandQuality = _service.BoostQualityProbability(0.25f, farmhand);
            var hostExtra = _service.BoostExtraHarvestChance(0.10f, host);
            var farmhandExtra = _service.BoostExtraHarvestChance(0.10f, farmhand);

            return (
                host.Tier == farmhand.Tier
                && host.ForceQualityZero == farmhand.ForceQualityZero
                && host.QualityBoost == farmhand.QualityBoost
                && host.ExtraHarvestBoost == farmhand.ExtraHarvestBoost
                && host.IsInScope == farmhand.IsInScope
                && hostQuality == farmhandQuality
                && hostExtra == farmhandExtra
            ).ToProperty();
        }
    }
}
