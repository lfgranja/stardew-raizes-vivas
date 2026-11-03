using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class ExtraHarvestTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Property]
        public Property Healthy_BoostExtraHarvestChance_AtLeast010(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 71.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);
            var boosted = _service.BoostExtraHarvestChance(0.0f, decision);

            return (boosted >= 0.10f).ToProperty();
        }

        [Property]
        public Property Poor_BoostExtraHarvestChance_Unchanged(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0.0f || health >= 31.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);
            var boosted = _service.BoostExtraHarvestChance(0.0f, decision);

            return (boosted == 0.0f).ToProperty();
        }

        [Property]
        public Property Moderate_BoostExtraHarvestChance_Unchanged(float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 31.0f || health >= 71.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };
            var decision = _service.Decide(health, inputs);
            var boosted = _service.BoostExtraHarvestChance(0.0f, decision);

            return (boosted == 0.0f).ToProperty();
        }
    }
}
