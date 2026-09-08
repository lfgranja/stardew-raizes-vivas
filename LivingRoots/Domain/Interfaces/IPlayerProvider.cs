using StardewValley;

namespace LivingRoots.Domain;

/// <summary>
/// Provides access to the current player and their held item.
/// Extracted from Game1.player for testability.
/// </summary>
public interface IPlayerProvider
{
    /// <summary>Gets the current player (Farmer).</summary>
    Farmer CurrentPlayer { get; }

    /// <summary>Gets the player's currently held item, or null if none.</summary>
    Item? CurrentItem { get; }
}
