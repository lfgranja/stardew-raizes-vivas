using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HarvestQualityMathTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();
        private const float Tolerance = 0.00001f;

        [Property]
        public Property ComputeBaselineProbability_MatchesVanillaFormula(
            int farmingLevel,
            int fertilizerTier,
            bool isPaddyCrop,
            int regrowDays)
        {
            if (farmingLevel < 0 || farmingLevel > 10)
            {
                return true.ToProperty();
            }

            if (fertilizerTier < 0 || fertilizerTier > 3)
            {
                return true.ToProperty();
            }

            int normalizedRegrow = regrowDays > 0 ? 5 : 0;

            float expected = 0.2f * (farmingLevel / 10.0f)
                             + 0.2f * fertilizerTier * ((farmingLevel + 2) / 12.0f)
                             + 0.01f;
            if (isPaddyCrop)
            {
                expected += 0.1f;
            }

            if (normalizedRegrow > 0)
            {
                expected += 0.1f;
            }

            float actual = HarvestQualityMath.ComputeBaselineProbability(
                farmingLevel,
                fertilizerTier,
                isPaddyCrop,
                normalizedRegrow);

            return (Math.Abs(actual - expected) < Tolerance).ToProperty();
        }

        [Property]
        public Property ComputeSilverProbability_IsMinOfOnePointFiveAndDoubleP(float baseline)
        {
            if (float.IsNaN(baseline) || float.IsInfinity(baseline) || baseline < 0.0f || baseline > 1.0f)
            {
                return true.ToProperty();
            }

            float expected = Math.Min(0.75f, 2.0f * baseline);
            float actual = HarvestQualityMath.ComputeSilverProbability(baseline);

            return (Math.Abs(actual - expected) < Tolerance).ToProperty();
        }

        [Property]
        public Property ComputeIridiumProbability_OnlyDeluxeFertilizer(float baseline, int fertilizerTier)
        {
            if (float.IsNaN(baseline) || float.IsInfinity(baseline) || baseline < 0.0f || baseline > 1.0f)
            {
                return true.ToProperty();
            }

            if (fertilizerTier < 0 || fertilizerTier > 3)
            {
                return true.ToProperty();
            }

            float expected = fertilizerTier >= 3 ? baseline / 2.0f : 0.0f;
            float actual = HarvestQualityMath.ComputeIridiumProbability(baseline, fertilizerTier);

            return (Math.Abs(actual - expected) < Tolerance).ToProperty();
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(15.0f)]
        [InlineData(30.999f)]
        public void PoorTier_ForceQualityZero_ForMeasuredPoorHealth(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 1 };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.Poor, decision.Tier);
            Assert.True(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost, 5);
            Assert.Equal(0.0f, decision.ExtraHarvestBoost, 5);
        }

        [Fact]
        public void ModerateBoundary_At31_DoesNotForceQualityZero()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 1 };
            var decision = _service.Decide(31.0f, inputs);

            Assert.False(decision.ForceQualityZero);
        }

        [Property]
        public Property ModerateTier_IsByteIdenticalToVanilla(float health, int farmingLevel, int fertilizerTier)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 31.0f || health >= 71.0f)
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
                IsPaddyCrop = false,
                RegrowDays = 0,
            };

            var decision = _service.Decide(health, inputs);

            return (decision.QualityBoost == 0.0f
                    && decision.ExtraHarvestBoost == 0.0f
                    && _service.BoostQualityProbability(0.25f, decision) == 0.25f
                    && _service.BoostExtraHarvestChance(0.10f, decision) == 0.10f).ToProperty();
        }

        [Property]
        public Property HealthyTier_AppliesSoilBoosts(float health, int farmingLevel, int fertilizerTier)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 71.0f || health > 100.0f)
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
            };

            var decision = _service.Decide(health, inputs);

            return (Math.Abs(decision.QualityBoost - ModConstants.SoilQualityHealthyBoost) < Tolerance
                    && Math.Abs(decision.ExtraHarvestBoost - ModConstants.SoilExtraHarvestHealthyBoost) < Tolerance
                    && Math.Abs(_service.BoostQualityProbability(0.25f, decision) - (0.25f + ModConstants.SoilQualityHealthyBoost)) < Tolerance
                    && Math.Abs(_service.BoostExtraHarvestChance(0.10f, decision) - (0.10f + ModConstants.SoilExtraHarvestHealthyBoost)) < Tolerance).ToProperty();
        }

        [Property]
        public Property BoostedProbabilities_AreBoundedInUnitInterval(float health, float probability, float extraChance)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            if (float.IsNaN(probability) || float.IsInfinity(probability) || probability < 0.0f || probability > 1.0f)
            {
                return true.ToProperty();
            }

            if (float.IsNaN(extraChance) || float.IsInfinity(extraChance) || extraChance < 0.0f || extraChance > 1.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 2 };
            var decision = _service.Decide(health, inputs);

            float quality = _service.BoostQualityProbability(probability, decision);
            float extra = _service.BoostExtraHarvestChance(extraChance, decision);

            return (quality >= 0.0f && quality <= 1.0f && extra >= 0.0f && extra <= 1.0f).ToProperty();
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void UnknownTier_DoesNotModifyHarvest(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5, FertilizerTier = 1 };
            var decision = _service.Decide(health, inputs);

            Assert.Equal(HarvestTier.Unknown, decision.Tier);
            Assert.False(decision.ForceQualityZero);
            Assert.Equal(0.0f, decision.QualityBoost, 5);
            Assert.Equal(0.0f, decision.ExtraHarvestBoost, 5);
        }
    }
}
