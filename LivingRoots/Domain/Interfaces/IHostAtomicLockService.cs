using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Interfaces
{
    /// <summary>
    /// Contract for host-authoritative atomic coordinate and machine action resolution.
    /// Manages transient mutual exclusion locks on tile coordinates to prevent race conditions in multiplayer.
    /// </summary>
    public interface IHostAtomicLockService
    {
        /// <summary>
        /// Attempts to atomically acquire a lock for a specific coordinate in a location for the given farmer.
        /// First-wins semantics: returns true if the lock was acquired, false if already locked.
        /// </summary>
        /// <param name="locationName">The unique name of the game location.</param>
        /// <param name="coordinate">The tile coordinate to lock.</param>
        /// <param name="farmerId">The unique multiplayer ID of the farmer requesting the lock.</param>
        /// <returns>True if the lock was successfully acquired; otherwise, false.</returns>
        bool TryAcquireLock(string locationName, Vector2 coordinate, long farmerId);

        /// <summary>
        /// Releases a previously acquired lock for a coordinate if held by the specified farmer.
        /// Idempotent and safe: if not held by this farmer or already unlocked, this operation is a no-op.
        /// </summary>
        /// <param name="locationName">The unique name of the game location.</param>
        /// <param name="coordinate">The tile coordinate to unlock.</param>
        /// <param name="farmerId">The unique multiplayer ID of the farmer releasing the lock.</param>
        void ReleaseLock(string locationName, Vector2 coordinate, long farmerId);

        /// <summary>
        /// Checks whether a lock is currently active for the specified coordinate in a location.
        /// </summary>
        /// <param name="locationName">The unique name of the game location.</param>
        /// <param name="coordinate">The tile coordinate to inspect.</param>
        /// <returns>True if the coordinate is locked; otherwise, false.</returns>
        bool IsLocked(string locationName, Vector2 coordinate);

        /// <summary>
        /// Gets the farmer ID of the current lock holder for the specified coordinate, if locked.
        /// </summary>
        /// <param name="locationName">The unique name of the game location.</param>
        /// <param name="coordinate">The tile coordinate to inspect.</param>
        /// <returns>The farmer ID holding the lock, or null if unlocked.</returns>
        long? GetLockHolder(string locationName, Vector2 coordinate);
    }
}
