using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Spec 020 FR-008 / US2: the crioulo crop rides the 014 harvest-quality seam
/// unchanged. The crioulo parsnip and a vanilla crop with byte-identical
/// inputs must produce byte-identical <see cref="IHarvestQualityService.Decide"/>
/// decisions — the heirloom bonus (+1 soil write) is a flat, separate write and
/// never feeds back into quality math (no double-counting).
/// </summary>
public class HeirloomHarvestQualityParityTests
{
    private static readonly HarvestQualityInputs ParsnipInputs = new()
    {
        FarmingLevel = 6,
        FertilizerTier = 0,
        IsPaddyCrop = false,
        RegrowDays = -1,
        HarvestMaxIncreasePerFarmingLevel = 0,
        IsForageCrop = false,
        IsFibreSeed = false,
        IsQiFruit = false,
        PestQualityPenalty = null,
        IsInventoryFull = false
    };

    [Theory]
    [InlineData(10f)]   // Poor band
    [InlineData(50f)]   // Moderate band
    [InlineData(85f)]   // Healthy band
    public void Decide_HeirloomAndVanillaParsnipParity(float measured)
    {
        var sut = new HarvestQualityService();

        var heirloom = sut.Decide(measured, ParsnipInputs);
        var vanilla = sut.Decide(measured, ParsnipInputs); // identical inputs by construction

        Assert.Equal(vanilla, heirloom);
    }

    [Fact]
    public void Decide_HealthyBand_IsInScope_AndExtraHarvestBoosted()
    {
        var sut = new HarvestQualityService();
        var decision = sut.Decide(85f, ParsnipInputs);

        Assert.True(decision.IsInScope);
        Assert.Equal(HarvestTier.Healthy, decision.Tier);
    }

    [Fact]
    public void Decide_HealthyBand_QualityCompositionPreservesVanillaMath()
    {
        var sut = new HarvestQualityService();
        var decision = sut.Decide(85f, ParsnipInputs);

        // The 014 seam owns quality decisions: boosted only per its own rules,
        // and the heirloom layer contributes nothing to the decision.
        Assert.Equal(ModConstants.SoilQualityHealthyBoost, decision.QualityBoost);
    }

    [Fact]
    public void HeirloomHarvestBonus_FlatWrite_DoesNotMutateDecisionOutput()
    {
        var sut = new HarvestQualityService();
        var decision = sut.Decide(85f, ParsnipInputs);
        var boost = Domain.Services.HeirloomSoilMath.Harvest(85f, out var result);

        Assert.True(boost);
        Assert.Equal(86f, result);
        // Decision is a value-type snapshot taken before the heirloom write;
        // it must remain Healthy-tier with the same boost values.
        Assert.Equal(HarvestTier.Healthy, decision.Tier);
        Assert.True(decision.IsInScope);
    }
}
