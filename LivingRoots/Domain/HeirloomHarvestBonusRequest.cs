using System;

namespace LivingRoots.Domain;

/// <summary>
/// Spec 020: typed farmhand → host request to apply the crioulo harvest bonus
/// (Constitution VI — farm state is host-authoritative). Mirrors
/// <see cref="HeirloomPlantBonusRequest"/> for the harvest-time +1 Healthy-band
/// write; farmhands never mutate shared soil state locally.
/// </summary>
public sealed record HeirloomHarvestBonusRequest(
    string Location,
    int TileX,
    int TileY,
    string DayKey,
    string ItemId)
{
    /// <summary>Creates a request for the host with the current day key.</summary>
    public static HeirloomHarvestBonusRequest Create(string location, int tileX, int tileY, string dayKey, string itemId)
    {
        return new HeirloomHarvestBonusRequest(location, tileX, tileY, dayKey, itemId);
    }
}
