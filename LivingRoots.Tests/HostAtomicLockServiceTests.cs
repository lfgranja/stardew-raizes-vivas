using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// Unit and concurrency tests for <see cref="HostAtomicLockService"/> verifying first-wins semantics,
/// release semantics, coordinate/location isolation, and thread-safe lock resolution (FR-019).
/// </summary>
public class HostAtomicLockServiceTests
{
    private readonly IHostAtomicLockService _lockService;

    public HostAtomicLockServiceTests()
    {
        _lockService = new HostAtomicLockService();
    }

    /// <summary>
    /// Verifies first-wins semantics: when farmer A acquires a lock at (X, Y),
    /// a subsequent attempt by farmer B returns false and farmer A remains the holder.
    /// </summary>
    [Fact]
    public void TryAcquireLock_FirstWins_SecondFarmerRefused()
    {
        // Arrange
        const string locationName = "Farm";
        var coordinate = new Vector2(5, 5);
        const long farmerA = 1001L;
        const long farmerB = 1002L;

        // Act
        var acquiredByA = _lockService.TryAcquireLock(locationName, coordinate, farmerA);
        var acquiredByB = _lockService.TryAcquireLock(locationName, coordinate, farmerB);

        // Assert
        Assert.True(acquiredByA, "Farmer A should successfully acquire an unlocked tile.");
        Assert.False(acquiredByB, "Farmer B should be refused when tile is already locked by Farmer A.");
        Assert.True(_lockService.IsLocked(locationName, coordinate));
        Assert.Equal(farmerA, _lockService.GetLockHolder(locationName, coordinate));
    }

    /// <summary>
    /// Verifies release semantics: once farmer A releases their lock,
    /// farmer B can subsequently acquire the lock.
    /// </summary>
    [Fact]
    public void ReleaseLock_ByHoldingFarmer_AllowsSubsequentAcquisition()
    {
        // Arrange
        const string locationName = "Farm";
        var coordinate = new Vector2(10, 12);
        const long farmerA = 1001L;
        const long farmerB = 1002L;

        _lockService.TryAcquireLock(locationName, coordinate, farmerA);
        Assert.True(_lockService.IsLocked(locationName, coordinate));

        // Act: Farmer A releases the lock
        _lockService.ReleaseLock(locationName, coordinate, farmerA);

        // Assert: tile is unlocked
        Assert.False(_lockService.IsLocked(locationName, coordinate));
        Assert.Null(_lockService.GetLockHolder(locationName, coordinate));

        // Act: Farmer B acquires the now-unlocked tile
        var acquiredByB = _lockService.TryAcquireLock(locationName, coordinate, farmerB);

        // Assert: Farmer B now holds the lock
        Assert.True(acquiredByB, "Farmer B should be able to acquire the tile after Farmer A releases it.");
        Assert.True(_lockService.IsLocked(locationName, coordinate));
        Assert.Equal(farmerB, _lockService.GetLockHolder(locationName, coordinate));
    }

    /// <summary>
    /// Verifies idempotent / correct farmer release: farmer B cannot release farmer A's lock.
    /// </summary>
    [Fact]
    public void ReleaseLock_ByDifferentFarmer_DoesNotReleaseLock()
    {
        // Arrange
        const string locationName = "Farm";
        var coordinate = new Vector2(2, 3);
        const long farmerA = 1001L;
        const long farmerB = 1002L;
        const long farmerC = 1003L;

        _lockService.TryAcquireLock(locationName, coordinate, farmerA);

        // Act: Farmer B attempts to release Farmer A's lock
        _lockService.ReleaseLock(locationName, coordinate, farmerB);

        // Assert: Farmer A's lock remains intact and active
        Assert.True(_lockService.IsLocked(locationName, coordinate), "Lock should not be released by a different farmer.");
        Assert.Equal(farmerA, _lockService.GetLockHolder(locationName, coordinate));

        // Attempt by Farmer C must still be refused
        var acquiredByC = _lockService.TryAcquireLock(locationName, coordinate, farmerC);
        Assert.False(acquiredByC, "Farmer C must still be refused because Farmer A's lock is intact.");
    }

