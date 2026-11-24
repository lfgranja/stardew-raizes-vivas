using System;
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
public sealed class RecordingPlayerInventory : IPlayerInventory, IPlayerInventoryCapacity
{
    private readonly List<Item> _added = new();
    private int _tryAddCalls;

    /// <summary>Items passed to <see cref="TryAddItem"/>, in call order.</summary>
    public IReadOnlyList<Item> AddedItems => _added;

    /// <summary>When false, <see cref="TryAddItem"/> refuses every item, simulating a full inventory.</summary>
    public bool AcceptsItems { get; set; } = true;

    /// <summary>When set, <see cref="TryAddItem"/> refuses on this 1-based call index (partial-add simulation).</summary>
    public int RefuseOnCall { get; set; }

    /// <summary>Configured dry-run capacity result.</summary>
    public bool HasCapacityResult { get; set; } = true;

    /// <summary>Predicate override for dry-run capacity; null uses <see cref="HasCapacityResult"/>.</summary>
    public Func<Item, int, bool>? CapacityPredicate { get; set; }

    /// <inheritdoc />
    public bool TryAddItem(Item item)
    {
        _tryAddCalls++;
        if (!AcceptsItems || (_tryAddCalls == RefuseOnCall && RefuseOnCall > 0))
        {
            return false;
        }

        _added.Add(item);
        return true;
    }

    /// <inheritdoc />
    public bool TryRemoveItem(Item item)
    {
        return _added.Remove(item);
    }

    public bool HasCapacity(Item item, int count)
    {
        if (CapacityPredicate != null)
        {
            return CapacityPredicate(item, count);
        }

        return HasCapacityResult;
    }

    /// <summary>Resets the call counter and granted list.</summary>
    public void Reset()
    {
        _added.Clear();
        _tryAddCalls = 0;
        AcceptsItems = true;
        RefuseOnCall = 0;
        HasCapacityResult = true;
        CapacityPredicate = null;
    }
}
