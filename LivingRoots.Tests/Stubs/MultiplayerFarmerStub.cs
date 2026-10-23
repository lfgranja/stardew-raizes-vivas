using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Tests.Fixtures;
using StardewValley;

namespace LivingRoots.Tests.Stubs;

/// <summary>
/// Stub representing an individual simulated farmer in a multiplayer session.
/// Implements <see cref="IPlayerProvider"/> and tracks inventory, held items, and action results
/// for concurrent multi-farmer simulation and test assertions.
/// </summary>
public class MultiplayerFarmerStub : IPlayerProvider
{
    private readonly ConcurrentBag<string> _refusalMessages = new();
    private int _attemptCount;
    private int _successCount;
    private int _refusedCount;
    private Farmer _currentPlayer = null!;
    private Item? _currentItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiplayerFarmerStub"/> class.
    /// </summary>
    /// <param name="farmerId">The unique multiplayer ID of this farmer.</param>
    /// <param name="name">The display name of the farmer.</param>
    /// <param name="isHost">True if this farmer is the host player; otherwise, false.</param>
    public MultiplayerFarmerStub(long farmerId = 1L, string name = "Host", bool isHost = true)
    {
        FarmerId = farmerId;
        Name = name;
        IsHost = isHost;
        Inventory = new RecordingPlayerInventory();
    }

    /// <summary>
    /// Gets or sets the unique multiplayer ID of this farmer.
    /// </summary>
    public long FarmerId { get; set; }

    /// <summary>
    /// Gets or sets the unique multiplayer ID matching Stardew Valley conventions.
    /// </summary>
    public long UniqueMultiplayerID
    {
        get
        {
            return FarmerId;
        }
        set
        {
            FarmerId = value;
        }
    }

    /// <summary>
    /// Gets or sets the farmer name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this farmer is the host.
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// Gets or sets the recorded inventory for this farmer.
    /// </summary>
    public RecordingPlayerInventory Inventory { get; set; }

    /// <summary>
    /// Gets or sets the underlying <see cref="Farmer"/> game entity.
    /// </summary>
    public Farmer CurrentPlayer
    {
        get
        {
            return _currentPlayer;
        }
        set
        {
            _currentPlayer = value;
        }
    }

    /// <summary>
    /// Gets or sets the current held item for this farmer.
    /// </summary>
    public Item? CurrentItem
    {
        get
        {
            return _currentItem;
        }
        set
        {
            _currentItem = value;
        }
    }

    /// <summary>
    /// Gets all refusal messages recorded for this farmer.
    /// </summary>
    public IReadOnlyList<string> RefusalMessages
    {
        get
        {
            return _refusalMessages.ToArray();
        }
    }

    /// <summary>
    /// Gets the number of actions attempted by this farmer.
    /// </summary>
    public int AttemptCount
    {
        get
        {
            return Volatile.Read(ref _attemptCount);
        }
    }

    /// <summary>
    /// Gets the number of actions that succeeded for this farmer.
    /// </summary>
    public int SuccessCount
    {
        get
        {
            return Volatile.Read(ref _successCount);
        }
    }

    /// <summary>
    /// Gets the number of actions that were refused for this farmer.
    /// </summary>
    public int RefusedCount
    {
        get
        {
            return Volatile.Read(ref _refusedCount);
        }
    }

    /// <summary>
    /// Sets the currently held item.
    /// </summary>
    /// <param name="item">The item to hold.</param>
    public void SetHeldItem(Item? item)
    {
        CurrentItem = item;
    }

    /// <summary>
    /// Clears the currently held item.
    /// </summary>
    public void ClearHeldItem()
    {
        CurrentItem = null;
    }

    /// <summary>
    /// Records an attempt to perform an action.
    /// </summary>
    public void RecordAttempt()
    {
        Interlocked.Increment(ref _attemptCount);
    }

