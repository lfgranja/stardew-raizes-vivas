namespace LivingRoots.Domain
{
    public interface IHarvestQualityService
    {
        HarvestQualityDecision Decide(float soilHealth, HarvestQualityInputs inputs);
        HarvestFeedback ResolveFeedback(HarvestQualityDecision decision, bool enhancedQualityOccurred, bool extraHarvestOccurred);
        float BoostQualityProbability(float vanillaProbability, HarvestQualityDecision decision);
        float BoostExtraHarvestChance(float vanillaChance, HarvestQualityDecision decision);
    }
}
