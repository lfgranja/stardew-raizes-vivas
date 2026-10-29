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

        // Check if item has any category tag
        bool hasSeedsTag = item.HasContextTag("category_seeds");
        bool hasVegetableTag = item.HasContextTag("category_vegetable");
        bool hasFruitsTag = item.HasContextTag("category_fruits");
        bool hasFlowersTag = item.HasContextTag("category_flowers");
        bool hasGreensTag = item.HasContextTag("category_greens");

        bool hasAnyCategoryTag = hasSeedsTag || hasVegetableTag || hasFruitsTag || hasFlowersTag || hasGreensTag;

        bool categoryQualifies = item.Category == -74 || item.Category == -75 || item.Category == -79 || item.Category == -80 || item.Category == -81;

        if (hasAnyCategoryTag)
        {
            // Accept if any matching tag-category pair exists
            if ((hasSeedsTag && item.Category == -74) ||
                (hasVegetableTag && item.Category == -75) ||
                (hasFruitsTag && item.Category == -79) ||
                (hasFlowersTag && item.Category == -80) ||
                (hasGreensTag && item.Category == -81))
            {
                return true;
            }
            return false;
        }

        // No category tags - accept if category is in qualifying set
        return categoryQualifies;
    }
}
