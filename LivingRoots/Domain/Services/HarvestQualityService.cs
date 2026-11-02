using System;
using LivingRoots;

namespace LivingRoots.Domain.Services
{
    public class HarvestQualityService : IHarvestQualityService
    {
        private const float PoorThreshold = 31.0f;
        private const float HealthyThreshold = 71.0f;

        public HarvestQualityDecision Decide(float soilHealth, HarvestQualityInputs inputs)
        {
            if (inputs == null)
            {
                inputs = new HarvestQualityInputs();
            }

            if (inputs.IsForageCrop || inputs.IsFibreSeed || inputs.IsQiFruit)
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.OutOfScope,
                    IsInScope = false,
                    ForceQualityZero = false,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = false,
                    ExtraItemDroppedAsDebris = false
                };
            }

            if (float.IsNaN(soilHealth) || float.IsInfinity(soilHealth))
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.Unknown,
                    IsInScope = false,
                    ForceQualityZero = false,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = false,
                    ExtraItemDroppedAsDebris = false
                };
            }

            if (soilHealth < PoorThreshold)
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.Poor,
                    IsInScope = true,
                    ForceQualityZero = true,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = false,
                    ExtraItemDroppedAsDebris = false
                };
            }

            if (soilHealth < HealthyThreshold)
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.Moderate,
                    IsInScope = true,
                    ForceQualityZero = false,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = false,
                    ExtraItemDroppedAsDebris = false
                };
            }

            return new HarvestQualityDecision
            {
                Tier = HarvestTier.Healthy,
                IsInScope = true,
                ForceQualityZero = false,
                QualityBoost = ModConstants.SoilQualityHealthyBoost,
                ExtraHarvestBoost = ModConstants.SoilExtraHarvestHealthyBoost,
                PestQualityPenalty = inputs.PestQualityPenalty,
                MainItemBlocked = false,
                ExtraItemDroppedAsDebris = false
            };
        }

        public HarvestFeedback ResolveFeedback(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred)
        {
            if (decision == null)
            {
                return HarvestFeedback.None;
            }

            var flags = HarvestFeedback.None;
            if (decision.Tier == HarvestTier.Poor && decision.IsInScope)
            {
                flags |= HarvestFeedback.Poor;
            }

            if (decision.Tier == HarvestTier.Healthy && decision.IsInScope)
            {
                if (enhancedQualityOccurred || extraHarvestOccurred)
                {
                    flags |= HarvestFeedback.LivingQuality;
                }
                if (extraHarvestOccurred)
                {
                    flags |= HarvestFeedback.ExtraHarvest;
                }
            }

            return flags;
        }

        public float BoostQualityProbability(float vanillaProbability, HarvestQualityDecision decision)
        {
            if (decision == null || decision.Tier != HarvestTier.Healthy)
            {
                return vanillaProbability;
            }
            return vanillaProbability + decision.QualityBoost;
        }

        public float BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision decision)
        {
            if (decision == null || decision.Tier != HarvestTier.Healthy)
            {
                return vanillaChance;
            }
            return vanillaChance + decision.ExtraHarvestBoost;
        }
    }
}
