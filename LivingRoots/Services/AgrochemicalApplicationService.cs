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
        // T099 (FR-008/FR-006): an overlong location name cannot be persisted or synchronized
        // (the persistence layer refuses it), so it is an invalid target — never a 0f floor
        // reading that falls into the soil-at-minimum branch. Refuse before any soil read or write.
        if (location == null ||
            string.IsNullOrWhiteSpace(location.Name) ||
            location.Name.Length > ModConstants.MaxLocationNameLength)
        {
            return CreateInvalidTargetResult();
        }

        // T075: an unmappable tile is an invalid target, never a malformed query that
        // the soil service answers 0f for. That 0f sentinel is indistinguishable from a
        // measured zero on the unknown-or-floor axis and would fall into the
        // soil-at-minimum branch. Refuse before any soil read or write; the NaN-tile
        // case must never reach the soil-at-minimum branch.
        if (!TryMapTile(tile, out var tilePoint))
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
        // T101 (documentation only): baselineUsed - postPenalty can differ from the requested
        // penalty by ~1 ulp (the subtraction is not exact in all cases); tests tolerate this via
        // matcher comparisons. The formula is kept as-is; do not "fix" it by switching to rounded
        // arithmetic.
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

        if (!_soilHealthService.TrySetSoilHealth(location.Name, tilePoint, postPenalty))
        {
            // 017 T097: the commit seam refused the measured write (capacity limit or a tile
            // the soil service cannot persist). The dedicated commit-drop refusal class is
            // reported with the commit-failed key — never the action-conflict key, never
            // success. RefusedConflict stays reserved for controller lock-contention; the
            // service no longer reports it for capacity. Baseline/penalty data is preserved
            // honestly — it IS clamped if clamped — and success is never claimed.
            return new AgrochemicalApplicationResult(
                AgrochemicalApplicationOutcome.RefusedCommitFailed,
                ModConstants.TranslationKeyAgrochemicalCommitFailed,
                baselineUsed,
                postPenalty,
                requestedPenalty,
                appliedPenalty,
                wasClamped,
                baselineWasUnknown);
        }

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

    private static bool TryMapTile(Vector2 tile, out Point tilePoint)
    {
        tilePoint = default;
        if (!float.IsFinite(tile.X) || !float.IsFinite(tile.Y))
        {
            return false;
        }

        var floorX = MathF.Floor(tile.X);
        var floorY = MathF.Floor(tile.Y);
        if (floorX > int.MaxValue || floorX < int.MinValue || floorY > int.MaxValue || floorY < int.MinValue)
        {
            return false;
        }

        // The persisted grid is bounded (IsValidTile enforces the same magnitude limit);
        // a finite tile outside it cannot be stored or synchronized.
        if (MathF.Abs(floorX) > ModConstants.MaxAbsoluteTileCoordinate ||
            MathF.Abs(floorY) > ModConstants.MaxAbsoluteTileCoordinate)
        {
            return false;
        }

        tilePoint = new Point((int)floorX, (int)floorY);
        return true;
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
        // T074: the refusal never read the soil, so it carries no baseline and
        // resolves no post penalty — NaN sentinels document "no value was produced",
        // and WasClamped is false because nothing was computed, let alone clamped.
        // RequestedPenalty still reports what was asked for, which is known.
        return new AgrochemicalApplicationResult(
            AgrochemicalApplicationOutcome.RefusedInvalidTarget,
            ModConstants.TranslationKeyAgrochemicalInvalidTarget,
            float.NaN,
            float.NaN,
            ModConstants.AgrochemicalSoilPenalty,
            float.NaN,
            false,
            false);
    }
}