    /// <summary>
    /// Verifies that releasing an unlocked tile or releasing multiple times is safe and idempotent.
    /// </summary>
    [Fact]
    public void ReleaseLock_WhenUnlockedOrDuplicate_IsSafeNoOp()
    {
        // Arrange
        const string locationName = "Farm";
        var coordinate = new Vector2(7, 8);
        const long farmerId = 1001L;

        // Act & Assert: Release without acquire does not throw
        var exception = Record.Exception(() =>
        {
            _lockService.ReleaseLock(locationName, coordinate, farmerId);
        });
        Assert.Null(exception);
        Assert.False(_lockService.IsLocked(locationName, coordinate));

        // Acquire then release twice
        _lockService.TryAcquireLock(locationName, coordinate, farmerId);
        _lockService.ReleaseLock(locationName, coordinate, farmerId);

        var duplicateReleaseException = Record.Exception(() =>
        {
            _lockService.ReleaseLock(locationName, coordinate, farmerId);
        });
        Assert.Null(duplicateReleaseException);
        Assert.False(_lockService.IsLocked(locationName, coordinate));
    }

    /// <summary>
    /// Verifies coordinate isolation: locks at (1, 1) and (1, 2) do not conflict.
    /// </summary>
    [Fact]
    public void TryAcquireLock_CoordinateIsolation_DifferentCoordinatesDoNotConflict()
    {
        // Arrange
        const string locationName = "Farm";
        var coord1 = new Vector2(1, 1);
        var coord2 = new Vector2(1, 2);
        const long farmerA = 1001L;
        const long farmerB = 1002L;

        // Act
        var acquiredCoord1 = _lockService.TryAcquireLock(locationName, coord1, farmerA);
        var acquiredCoord2 = _lockService.TryAcquireLock(locationName, coord2, farmerB);

        // Assert
        Assert.True(acquiredCoord1, "Farmer A should acquire (1, 1).");
        Assert.True(acquiredCoord2, "Farmer B should acquire (1, 2) concurrently.");
        Assert.Equal(farmerA, _lockService.GetLockHolder(locationName, coord1));
        Assert.Equal(farmerB, _lockService.GetLockHolder(locationName, coord2));

        // Releasing coord1 does not release coord2
        _lockService.ReleaseLock(locationName, coord1, farmerA);
        Assert.False(_lockService.IsLocked(locationName, coord1));
        Assert.True(_lockService.IsLocked(locationName, coord2));
        Assert.Equal(farmerB, _lockService.GetLockHolder(locationName, coord2));
    }

    /// <summary>
    /// Verifies location isolation: locks at ("Farm", 1, 1) and ("Greenhouse", 1, 1) do not conflict.
    /// </summary>
    [Fact]
    public void TryAcquireLock_LocationIsolation_DifferentLocationsDoNotConflict()
    {
        // Arrange
        const string farmLocation = "Farm";
        const string greenhouseLocation = "Greenhouse";
        var coordinate = new Vector2(1, 1);
        const long farmerA = 1001L;
        const long farmerB = 1002L;

        // Act
        var acquiredFarm = _lockService.TryAcquireLock(farmLocation, coordinate, farmerA);
        var acquiredGreenhouse = _lockService.TryAcquireLock(greenhouseLocation, coordinate, farmerB);

        // Assert
        Assert.True(acquiredFarm, "Farmer A should acquire Farm (1, 1).");
        Assert.True(acquiredGreenhouse, "Farmer B should acquire Greenhouse (1, 1).");
        Assert.Equal(farmerA, _lockService.GetLockHolder(farmLocation, coordinate));
        Assert.Equal(farmerB, _lockService.GetLockHolder(greenhouseLocation, coordinate));

        // Releasing Farm does not release Greenhouse
        _lockService.ReleaseLock(farmLocation, coordinate, farmerA);
        Assert.False(_lockService.IsLocked(farmLocation, coordinate));
        Assert.True(_lockService.IsLocked(greenhouseLocation, coordinate));
        Assert.Equal(farmerB, _lockService.GetLockHolder(greenhouseLocation, coordinate));
    }

