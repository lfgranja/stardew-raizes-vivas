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
    public Farmer CurrentPlayer => Game1.player;

    /// <summary>Gets the player's currently held item, or null if none.</summary>
    public Item? CurrentItem => Game1.player.CurrentItem;
}
