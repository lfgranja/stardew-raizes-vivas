using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Production implementation of <see cref="IPlayerProvider"/> that wraps <see cref="Game1"/>.
/// </summary>
public class PlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public Farmer CurrentPlayer => Game1.player;

    /// <inheritdoc />
    public Item? CurrentItem => Game1.player?.CurrentItem;
}
