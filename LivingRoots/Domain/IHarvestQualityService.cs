namespace LivingRoots.Domain
{
    public interface IHarvestQualityService
    {
        HarvestQualityDecision Decide(float soilHealth, HarvestQualityInputs inputs);
        HarvestFeedbackFlags ResolveFeedback(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred);
        HarvestFeedbackPlan BuildFeedbackPlan(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred, bool isMainPlayer);
        float BoostQualityProbability(float vanillaProbability, HarvestQualityDecision decision);
        float BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision decision);
    }
}
