using LivingRoots.Domain;
using StardewValley;

namespace LivingRoots.Services;

/// <summary>
/// Resolves locations through the live <c>Game1.locations</c> collection.
/// </summary>
public class LocationProvider : ILocationProvider
{
    /// <inheritdoc />
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
}
