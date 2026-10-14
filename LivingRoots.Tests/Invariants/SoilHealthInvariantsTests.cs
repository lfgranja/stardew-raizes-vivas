using System.Threading;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Property-based invariant tests for soil health bounds and clamping (T027,
/// US3): for any initial health and any compost applications, health stays in
/// [0, 100], with exactly <see cref="ModConstants.RestorationAmount"/> per
/// valid application.
/// </summary>
public class SoilHealthInvariantsTests
{
    private readonly SoilHealthService _service;
    private int _caseSalt;

    public SoilHealthInvariantsTests()
    {
        _service = new SoilHealthService(
            new Mock<IModDataService>().Object,
            new Mock<IMonitor>().Object,
            new Mock<IFileNameSanitizationService>().Object);
    }

    /// <summary>
    /// Invariant: for any finite initial health and any application count,
    /// every compost application adds exactly
    /// <see cref="ModConstants.RestorationAmount"/> clamped to
    /// <see cref="ModConstants.MaxSoilHealth"/>, and the result never leaves
    /// [0, 100].
    /// </summary>
    [Property(MaxTest = 100)]
    public void CompostApplication_ForAnyInitialHealth_AddsExactlyRestorationAmountClampedToBounds(
        float initialHealth,
        PositiveInt applicationCount)
    {
        float seed = float.IsNaN(initialHealth) || float.IsInfinity(initialHealth)
            ? 0f
            : initialHealth;
        int salt = Interlocked.Increment(ref _caseSalt);
        var tile = new Vector2(salt % 64, (salt / 64) % 64);
        int applications = (applicationCount.Get % 5) + 1;

        _service.SetSoilHealth("Farm", tile, seed);
        float expected = Math.Clamp(seed, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        for (int i = 0; i < applications; i++)
        {
            _service.UpdateHealth("Farm", tile, ModConstants.RestorationAmount);
            expected = Math.Min(expected + ModConstants.RestorationAmount, ModConstants.MaxSoilHealth);
        }

        float actual = _service.GetSoilHealth("Farm", tile);
        Assert.InRange(actual, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Invariant: for any finite initial health and any decay magnitude, a
    /// single large negative delta floors at
    /// <see cref="ModConstants.MinSoilHealth"/> and the result never leaves
    /// [0, 100].
    /// </summary>
    [Property(MaxTest = 100)]
    public void SoilHealth_ForAnyInitialHealthAndLargeDecay_NeverDropsBelowMin(
        float initialHealth,
        PositiveInt decayMagnitude)
    {
        float seed = float.IsNaN(initialHealth) || float.IsInfinity(initialHealth)
            ? 0f
            : initialHealth;
        int salt = Interlocked.Increment(ref _caseSalt);
        var tile = new Vector2(salt % 64, (salt / 64) % 64);
        float decay = (decayMagnitude.Get % 1000) + 1;

        _service.SetSoilHealth("Farm", tile, seed);
        float clampedSeed = Math.Clamp(seed, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        _service.UpdateHealth("Farm", tile, -decay);

        float expected = Math.Max(clampedSeed - decay, ModConstants.MinSoilHealth);
        float actual = _service.GetSoilHealth("Farm", tile);
        Assert.InRange(actual, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);
        Assert.Equal(expected, actual);
    }
}
