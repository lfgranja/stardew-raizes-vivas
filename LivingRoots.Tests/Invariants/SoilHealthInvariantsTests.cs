using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests verifying soil health bounds, clamping, and compost restoration deltas.
/// Validates Task T027 [P] [US3] under Feature 004 (Playable Compost Loop) and FR-007, FR-009.
/// </summary>
public class SoilHealthInvariantsTests
{
    private static SoilHealthService CreateService(out Mock<IModDataService> mockDataService)
    {
        mockDataService = new Mock<IModDataService>();
        var mockMonitor = new Mock<IMonitor>();
        var mockSanitizer = new Mock<IFileNameSanitizationService>();
        mockSanitizer.Setup(s => s.Sanitize(It.IsAny<string>()))
            .Returns<string?>(id => id);

        return new SoilHealthService(mockDataService.Object, mockMonitor.Object, mockSanitizer.Object);
    }

    /// <summary>
    /// Invariant: For any initial float health value set into SoilHealthService,
    /// the returned soil health is the T135 unknown sentinel (NaN) or strictly
    /// bounded in [0, 100] (FR-009, FR-014). A value that clamps to 0 — NaN,
    /// -Infinity, or any negative input — is omitted by the sparse cache, so
    /// the query reports the unknown sentinel instead of a 0% reading.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SoilHealth_ForAnyInitialValue_IsAlwaysClampedBetween0And100(float initialHealth)
    {
        var service = CreateService(out _);
        var tile = new Vector2(5, 5);
        const string location = "Farm";

        service.SetSoilHealth(location, tile, initialHealth);
        float currentHealth = service.GetSoilHealth(location, tile);

        return float.IsNaN(currentHealth) ||
               (currentHealth >= ModConstants.MinSoilHealth &&
                currentHealth <= ModConstants.MaxSoilHealth);
    }

    /// <summary>
    /// Invariant: For any sequence of valid compost applications (+15 per application), the resulting
    /// soil health is always clamped to [0, 100], and each non-clamped application adds exactly +15 (FR-007, FR-009).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CompostApplication_RepeatedApplications_AlwaysBoundedAndExactlyAddsRestorationAmount(
        PositiveInt initialHealthInt,
        PositiveInt applicationCountInt)
    {
        var service = CreateService(out _);
        var tile = new Vector2(12, 18);
        const string location = "Farm";

        float initialHealth = Math.Clamp((float)(initialHealthInt.Get % 101), 0f, 100f);
        int applications = (applicationCountInt.Get % 10) + 1; // 1 to 10 applications

        service.SetSoilHealth(location, tile, initialHealth);
        float expectedHealth = initialHealth;

        for (int i = 0; i < applications; i++)
        {
            float previousHealth = service.GetSoilHealth(location, tile);
            service.UpdateHealth(location, tile, ModConstants.RestorationAmount);
            float actualHealth = service.GetSoilHealth(location, tile);

            expectedHealth = Math.Min(expectedHealth + ModConstants.RestorationAmount, ModConstants.MaxSoilHealth);

            // Health must never exceed 100 or drop below 0
            if (actualHealth < ModConstants.MinSoilHealth || actualHealth > ModConstants.MaxSoilHealth)
            {
                return false;
            }

            // Expected clamped health must match
            if (Math.Abs(actualHealth - expectedHealth) > 0.001f)
            {
                return false;
            }

            // If not clamped, delta must be exactly +15
            if (previousHealth + ModConstants.RestorationAmount <= ModConstants.MaxSoilHealth)
            {
                if (Math.Abs((actualHealth - previousHealth) - ModConstants.RestorationAmount) > 0.001f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Invariant: Applying compost when health is already 100 results in health staying exactly at 100 (FR-007, FR-009).
    /// </summary>
    [Fact]
    public void CompostApplication_WhenHealthIsAtMax_StaysAtMax()
    {
        var service = CreateService(out _);
        var tile = new Vector2(3, 4);
        const string location = "Farm";

        service.SetSoilHealth(location, tile, ModConstants.MaxSoilHealth);
        Assert.Equal(ModConstants.MaxSoilHealth, service.GetSoilHealth(location, tile));

        service.UpdateHealth(location, tile, ModConstants.RestorationAmount);
        Assert.Equal(ModConstants.MaxSoilHealth, service.GetSoilHealth(location, tile));
    }

    /// <summary>
    /// Invariant: Applying compost to an unmeasured tile increases health to
    /// exactly +15 (FR-007). An unmeasured tile reads back as the T135
    /// unknown sentinel; UpdateHealth treats the absent record as the 0
    /// baseline, so the first compost application lands exactly at the
    /// restoration amount.
    /// </summary>
    [Fact]
    public void CompostApplication_FromZeroBaseline_IncreasesToExactlyRestorationAmount()
    {
        var service = CreateService(out _);
        var tile = new Vector2(7, 8);
        const string location = "Farm";

        // The tile was never measured: no record exists, so the query
        // reports the T135 unknown sentinel — not a 0% reading.
        Assert.Equal(float.NaN, service.GetSoilHealth(location, tile));

        service.UpdateHealth(location, tile, ModConstants.RestorationAmount);
        Assert.Equal(ModConstants.RestorationAmount, service.GetSoilHealth(location, tile));
    }
}
