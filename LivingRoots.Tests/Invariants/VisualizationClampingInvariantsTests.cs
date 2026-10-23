using System;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain.Visualization;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for health-value clamping in the
/// visualization color path. Validates Task T110 [P] [US1] under
/// Feature 001 (Soil Health Visualization) and FR-008, FR-014, SC-007.
/// Pins the data-model.md SoilHealthTile/ColorMapping validation rules:
/// HealthValue clamped to [0, 100] per ModConstants.MinSoilHealth/MaxSoilHealth,
/// and NaN/Infinity values default to the Unknown category with HealthValue = 0.
/// </summary>
public class VisualizationClampingInvariantsTests
{
    private readonly ColorInterpolationService _service = new();

    /// <summary>
    /// Invariant: for any finite health value, the color path resolves the
    /// clamped value — an out-of-range input renders identically to its
    /// clamped counterpart in [0, 100] (FR-008, SC-007).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetColorForHealth_ForAnyFiniteHealth_ResolvesClampedValue(float health)
    {
        if (float.IsNaN(health) || float.IsInfinity(health))
        {
            return true; // non-finite inputs follow the Unknown normalization rule below
        }

        float clamped = Math.Clamp(health, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);

        return _service.GetColorForHealth(health) == _service.GetColorForHealth(clamped);
    }

    /// <summary>
    /// Invariant: for any finite health value, the category path resolves the
    /// clamped value — a producer value outside 0–100 clamps into the boundary
    /// category (0 → Poor, 100 → Healthy) per spec acceptance scenario US1-S7
    /// (FR-008, SC-007).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetCategoryForHealth_ForAnyFiniteHealth_ResolvesClampedValue(float health)
    {
        if (float.IsNaN(health) || float.IsInfinity(health))
        {
            return true; // non-finite inputs follow the Unknown normalization rule below
        }

        float clamped = Math.Clamp(health, ModConstants.MinSoilHealth, ModConstants.MaxSoilHealth);

        return _service.GetCategoryForHealth(health) == _service.GetCategoryForHealth(clamped);
    }

    /// <summary>
    /// Invariant: NaN/Infinity health values map to the Unknown category —
    /// never to a real category — for any generated float (FR-014).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetCategoryForHealth_ForAnyNonFiniteHealth_MapsToUnknown(float health)
    {
        if (!float.IsNaN(health) && !float.IsInfinity(health))
        {
            return true; // finite inputs are governed by the clamping invariant above
        }

        return _service.GetCategoryForHealth(health) == HealthCategory.Unknown;
    }

    /// <summary>
    /// Invariant: NaN/Infinity health values map to the Unknown color at full
    /// opacity (FR-014). Per data-model.md the tile's HealthValue normalizes
    /// to 0 in the Unknown mapping — distinct from health 0 itself, which is a
    /// valid Poor-category value (FR-008, FR-014, SC-007).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool GetColorForHealth_ForAnyNonFiniteHealth_MapsToUnknownColor(float health)
    {
        if (!float.IsNaN(health) && !float.IsInfinity(health))
        {
            return true; // finite inputs are governed by the clamping invariant above
        }

        Color color = _service.GetColorForHealth(health);

        return color.R == ModConstants.UnknownColor.R
            && color.G == ModConstants.UnknownColor.G
            && color.B == ModConstants.UnknownColor.B
            && color.A == (byte)255;
    }

    /// <summary>
    /// Boundary fact: a health value below the valid range clamps to 0 and
    /// resolves to the Poor category, not Unknown (FR-008, SC-007).
    /// </summary>
    [Fact]
    public void GetCategoryForHealth_BelowZero_ClampsToPoorCategory()
    {
        Assert.Equal(HealthCategory.Poor, _service.GetCategoryForHealth(-1f));
        Assert.Equal(HealthCategory.Poor, _service.GetCategoryForHealth(float.MinValue));
    }

    /// <summary>
    /// Boundary fact: a health value above the valid range clamps to 100 and
    /// resolves to the Healthy category, not Unknown (FR-008, SC-007).
    /// </summary>
    [Fact]
    public void GetCategoryForHealth_AboveHundred_ClampsToHealthyCategory()
    {
        Assert.Equal(HealthCategory.Healthy, _service.GetCategoryForHealth(100.001f));
        Assert.Equal(HealthCategory.Healthy, _service.GetCategoryForHealth(float.MaxValue));
    }

    /// <summary>
    /// Boundary fact: the color path already clamps below-range values to the
    /// health-0 color (FR-008, SC-007).
    /// </summary>
    [Fact]
    public void GetColorForHealth_BelowZero_MatchesColorAtZero()
    {
        Color atZero = _service.GetColorForHealth(ModConstants.MinSoilHealth);

        Assert.Equal(atZero, _service.GetColorForHealth(-1f));
        Assert.Equal(atZero, _service.GetColorForHealth(float.MinValue));
    }

    /// <summary>
    /// Boundary fact: the color path already clamps above-range values to the
    /// health-100 color (FR-008, SC-007).
    /// </summary>
    [Fact]
    public void GetColorForHealth_AboveHundred_MatchesColorAtHundred()
    {
        Color atHundred = _service.GetColorForHealth(ModConstants.MaxSoilHealth);

        Assert.Equal(atHundred, _service.GetColorForHealth(100.001f));
        Assert.Equal(atHundred, _service.GetColorForHealth(float.MaxValue));
    }

    /// <summary>
    /// Boundary fact: NaN/Infinity health values normalize to the Unknown
    /// mapping per data-model.md ("NaN/Infinity values default to Unknown
    /// category with HealthValue = 0") (FR-014, SC-007).
    /// </summary>
    [Fact]
    public void NonFiniteHealth_NormalizesToUnknownCategoryWithZeroHealthValue()
    {
        foreach (float nonFinite in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.Equal(HealthCategory.Unknown, _service.GetCategoryForHealth(nonFinite));

            Color color = _service.GetColorForHealth(nonFinite);
            Assert.Equal(ModConstants.UnknownColor.R, color.R);
            Assert.Equal(ModConstants.UnknownColor.G, color.G);
            Assert.Equal(ModConstants.UnknownColor.B, color.B);
        }
    }
}
