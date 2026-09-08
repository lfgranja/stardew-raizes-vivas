using Xunit;
using LivingRoots.Domain.Services;

namespace LivingRoots.Tests;

public class SeasonalDecayMultiplierTests
{
    private readonly SeasonalDecayMultiplier _multiplier;

    public SeasonalDecayMultiplierTests()
    {
        _multiplier = new SeasonalDecayMultiplier();
    }

    [Fact]
    public void GetMultiplier_Spring_ReturnsHalf()
    {
        Assert.Equal(0.5f, _multiplier.GetMultiplier("spring"));
    }

    [Fact]
    public void GetMultiplier_Summer_ReturnsOneAndHalf()
    {
        Assert.Equal(1.5f, _multiplier.GetMultiplier("summer"));
    }

    [Fact]
    public void GetMultiplier_Fall_ReturnsHalf()
    {
        Assert.Equal(0.5f, _multiplier.GetMultiplier("fall"));
    }

    [Fact]
    public void GetMultiplier_Winter_ReturnsZero()
    {
        Assert.Equal(0.0f, _multiplier.GetMultiplier("winter"));
    }

    [Fact]
    public void GetMultiplier_InvalidSeason_ReturnsDefault()
    {
        Assert.Equal(0.5f, _multiplier.GetMultiplier("invalid"));
    }
}