    /// <summary>
    /// Records a successfully completed action.
    /// </summary>
    public void RecordSuccess()
    {
        Interlocked.Increment(ref _successCount);
    }

    /// <summary>
    /// Records a refused action along with the refusal explanation message.
    /// </summary>
    /// <param name="message">The refusal message or translation key shown to the farmer.</param>
    public void RecordRefusal(string message)
    {
        Interlocked.Increment(ref _refusedCount);
        if (!string.IsNullOrEmpty(message))
        {
            _refusalMessages.Add(message);
        }
    }

    /// <summary>
    /// Resets attempt, success, and refusal counters and clears recorded refusal messages.
    /// </summary>
    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _attemptCount, 0);
        Interlocked.Exchange(ref _successCount, 0);
        Interlocked.Exchange(ref _refusedCount, 0);
        while (_refusalMessages.TryTake(out _))
        {
        }
    }

    /// <summary>
    /// Factory method to create a host farmer stub.
    /// </summary>
    /// <param name="id">Farmer ID.</param>
    /// <param name="name">Farmer name.</param>
    /// <returns>A configured host farmer stub.</returns>
    public static MultiplayerFarmerStub CreateHost(long id = 1L, string name = "Host")
    {
        return new MultiplayerFarmerStub(id, name, isHost: true);
    }

    /// <summary>
    /// Factory method to create a farmhand stub.
    /// </summary>
    /// <param name="id">Farmer ID.</param>
    /// <param name="name">Farmer name.</param>
    /// <returns>A configured farmhand stub.</returns>
    public static MultiplayerFarmerStub CreateFarmhand(long id = 2L, string name = "Farmhand")
    {
        return new MultiplayerFarmerStub(id, name, isHost: false);
    }

    /// <summary>
    /// Factory method to create a context containing the specified farmers.
    /// </summary>
    /// <param name="farmers">The farmers participating in the session.</param>
    /// <returns>A configured multiplayer farmer context.</returns>
    public static MultiplayerFarmerContext CreateContext(params MultiplayerFarmerStub[] farmers)
    {
        return new MultiplayerFarmerContext(farmers);
    }

    /// <summary>
    /// Factory method to create a standard session with one host and the specified number of farmhands.
    /// </summary>
    /// <param name="farmhandCount">Number of farmhands to create.</param>
    /// <returns>A configured multiplayer farmer context.</returns>
    public static MultiplayerFarmerContext CreateSession(int farmhandCount = 1)
    {
        var farmers = new List<MultiplayerFarmerStub>
        {
            CreateHost(1L, "Host")
        };

        for (int i = 1; i <= farmhandCount; i++)
        {
            farmers.Add(CreateFarmhand(i + 1L, $"Farmhand_{i}"));
        }

        return new MultiplayerFarmerContext(farmers);
    }

    /// <summary>
    /// Factory method to create a two-player host and farmhand pair with their coordinating context.
    /// </summary>
    /// <param name="hostId">Host farmer ID.</param>
    /// <param name="farmhandId">Farmhand farmer ID.</param>
    /// <returns>The host stub, farmhand stub, and shared context.</returns>
    public static (MultiplayerFarmerStub Host, MultiplayerFarmerStub Farmhand, MultiplayerFarmerContext Context) CreatePair(
        long hostId = 1L,
        long farmhandId = 2L)
    {
        var host = CreateHost(hostId, "Host");
        var farmhand = CreateFarmhand(farmhandId, "Farmhand");
        var context = CreateContext(host, farmhand);
        return (host, farmhand, context);
    }
}

