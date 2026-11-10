using System;

namespace LivingRoots.Domain;

/// <summary>
/// Spec 020: typed farmhand → host request to apply the crioulo plant bonus
/// (Constitution VI — farm state is host-authoritative). Farmhands never
/// mutate shared soil state locally; the host validates, applies, and
/// broadcasts through the existing <see cref="SoilHealthSyncMessage"/> path.
/// Session-scoped messaging; duplicates are suppressed host-side by a bounded
/// (location, tile, dayKey) dedupe set.
/// </summary>
public sealed record HeirloomPlantBonusRequest(
    string Location,
    int TileX,
    int TileY,
    string DayKey,
    string ItemId)
{
    /// <summary>Creates a request for the host with the current day key.</summary>
    public static HeirloomPlantBonusRequest Create(string location, int tileX, int tileY, string dayKey, string itemId)
    {
        return new HeirloomPlantBonusRequest(location, tileX, tileY, dayKey, itemId);
    }
}
