using LivingRoots.Domain;

namespace LivingRoots.Tests.Stubs
{
    /// <summary>
    /// Stub implementation of <see cref="ITimeProvider"/> for unit testing.
    /// Provides a settable <see cref="TotalDays"/> so tests can control the simulated day count.
    /// </summary>
    public class TimeProviderStub : ITimeProvider
    {
        /// <summary>
        /// Gets or sets the total number of days elapsed in the simulated save.
        /// Defaults to 1 (first day of the game).
        /// </summary>
        public int TotalDays { get; set; } = 1;
    }
}
