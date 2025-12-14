using System;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Provides the save ID for data persistence operations.
    /// This abstraction allows for testability by enabling mocking in unit tests.
    /// </summary>
    public interface ISaveIdProvider
    {
        /// <summary>
        /// Gets the save ID for data persistence.
        /// </summary>
        /// <returns>The save ID or null if unavailable</returns>
        string? GetSaveId();
    }
}