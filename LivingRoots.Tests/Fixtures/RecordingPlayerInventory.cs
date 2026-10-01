using System.Collections.Generic;
using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Tests.Fixtures;

/// <summary>
/// Records the items handed out instead of granting them to a real <c>Farmer</c>, which cannot be
/// constructed in a unit-test host.
/// </summary>
/// <remarks>
/// A real inventory is impossible to substitute: <c>Farmer</c>'s constructor dereferences sprite
/// data that only exists once the game has booted, and <c>addItemToInventoryBool</c> is not
/// virtual, so Moq rejects it. This fake satisfies <see cref="IPlayerInventory"/> and lets a test
/// assert exactly what the service tried to grant.
/// </remarks>
public sealed class RecordingPlayerInventory : IPlayerInventory
{
    private readonly List<Item> _added = new();

    /// <summary>Items passed to <see cref="TryAddItem"/>, in call order.</summary>
    public IReadOnlyList<Item> AddedItems => _added;

    /// <summary>When false, <see cref="TryAddItem"/> refuses every item, simulating a full inventory.</summary>
    public bool AcceptsItems { get; set; } = true;

    /// <inheritdoc />
    public bool TryAddItem(Item item)
    {
        if (!AcceptsItems)
        {
            return false;
        }

        _added.Add(item);
        return true;
    }
}
