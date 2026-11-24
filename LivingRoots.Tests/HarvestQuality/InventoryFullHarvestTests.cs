using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class InventoryFullHarvestTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Property]
        public Property InventoryFull_MainItemBlockedAndExtraDroppedAsDebris(float health)
        {
            var inputs = new HarvestQualityInputs
            {
                FarmingLevel = 10,
                FertilizerTier = 0,
                IsInventoryFull = true
            };

            var decision = _service.Decide(health, inputs);

            return (decision.MainItemBlocked && decision.ExtraItemDroppedAsDebris).ToProperty();
        }
    }
}
