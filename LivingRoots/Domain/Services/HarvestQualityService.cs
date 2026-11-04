using System;
using LivingRoots;

namespace LivingRoots.Domain.Services
{
    public class HarvestQualityService : IHarvestQualityService
    {
        public HarvestQualityDecision Decide(float soilHealth, HarvestQualityInputs inputs)
        {
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
                    MainItemBlocked = inputs.IsInventoryFull,
                    ExtraItemDroppedAsDebris = inputs.IsInventoryFull
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
                    MainItemBlocked = inputs.IsInventoryFull,
                    ExtraItemDroppedAsDebris = inputs.IsInventoryFull
                };
            }

            if (soilHealth < ModConstants.PoorModerateBandEdge)
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.Poor,
                    IsInScope = true,
                    ForceQualityZero = true,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = inputs.IsInventoryFull,
                    ExtraItemDroppedAsDebris = inputs.IsInventoryFull
                };
            }

            if (soilHealth < ModConstants.ModerateHealthyBandEdge)
            {
                return new HarvestQualityDecision
                {
                    Tier = HarvestTier.Moderate,
                    IsInScope = true,
                    ForceQualityZero = false,
                    QualityBoost = 0.0f,
                    ExtraHarvestBoost = 0.0f,
                    PestQualityPenalty = inputs.PestQualityPenalty,
                    MainItemBlocked = inputs.IsInventoryFull,
                    ExtraItemDroppedAsDebris = inputs.IsInventoryFull
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
                MainItemBlocked = inputs.IsInventoryFull,
                ExtraItemDroppedAsDebris = inputs.IsInventoryFull
            };
        }

        public HarvestFeedbackFlags ResolveFeedback(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred)
        {
            var flags = HarvestFeedbackFlags.None;
            if (decision.Tier == HarvestTier.Poor && decision.IsInScope)
            {
                flags |= HarvestFeedbackFlags.Poor;
            }

            if (decision.Tier == HarvestTier.Healthy && decision.IsInScope)
            {
                if (enhancedQualityOccurred || extraHarvestOccurred)
                {
                    flags |= HarvestFeedbackFlags.LivingQuality;
                }
                if (extraHarvestOccurred)
                {
                    flags |= HarvestFeedbackFlags.ExtraHarvest;
                }
            }

            return flags;
        }

        public float BoostQualityProbability(float vanillaProbability, HarvestQualityDecision decision)
        {
            if (decision.Tier != HarvestTier.Healthy)
            {
                return vanillaProbability;
            }
            return vanillaProbability + decision.QualityBoost;
        }

        public float BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision decision)
        {
            if (decision.Tier != HarvestTier.Healthy)
            {
                return vanillaChance;
            }
            return vanillaChance + decision.ExtraHarvestBoost;
        }
    }
}
