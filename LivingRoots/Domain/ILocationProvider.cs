namespace LivingRoots.Domain
{
    /// <summary>
    /// Resolves a game location by name.
    /// Exists so services that walk a location's terrain features can be tested without
    /// a booted game, matching the extraction pattern already used for
    /// <see cref="ITimeProvider"/>, <see cref="ISeasonProvider"/> and <see cref="IPlayerProvider"/>.
    /// </summary>
    public interface ILocationProvider
    {
        /// <summary>
        /// Gets the location with the given name.
        /// </summary>
        /// <param name="locationName">Location name, as persisted in soil health keys.</param>
        /// <returns>The matching location, or null when the name is unknown.</returns>
        StardewValley.GameLocation? GetLocationByName(string locationName);
    }
}