/// <summary>
/// Provides a concurrent multi-farmer context stub for simulating multiple farmers
/// acting concurrently on items, bins, and tiles.
/// Manages player registration, thread-safe and async-local context dispatch,
/// and barrier synchronization for race condition testing.
/// </summary>
public class MultiplayerFarmerContext : IPlayerProvider
{
    private static readonly AsyncLocal<MultiplayerFarmerStub?> CurrentAmbientFarmer = new();
    private readonly List<MultiplayerFarmerStub> _farmers = new();
    private readonly object _syncLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiplayerFarmerContext"/> class.
    /// </summary>
    /// <param name="farmers">Initial farmers in the session.</param>
    public MultiplayerFarmerContext(IEnumerable<MultiplayerFarmerStub>? farmers = null)
    {
        if (farmers != null)
        {
            foreach (var farmer in farmers)
            {
                if (farmer != null)
                {
                    _farmers.Add(farmer);
                }
            }
        }

        if (_farmers.Count == 0)
        {
            _farmers.Add(MultiplayerFarmerStub.CreateHost());
        }
    }

    /// <summary>
    /// Gets all farmers currently registered in the multiplayer context.
    /// </summary>
    public IReadOnlyList<MultiplayerFarmerStub> AllFarmers
    {
        get
        {
            lock (_syncLock)
            {
                return _farmers.ToList();
            }
        }
    }

    /// <summary>
    /// Gets the host farmer in the session.
    /// </summary>
    public MultiplayerFarmerStub Host
    {
        get
        {
            lock (_syncLock)
            {
                var host = _farmers.FirstOrDefault(f => f.IsHost);
                return host ?? _farmers[0];
            }
        }
    }

    /// <summary>
    /// Gets all farmhands in the session.
    /// </summary>
    public IReadOnlyList<MultiplayerFarmerStub> Farmhands
    {
        get
        {
            lock (_syncLock)
            {
                return _farmers.Where(f => !f.IsHost).ToList();
            }
        }
    }

    /// <summary>
    /// Gets the farmer active in the current ambient async execution flow,
    /// or falls back to the host farmer if no ambient farmer is set.
    /// </summary>
    public MultiplayerFarmerStub CurrentFarmer
    {
        get
        {
            return CurrentAmbientFarmer.Value ?? Host;
        }
    }

    /// <inheritdoc />
    public Farmer CurrentPlayer
    {
        get
        {
            return CurrentFarmer.CurrentPlayer;
        }
    }

    /// <inheritdoc />
    public Item? CurrentItem
    {
        get
        {
            return CurrentFarmer.CurrentItem;
        }
    }

    /// <summary>
    /// Adds a new farmhand to the multiplayer session.
    /// </summary>
    /// <param name="id">Unique multiplayer ID.</param>
    /// <param name="name">Farmhand name.</param>
    /// <returns>The created farmhand stub.</returns>
    public MultiplayerFarmerStub AddFarmhand(long id, string name)
    {
        lock (_syncLock)
        {
            var farmhand = MultiplayerFarmerStub.CreateFarmhand(id, name);
            _farmers.Add(farmhand);
            return farmhand;
        }
    }

    /// <summary>
    /// Finds a farmer by their unique multiplayer ID.
    /// </summary>
    /// <param name="id">Farmer ID.</param>
    /// <returns>The matching farmer stub, or null if not found.</returns>
    public MultiplayerFarmerStub? GetFarmer(long id)
    {
        lock (_syncLock)
        {
            return _farmers.FirstOrDefault(f => f.FarmerId == id);
        }
    }

    /// <summary>
    /// Establishes an ambient execution scope where the specified farmer is active.
    /// </summary>
    /// <param name="farmer">The farmer to set as active.</param>
    /// <returns>A disposable scope that restores the previous ambient farmer on disposal.</returns>
    public IDisposable UseFarmer(MultiplayerFarmerStub farmer)
    {
        var previous = CurrentAmbientFarmer.Value;
        CurrentAmbientFarmer.Value = farmer;
        return new FarmerScope(previous);
    }

