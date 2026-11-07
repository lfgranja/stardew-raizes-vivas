using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using Microsoft.Xna.Framework;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Applies the 017 Joja/agrochemical penalty to one soil tile.
/// </summary>
/// <remarks>
/// The service owns only the domain mutation and its explicit result. It never
/// consumes the held item, displays feedback, or broadcasts multiplayer state;
/// those effects are controller-owned (see the 017 application-service contract).
/// </remarks>
public class AgrochemicalApplicationService(ISoilHealthService soilHealthService) : IAgrochemicalApplicationService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));

    /// <inheritdoc />
    public AgrochemicalApplicationResult Apply(GameLocation location, Vector2 tile)
    {
        if (location == null || string.IsNullOrWhiteSpace(location.Name))
        {
            return CreateInvalidTargetResult();
        }

        var currentHealth = _soilHealthService.GetSoilHealth(location.Name, tile);
        var baselineWasUnknown = float.IsNaN(currentHealth);
        var baselineUsed = ResolveBaseline(currentHealth, baselineWasUnknown);

        var requestedPenalty = ModConstants.AgrochemicalSoilPenalty;
        var postPenalty = Math.Clamp(
            baselineUsed - requestedPenalty,
            ModConstants.MinSoilHealth,
            ModConstants.MaxSoilHealth);
        var appliedPenalty = baselineUsed - postPenalty;
        var wasClamped = appliedPenalty < requestedPenalty;

        if (baselineUsed <= ModConstants.MinSoilHealth)
        {
            return new AgrochemicalApplicationResult(
                AgrochemicalApplicationOutcome.RefusedSoilAtMinimum,
                ModConstants.TranslationKeyAgrochemicalSoilAtMinimum,
                baselineUsed,
                baselineUsed,
                requestedPenalty,
                appliedPenalty,
                true,
                baselineWasUnknown);
        }

        _soilHealthService.SetSoilHealth(location.Name, tile, postPenalty);

        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.Success,
            SelectSuccessMessageKey(baselineWasUnknown, wasClamped),
            baselineUsed,
            postPenalty,
            requestedPenalty,
            appliedPenalty,
            wasClamped,
            baselineWasUnknown);
    }

    private static float ResolveBaseline(float currentHealth, bool baselineWasUnknown)
    {
        if (baselineWasUnknown)
        {
            return ModConstants.MaxSoilHealth;
        }

        return Math.Clamp(
            currentHealth,
            ModConstants.MinSoilHealth,
            ModConstants.MaxSoilHealth);
    }

    private static string SelectSuccessMessageKey(bool baselineWasUnknown, bool wasClamped)
    {
        if (baselineWasUnknown)
        {
            return ModConstants.TranslationKeyAgrochemicalAppliedUnknownBaseline;
        }

        if (wasClamped)
        {
            return ModConstants.TranslationKeyAgrochemicalAppliedClamped;
        }

        return ModConstants.TranslationKeyAgrochemicalApplied;
    }

    private static AgrochemicalApplicationResult CreateInvalidTargetResult()
    {
        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.RefusedInvalidTarget,
            ModConstants.TranslationKeyAgrochemicalInvalidTarget,
            0f,
            0f,
            ModConstants.AgrochemicalSoilPenalty,
            0f,
            true,
            false);
    }
}
