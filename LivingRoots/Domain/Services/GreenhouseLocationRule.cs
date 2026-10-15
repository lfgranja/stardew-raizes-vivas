namespace LivingRoots.Domain.Services;

/// <summary>
/// T105/T197 (FR-002): the single definition of a greenhouse-like location.
/// </summary>
/// <remarks>
/// <para>
/// FR-002 names "the Greenhouse" as a valid place to put a composting bin and
/// apply compost, but a modded greenhouse — a custom greenhouse map or an
/// added greenhouse variant — carries its own name. Two gates must agree on
/// which locations qualify: <c>ModController.TryPlaceBin</c> (placement, T105)
/// and <c>CompostApplicationService.TryApplyCompost</c> (application, T197).
/// They previously did not, so a bin could be placed in a modded greenhouse
/// where its compost could never be applied.
/// </para>
/// <para>
/// A location is greenhouse-like when its name contains "Greenhouse" under a
/// case-insensitive comparison, so the literal Greenhouse and any modded
/// greenhouse map or variant both qualify. A name without the fragment is not
/// a greenhouse, so every other non-farm interior stays refused by both gates.
/// </para>
/// <para>
/// The rule lives here, in the domain, because it is a business rule both the
/// controller and the application service must share — never restated at a call
/// site, so the two gates cannot drift apart again.
/// </para>
/// </remarks>
public static class GreenhouseLocationRule
{
    /// <summary>The case-insensitive name fragment that marks a greenhouse.</summary>
    private const string GreenhouseFragment = "Greenhouse";

    /// <summary>
    /// Determines whether a location is greenhouse-like (T105, FR-002): its name
    /// contains "Greenhouse" under a case-insensitive comparison. A name without
    /// the fragment — or no name at all — is not a greenhouse.
    /// </summary>
    /// <param name="location">The location to classify.</param>
    /// <returns>True when the location is greenhouse-like.</returns>
    public static bool IsGreenhouseLike(StardewValley.GameLocation location)
    {
        return location.Name != null &&
            location.Name.Contains(GreenhouseFragment, StringComparison.OrdinalIgnoreCase);
    }
}
