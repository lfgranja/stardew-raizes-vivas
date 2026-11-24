using System.Threading;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests for the ReGen Valley host-authority
/// interaction (A5 — documentation-only).
/// ReGen Valley interactions are first-wins host-authoritative: the first
/// concurrent mutation wins, subsequent mutations are refused, and the
/// system is never last-write-wins.
/// </summary>
/// <remarks>
/// ReGen Valley is documentation-only (A5). These tests verify the
/// host-authority invariant through the <see cref="IHostAtomicLockService"/>
/// seam — the same service the production controller uses for multiplayer
/// contention resolution. Per-case isolation (T280(b) pattern).
/// </remarks>
public class ReGenValleyInvariantsTests
{
    public ReGenValleyInvariantsTests()
    {
        FsCheckFixture.Register();
    }

    // ------------------------------------------------------------------
    // First-wins invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: the first mutation to acquire the lock wins. For any
    /// number of concurrent contenders, exactly one acquires the lock.
    /// </summary>
    [Property(MaxTest = 100)]
    public void FirstWins_ConcurrentContenders_ExactlyOneAcquires(PositiveInt contenders)
    {
        int count = 2 + (contenders.Get % 8);
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(contenders.Get % 64, 0);

        int winners = 0;
        var startGate = new ManualResetEventSlim(false);
        var threads = new Thread[count];

        for (int i = 0; i < count; i++)
        {
            long farmerId = i + 1;
            threads[i] = new Thread(() =>
            {
                startGate.Wait();
                if (lockService.TryAcquireLock("Farm", tile, farmerId))
                {
                    Interlocked.Increment(ref winners);
                }
            });
            threads[i].Start();
        }

        startGate.Set();
        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.Equal(1, winners);
        Assert.True(lockService.IsLocked("Farm", tile));
    }

    // ------------------------------------------------------------------
    // Second mutation refused invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: a second concurrent mutation is refused while the first
    /// holds the lock. The second farmer cannot acquire the lock until the
    /// first releases it.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SecondMutation_WhileLockHeld_IsRefused(PositiveInt arbitraryTileX)
    {
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(arbitraryTileX.Get % 64, 0);

        // First farmer acquires the lock
        Assert.True(lockService.TryAcquireLock("Farm", tile, 1));

        // Act: second farmer tries to acquire
        bool secondAcquired = lockService.TryAcquireLock("Farm", tile, 2);

        // Assert — second mutation is refused
        Assert.False(secondAcquired);
        Assert.True(lockService.IsLocked("Farm", tile));
        Assert.Equal(1, lockService.GetLockHolder("Farm", tile));
    }

    // ------------------------------------------------------------------
    // Never last-write-wins invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: the system is never last-write-wins. If farmer A holds
    /// the lock and farmer B tries to acquire it, B is refused — B's
    /// mutation does not overwrite A's state.
    /// </summary>
    [Property(MaxTest = 100)]
    public void NeverLastWriteWins_SecondFarmerDoesNotOverwrite(PositiveInt arbitraryTileY)
    {
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(0, arbitraryTileY.Get % 64);

        // First farmer acquires and "mutates"
        Assert.True(lockService.TryAcquireLock("Farm", tile, 1));

        // Second farmer attempts concurrent mutation — must be refused
        bool secondAcquired = lockService.TryAcquireLock("Farm", tile, 2);
        Assert.False(secondAcquired);

        // The lock holder is still the first farmer
        Assert.Equal(1, lockService.GetLockHolder("Farm", tile));
    }

    // ------------------------------------------------------------------
    // Release and re-acquire invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: after the lock holder releases the lock, a subsequent
    /// mutation can acquire it. The lock is not permanently held.
    /// </summary>
    [Property(MaxTest = 100)]
    public void AfterRelease_LockCanBeReacquired(PositiveInt arbitrarySalt)
    {
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(arbitrarySalt.Get % 64, 0);

        // First farmer acquires and releases
        Assert.True(lockService.TryAcquireLock("Farm", tile, 1));
        lockService.ReleaseLock("Farm", tile, 1);
        Assert.False(lockService.IsLocked("Farm", tile));

        // Act: second farmer acquires
        bool secondAcquired = lockService.TryAcquireLock("Farm", tile, 2);

        // Assert — the lock was re-acquired, not permanently held
        Assert.True(secondAcquired);
        Assert.True(lockService.IsLocked("Farm", tile));
        Assert.Equal(2, lockService.GetLockHolder("Farm", tile));
    }

    // ------------------------------------------------------------------
    // Same-farmer re-entrancy invariant
    // ------------------------------------------------------------------

    /// <summary>
    /// Invariant: the same farmer can re-acquire a lock they already hold
    /// (idempotent re-entrancy). This is not a second concurrent mutation.
    /// </summary>
    [Property(MaxTest = 100)]
    public void SameFarmer_ReEntrantAcquire_Succeeds(PositiveInt arbitrarySalt)
    {
        var lockService = new HostAtomicLockService();
        var tile = new Vector2(arbitrarySalt.Get % 64, 0);

        // First farmer acquires
        Assert.True(lockService.TryAcquireLock("Farm", tile, 1));

        // Act: same farmer re-acquires (re-entrant)
        bool reAcquired = lockService.TryAcquireLock("Farm", tile, 1);

        // Assert — re-entrant acquire succeeds
        Assert.True(reAcquired);
        Assert.Equal(1, lockService.GetLockHolder("Farm", tile));
    }
}
