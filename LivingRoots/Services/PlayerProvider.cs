using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of IPlayerProvider.
/// Wraps Game1.player for player-dependent logic.
/// </summary>
public class PlayerProvider : IPlayerProvider
{
    /// <summary>Gets the current player (Farmer).</summary>
#pragma warning disable S2325 // S2325: implements IPlayerProvider; cannot be static
    public Farmer CurrentPlayer => Game1.player;

    /// <summary>Gets the player's currently held item, or null if none.</summary>
    public Item? CurrentItem => Game1.player.CurrentItem;
#pragma warning restore S2325
}
