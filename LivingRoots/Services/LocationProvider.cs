using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Resolves locations through the live <c>Game1.locations</c> collection.
/// </summary>
public class LocationProvider : ILocationProvider
{
    /// <inheritdoc />
#pragma warning disable S2325 // S2325: implements ILocationProvider; cannot be static
    public GameLocation? GetLocationByName(string locationName)
    {
        foreach (var location in Game1.locations)
        {
            if (location.Name == locationName)
            {
                return location;
            }
        }

        return null;
    }
#pragma warning restore S2325
}
