using LivingRoots.Domain;

namespace LivingRoots.Domain.Services;

/// <summary>
/// Validates whether an item qualifies as organic waste for composting.
/// Uses hybrid validation: vanilla category IDs OR custom inclusion tag,
/// with exclusion tag override.
/// Pure domain rule: no SMAPI dependency and no logging — trace
/// diagnostics for the validation outcome are emitted by the
/// application layer that consumes this validator.
/// </summary>
public class OrganicWasteValidator : IOrganicWasteValidator
{
    /// <inheritdoc />
    public bool IsValidOrganicWaste(StardewValley.Item item)
    {
        if (item == null)
        {
            return false;
        }

        // Exclusion tag overrides everything — specific items can be excluded
        if (item.HasContextTag("not_compostable"))
        {
            return false;
        }

        // Custom inclusion tag for mod compatibility
        if (item.HasContextTag("compostable_item"))
        {
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

        return isValid;
    }
}