    /// <summary>
    /// Verifies multi-threaded contention: out of N concurrent attempts on the exact same tile,
    /// exactly one succeeds and all others fail.
    /// </summary>
    [Fact]
    public async Task TryAcquireLock_MultiThreadedContention_ExactlyOneSucceeds()
    {
        // Arrange
        const string locationName = "Farm";
        var coordinate = new Vector2(42, 42);
        const int concurrentAttempts = 50;
        var startSignal = new ManualResetEventSlim(false);
        var results = new ConcurrentBag<(long FarmerId, bool Acquired)>();

        // Act: Spin up concurrent tasks attempting to acquire the lock at the same instant
        var tasks = Enumerable.Range(1, concurrentAttempts).Select(i =>
        {
            var farmerId = 1000L + i;
            return Task.Run(() =>
            {
                startSignal.Wait();
                var success = _lockService.TryAcquireLock(locationName, coordinate, farmerId);
                results.Add((farmerId, success));
            });
        }).ToArray();

        // Release all threads simultaneously
        startSignal.Set();
        await Task.WhenAll(tasks);

        // Assert
        var successfulAttempts = results.Where(r => r.Acquired).ToList();
        var failedAttempts = results.Where(r => !r.Acquired).ToList();

        Assert.Single(successfulAttempts);
        Assert.Equal(concurrentAttempts - 1, failedAttempts.Count);

        var winningFarmerId = successfulAttempts[0].FarmerId;
        Assert.True(_lockService.IsLocked(locationName, coordinate));
        Assert.Equal(winningFarmerId, _lockService.GetLockHolder(locationName, coordinate));
    }

    /// <summary>
    /// Verifies multi-threaded contention across multiple independent coordinates:
    /// each coordinate resolves independently with exactly one winner per tile.
    /// </summary>
    [Fact]
    public async Task TryAcquireLock_MultiThreadedContentionAcrossMultipleTiles_IndependentResolution()
    {
        // Arrange
        const string locationName = "Farm";
        const int tileCount = 5;
        const int competitorsPerTile = 20;
        var startSignal = new ManualResetEventSlim(false);
        var results = new ConcurrentBag<(Vector2 Coordinate, long FarmerId, bool Acquired)>();

        var tasks = new List<Task>();
        for (var t = 0; t < tileCount; t++)
        {
            var tile = new Vector2(t, t);
            for (var c = 0; c < competitorsPerTile; c++)
            {
                var farmerId = (t * 1000L) + c + 1;
                tasks.Add(Task.Run(() =>
                {
                    startSignal.Wait();
                    var success = _lockService.TryAcquireLock(locationName, tile, farmerId);
                    results.Add((tile, farmerId, success));
                }));
            }
        }

        // Release all threads simultaneously
        startSignal.Set();
        await Task.WhenAll(tasks);

        // Assert: Each tile must have exactly 1 winner
        for (var t = 0; t < tileCount; t++)
        {
            var tile = new Vector2(t, t);
            var tileResults = results.Where(r => r.Coordinate == tile).ToList();
            var winners = tileResults.Where(r => r.Acquired).ToList();

            Assert.Single(winners);
            Assert.Equal(competitorsPerTile - 1, tileResults.Count(r => !r.Acquired));
            Assert.True(_lockService.IsLocked(locationName, tile));
            Assert.Equal(winners[0].FarmerId, _lockService.GetLockHolder(locationName, tile));
        }
    }
}
