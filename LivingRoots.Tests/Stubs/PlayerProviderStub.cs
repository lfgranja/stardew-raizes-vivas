using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Tests.Stubs;

/// <summary>
/// Stub implementation of IPlayerProvider for player-dependent tests.
/// Allows tests to control the current player and held item.
/// </summary>
public class PlayerProviderStub : IPlayerProvider
{
    private Farmer _currentPlayer = null!;
    private Item? _currentItem;

    /// <summary>Gets or sets the current player for testing.</summary>
    public Farmer CurrentPlayer
    {
        get => _currentPlayer;
        set => _currentPlayer = value;
    }

    /// <summary>Gets or sets the player's current held item for testing.</summary>
    public Item? CurrentItem
    {
        get => _currentItem;
        set => _currentItem = value;
    }
}
