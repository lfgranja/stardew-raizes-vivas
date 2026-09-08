using LivingRoots.Domain;

namespace LivingRoots.Tests.Stubs
{
    /// <summary>
    /// Stub implementation of <see cref="ISeasonProvider"/> for unit testing.
    /// Provides a settable <see cref="CurrentSeason"/> so tests can control the simulated season.
    /// </summary>
    public class SeasonProviderStub : ISeasonProvider
    {
        /// <summary>
        /// Gets or sets the current season name.
        /// Defaults to "spring".
        /// </summary>
        public string CurrentSeason { get; set; } = "spring";
    }
}
