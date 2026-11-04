using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class ExtraHarvestStatisticalTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        // Deterministic roll mirroring HarvestPatches.cs: combines tile coordinates,
        // day and a game-unique ID into a hash, then maps it to [0, 1) via % 10000.
        private const int ExtraHarvestSalt = 0x5A827999;
        private const int HarvestDay = 10;
        private const int GameUniqueID = 123456789;
        private const int MinHarvests = 1000;
        private const int MaxHarvests = 100_000;

        private static double DeterministicRoll(int xTile, int yTile, int day, int uniqueID)
        {
            unchecked
            {
                int hash = (xTile * 397) ^ (yTile * 17) ^ day ^ uniqueID ^ ExtraHarvestSalt;
                return Math.Abs(hash % 10000) / 10000.0;
            }
        }

        [Property]
        public Property Healthy_ExtraHarvestRate_GreaterThan_Medium(float baseChance, int numHarvests)
        {
            // Restrict to a safe domain so boosted chance stays within [0, 1].
            if (float.IsNaN(baseChance) || float.IsInfinity(baseChance))
            {
                return true.ToProperty();
            }

            baseChance = Math.Clamp(baseChance, 0.0f, 0.80f);
            numHarvests = Math.Clamp(numHarvests, MinHarvests, MaxHarvests);

            var inputs = new HarvestQualityInputs { FarmingLevel = 10, FertilizerTier = 0 };

            // Healthy (Living) soil [71, 100]: tier Healthy → +0.10 extra-harvest boost.
            var healthyDecision = _service.Decide(90.0f, inputs);
            // Medium (Moderate) soil [31, 71): tier Moderate → no change.
            var mediumDecision = _service.Decide(50.0f, inputs);

            int healthyExtraCount = 0;
            int mediumExtraCount = 0;

            // Coupled rolls: the same roll is tested against both chances, isolating
            // the effect of the +0.10 boost (common random numbers).
            for (int i = 0; i < numHarvests; i++)
            {
                int xTile = i % 100;
                int yTile = i / 100;
                int day = HarvestDay + i; // diversify per-harvest input to the hash
                double roll = DeterministicRoll(xTile, yTile, day, GameUniqueID);

                float healthyChance = _service.BoostExtraHarvestChance(baseChance, healthyDecision);
                float mediumChance = _service.BoostExtraHarvestChance(baseChance, mediumDecision);

                if (roll < healthyChance)
                {
                    healthyExtraCount++;
                }
                if (roll < mediumChance)
                {
                    mediumExtraCount++;
                }
            }

            // With N >= 1000 and a deterministic +0.10 boost for Healthy, the
            // Healthy extra-harvest count must measurably exceed Medium (SC-009).
            // Expected gap ≈ 0.10 × N, far above sampling noise.
            return (healthyExtraCount > mediumExtraCount).ToProperty();
        }
    }
}