    /// <summary>
    /// Executes an asynchronous action in the context of the specified farmer.
    /// </summary>
    /// <param name="farmer">The farmer performing the action.</param>
    /// <param name="action">The asynchronous action.</param>
    /// <returns>A task representing the completion of the action.</returns>
    public async Task RunAsAsync(MultiplayerFarmerStub farmer, Func<Task> action)
    {
        using (UseFarmer(farmer))
        {
            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes an asynchronous function in the context of the specified farmer and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="farmer">The farmer performing the action.</param>
    /// <param name="action">The asynchronous function returning a result.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> RunAsAsync<T>(MultiplayerFarmerStub farmer, Func<Task<T>> action)
    {
        using (UseFarmer(farmer))
        {
            return await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes a synchronous action in the context of the specified farmer.
    /// </summary>
    /// <param name="farmer">The farmer performing the action.</param>
    /// <param name="action">The synchronous action.</param>
    public void RunAs(MultiplayerFarmerStub farmer, Action action)
    {
        using (UseFarmer(farmer))
        {
            action();
        }
    }

    /// <summary>
    /// Executes a synchronous function in the context of the specified farmer and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="farmer">The farmer performing the action.</param>
    /// <param name="action">The synchronous function.</param>
    /// <returns>The result of the action.</returns>
    public T RunAs<T>(MultiplayerFarmerStub farmer, Func<T> action)
    {
        using (UseFarmer(farmer))
        {
            return action();
        }
    }

    /// <summary>
    /// Simulates concurrent actions by all registered farmers simultaneously using barrier synchronization
    /// to maximize thread contention on shared resources (tiles, bins, locks).
    /// </summary>
    /// <typeparam name="T">Result type of each farmer action.</typeparam>
    /// <param name="action">Action to run for each farmer concurrently.</param>
    /// <returns>Array of results corresponding to each farmer.</returns>
    public async Task<T[]> RunSimultaneouslyAsync<T>(Func<MultiplayerFarmerStub, Task<T>> action)
    {
        List<MultiplayerFarmerStub> snapshot;
        lock (_syncLock)
        {
            snapshot = _farmers.ToList();
        }

        if (snapshot.Count == 0)
        {
            return Array.Empty<T>();
        }

        using var readyBarrier = new Barrier(snapshot.Count + 1);
        var startSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = snapshot.Select(farmer => Task.Run(async () =>
        {
            using (UseFarmer(farmer))
            {
                farmer.RecordAttempt();
                readyBarrier.SignalAndWait();
                await startSignal.Task.ConfigureAwait(false);
                return await action(farmer).ConfigureAwait(false);
            }
        })).ToArray();

        // Wait until all worker tasks have reached the barrier
        readyBarrier.SignalAndWait();

        // Release all concurrent tasks at the exact same instant once all have reached the barrier
        startSignal.SetResult(true);
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates concurrent actions by all registered farmers simultaneously without return values.
    /// </summary>
    /// <param name="action">Action to run for each farmer concurrently.</param>
    /// <returns>A task representing completion of all concurrent actions.</returns>
    public async Task RunSimultaneouslyAsync(Func<MultiplayerFarmerStub, Task> action)
    {
        await RunSimultaneouslyAsync(async farmer =>
        {
            await action(farmer).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates concurrent actions synchronously on thread pool threads with barrier synchronization.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">Synchronous action to execute per farmer.</param>
    /// <returns>Array of results.</returns>
    public T[] RunSimultaneously<T>(Func<MultiplayerFarmerStub, T> action)
    {
        return RunSimultaneouslyAsync(farmer => Task.FromResult(action(farmer))).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Simulates concurrent actions synchronously on thread pool threads without return values.
    /// </summary>
    /// <param name="action">Synchronous action to execute per farmer.</param>
    public void RunSimultaneously(Action<MultiplayerFarmerStub> action)
    {
        RunSimultaneouslyAsync(farmer =>
        {
            action(farmer);
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();
    }

    private sealed class FarmerScope : IDisposable
    {
        private readonly MultiplayerFarmerStub? _previous;

        public FarmerScope(MultiplayerFarmerStub? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            CurrentAmbientFarmer.Value = _previous;
        }
    }
}
