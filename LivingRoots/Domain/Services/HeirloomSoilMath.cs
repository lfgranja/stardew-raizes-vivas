using System;

namespace LivingRoots.Domain.Services;

/// <summary>
/// Pure soil-effect rules for the crioulo ("Sementes Crioulas") surface — spec 020.
/// No SMAPI dependency, no logging, no I/O. The Harmony postfixes stay thin glue
/// over these rules; measurement/bounds conventions mirror spec 014/017:
/// NaN (unmeasured) resolves to a neutral baseline of MaxSoilHealth (100);
/// writes clamp into [MinSoilHealth, MaxSoilHealth]; results are always measured.
/// </summary>
public static class HeirloomSoilMath
{
    /// <summary>Resolves the neutral baseline for an unmeasured/NaN reading.</summary>
    public static float ResolveBaseline(float measured)
    {
        return float.IsNaN(measured) ? ModConstants.MaxSoilHealth : measured;
    }

    /// <summary>True when the measured reading is in the Healthy band (>= ModerateHealthyBandEdge, 71). Unmeasured readings are not Healthy.</summary>
    public static bool IsHealthyBand(float measured)
    {
        return !float.IsNaN(measured) && measured >= ModConstants.ModerateHealthyBandEdge;
    }

    /// <summary>Bounded +HeirloomPlantBonus write; NaN baseline resolves to 100; clamps into [0,100].</summary>
    public static float Plant(float measured)
    {
        var baseline = ResolveBaseline(measured);
        return Clamp(baseline + ModConstants.HeirloomPlantBonus);
    }

    /// <summary>
    /// Flat +HeirloomHarvestBonus write, applied only in the Healthy band.
    /// Returns true when a bounded write was computed; false when the bonus is not applicable (below band or NaN) — in which case <paramref name="result"/> equals <paramref name="measured"/>.
    /// </summary>
    public static bool Harvest(float measured, out float result)
    {
        if (!IsHealthyBand(measured))
        {
            result = measured;
            return false;
        }

        result = Clamp(measured + ModConstants.HeirloomHarvestBonus);
        return true;
    }

    private static float Clamp(float value)
    {
        if (float.IsNaN(value))
        {
            return ModConstants.MaxSoilHealth; // NaN never escapes the bounded write (measured fallback is 100).
        }

        if (value < ModConstants.MinSoilHealth)
        {
            return ModConstants.MinSoilHealth;
        }

        if (value > ModConstants.MaxSoilHealth)
        {
            return ModConstants.MaxSoilHealth;
        }

        return value;
    }
}
