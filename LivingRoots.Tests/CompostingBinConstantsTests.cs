using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit tests verifying composting bin, soil health, and maturation constants in <see cref="ModConstants"/>.
/// </summary>
public class CompostingBinConstantsTests
{
    [Fact]
    public void MaturationDays_IsExactlyTwo()
    {
        Assert.Equal(2, ModConstants.MaturationDays);
    }

    [Fact]
    public void RestorationAmount_IsFifteen()
    {
        Assert.Equal(15f, ModConstants.RestorationAmount);
    }

    [Fact]
    public void SoilHealthBounds_AreZeroAndOneHundred()
    {
        Assert.Equal(0f, ModConstants.MinSoilHealth);
        Assert.Equal(100f, ModConstants.MaxSoilHealth);
    }

    [Fact]
    public void MachineCompostingBinId_MatchesExpectedId()
    {
        Assert.Equal("LivingRoots.CompostingBin", ModConstants.MachineCompostingBinId);
    }

    [Fact]
    public void CompostingBinItemId_MatchesExpectedId()
    {
        Assert.Equal("LivingRoots.CompostingBin", ModConstants.CompostingBinItemId);
    }

    [Fact]
    public void RecipeCosts_MatchSpecification()
    {
        Assert.Equal(50, ModConstants.CompostingBinWoodCost);
        Assert.Equal(25, ModConstants.CompostingBinStoneCost);
        Assert.Equal(15, ModConstants.CompostingBinFiberCost);
    }

    [Fact]
    public void ModDataKeys_MatchSpecification()
    {
        Assert.Equal("LivingRoots.InputId", ModConstants.ModDataInputIdKey);
        Assert.Equal("LivingRoots.DaysRemaining", ModConstants.ModDataDaysRemainingKey);
    }
}
