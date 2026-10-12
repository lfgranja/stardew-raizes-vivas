using LivingRoots.Domain;
using StardewModdingAPI;
using StardewValley;

namespace LivingRoots.Domain.Services;

/// <summary>
/// Validates whether an item qualifies as organic waste for composting.
/// Uses hybrid validation: vanilla category IDs OR custom inclusion tag,
/// with exclusion tag override.
/// </summary>
/// <param name="monitor">Logger for trace diagnostics.</param>
public class OrganicWasteValidator(IMonitor monitor) : IOrganicWasteValidator
{
    private readonly IMonitor _monitor = monitor;

    /// <summary>Context tag an item carries to opt into composting.</summary>
    public const string CompostableItemTag = "compostable_item";

    /// <summary>Context tag that overrides every inclusion rule and excludes the item.</summary>
    public const string NotCompostableTag = "not_compostable";

    /// <summary>
    /// T133 (FR-003): the tag rule the native <c>Data/Machines</c> triggers use —
    /// inclusion by <see cref="CompostableItemTag"/> OR vanilla category, with
    /// <see cref="NotCompostableTag"/> overriding everything. Derived from the
    /// validator's own constants, never restated, so the native machine path and
    /// the mod's <see cref="IsValidOrganicWaste"/> can never drift.
    /// </summary>
    public static IReadOnlyList<string> MachineWasteTags { get; } = new[] { CompostableItemTag, "!" + NotCompostableTag };

    /// <summary>
    /// T120 (FR-003): the tag-rule decision for an item, asserted to match
    /// <see cref="IsValidOrganicWaste"/> exactly. This is the parity seam the
    /// Data/Machines RequiredTags are derived from, so drift between the native
    /// machine filter and the validator surfaces here, never in gameplay.
    /// </summary>
    /// <param name="item">Item to evaluate.</param>
    /// <returns>True when the tag rule accepts the item.</returns>
    public static bool MatchesMachineTagRule(Item item)
    {
        if (item == null)
        {
            return false;
        }

        if (item.HasContextTag(NotCompostableTag))
        {
            return false;
        }

        if (item.HasContextTag(CompostableItemTag))
        {
            return true;
        }

        return item.Category switch
        {
            -74 => true,
            -75 => true,
            -79 => true,
            -80 => true,
            -81 => true,
            _ => false
        };
    }

    /// <inheritdoc />
    public bool IsValidOrganicWaste(Item item)
    {
        if (item == null)
        {
            _monitor.Log("OrganicWasteValidator: null item rejected.", LogLevel.Trace);
            return false;
        }

        // Exclusion tag overrides everything — specific items can be excluded
        if (item.HasContextTag(NotCompostableTag))
        {
            _monitor.Log($"OrganicWasteValidator: {item.QualifiedItemId} rejected (not_compostable tag).", LogLevel.Trace);
            return false;
        }

        // Custom inclusion tag for mod compatibility
        if (item.HasContextTag(CompostableItemTag))
        {
            _monitor.Log($"OrganicWasteValidator: {item.QualifiedItemId} accepted (compostable_item tag).", LogLevel.Trace);
            return true;
        }

        // Vanilla category-based validation
        var isValid = item.Category switch
        {
            -74 => true,  // Seeds (wild seeds, tree seeds)
            -75 => true,  // Vegetables (crop produce)
            -79 => true,  // Fruits (fruit produce)
            -80 => true,  // Flowers
            -81 => true,  // Forage/Greens
            _ => false
        };

        _monitor.Log($"OrganicWasteValidator: {item.QualifiedItemId} (category {item.Category}) {(isValid ? "accepted" : "rejected")}.", LogLevel.Trace);
        return isValid;
    }
}
