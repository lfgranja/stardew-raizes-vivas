using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Spec 020 soil-effect math and identity predicates (FR-006/FR-007/FR-018):
/// pure logic in <see cref="HeirloomSoilMath"/> / <see cref="HeirloomIdentity"/>
/// so the Harmony postfixes stay thin glue over tested rules.
/// </summary>
public class HeirloomSoilEffectsTests
{
    // ------------------------------------------------------------------
    // Identity predicates (qualified IDs, context tags)
    // ------------------------------------------------------------------

    [Fact]
    public void Identity_IsHeirloomSeed_MatchesQualifiedAndUnqualifiedForms()
    {
        Assert.True(HeirloomIdentity.IsHeirloomSeedId(ModConstants.HeirloomItemId));
        Assert.True(HeirloomIdentity.IsHeirloomSeedId(ModConstants.QualifiedHeirloomItemId));
        Assert.False(HeirloomIdentity.IsHeirloomSeedId("472"));
        Assert.False(HeirloomIdentity.IsHeirloomSeedId(null));
        Assert.False(HeirloomIdentity.IsHeirloomSeedId(string.Empty));
    }

    [Fact]
    public void Identity_IsHeirloomProduce_MatchesQualifiedAndUnqualifiedForms()
    {
        Assert.True(HeirloomIdentity.IsHeirloomProduceId(ModConstants.HeirloomProduceItemId));
        Assert.True(HeirloomIdentity.IsHeirloomProduceId("(O)LivingRoots.HeirloomParsnip"));
        Assert.False(HeirloomIdentity.IsHeirloomProduceId("24"));
        Assert.False(HeirloomIdentity.IsHeirloomProduceId(null));
    }

    // ------------------------------------------------------------------
    // Plant bonus math (NaN→100 baseline, clamp, bounded write)
    // ------------------------------------------------------------------

    [Fact]
    public void PlantBonus_MeasuredBaseline_AddsBoundedGain()
    {
        Assert.Equal(52f, HeirloomSoilMath.Plant(50f));
    }

    [Fact]
    public void PlantBonus_ClampsAt100()
    {
        Assert.Equal(100f, HeirloomSoilMath.Plant(99.5f));
        Assert.Equal(100f, HeirloomSoilMath.Plant(100f));
    }

    [Fact]
    public void PlantBonus_UnmeasuredBaseline_ResolvesTo100_BoundedWrite()
    {
        Assert.Equal(100f, HeirloomSoilMath.Plant(float.NaN));
    }

    [Fact]
    public void PlantBonus_ZeroBaseline_IsBoundedWrite()
    {
        Assert.Equal(2f, HeirloomSoilMath.Plant(0f));
    }

    // ------------------------------------------------------------------
    // Harvest bonus (Healthy-band gate ≥71; otherwise no-op)
    // ------------------------------------------------------------------

    [Fact]
    public void HarvestBonus_HealthyBand_Applies()
    {
        var applied = HeirloomSoilMath.Harvest(85f, out float result);
        Assert.True(applied);
        Assert.Equal(86f, result);
    }

    [Fact]
    public void HarvestBonus_ModerateBand_DoesNotApply()
    {
        var applied = HeirloomSoilMath.Harvest(50f, out float result);
        Assert.False(applied);
        Assert.Equal(50f, result);
    }

    [Fact]
    public void HarvestBonus_AtBandEdge_Applies()
    {
        var applied = HeirloomSoilMath.Harvest(ModConstants.ModerateHealthyBandEdge, out float result);
        Assert.True(applied);
        Assert.Equal(ModConstants.ModerateHealthyBandEdge + ModConstants.HeirloomHarvestBonus, result);
    }

    [Fact]
    public void HarvestBonus_UnmeasuredBaseline_DoesNotApply()
    {
        var applied = HeirloomSoilMath.Harvest(float.NaN, out float result);
        Assert.False(applied);
        Assert.True(float.IsNaN(result));
    }

    [Fact]
    public void HarvestBonus_HealthyBandAt100_ClampsAt100()
    {
        var applied = HeirloomSoilMath.Harvest(100f, out float result);
        Assert.True(applied);
        Assert.Equal(100f, result);
    }

    // ------------------------------------------------------------------
    // Bound invariants (property-shaped assertions over the valid domain)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(0f)]
    [InlineData(10.5f)]
    [InlineData(31f)]
    [InlineData(70f)]
    [InlineData(71f)]
    [InlineData(100f)]
    public void PlantBonus_AlwaysStaysWithin0To100(float baseline)
    {
        var result = HeirloomSoilMath.Plant(baseline);
        Assert.InRange(result, 0f, 100f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(10.5f)]
    [InlineData(31f)]
    [InlineData(70f)]
    [InlineData(71f)]
    [InlineData(100f)]
    public void HarvestBonus_AlwaysStaysWithin0To100(float baseline)
    {
        var applied = HeirloomSoilMath.Harvest(baseline, out float result);
        Assert.False(float.IsNaN(result));
        // NaN is never emitted by the implementation for measured inputs.
        Assert.InRange(result, 0f, 100f);
    }
}
