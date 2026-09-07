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

    /// <inheritdoc />
    public bool IsValidOrganicWaste(Item item)
    {
        if (item == null)
        {
            _monitor.Log("OrganicWasteValidator: null item rejected.", LogLevel.Trace);
            return false;
        }

        // Exclusion tag overrides everything — specific items can be excluded
        if (item.HasContextTag("not_compostable"))
        {
            _monitor.Log($"OrganicWasteValidator: {item.QualifiedItemId} rejected (not_compostable tag).", LogLevel.Trace);
            return false;
        }

        // Custom inclusion tag for mod compatibility
        if (item.HasContextTag("compostable_item"))
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
