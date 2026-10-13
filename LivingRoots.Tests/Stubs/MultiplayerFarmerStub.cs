using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LivingRoots.Domain;
using LivingRoots.Tests.Fixtures;
using StardewValley;

namespace LivingRoots.Tests.Stubs
{
    /// <summary>
    /// T002 (spec 004 Phase 1): a concurrent multi-farmer context stub simulating
    /// the multiplayer actors the host-authoritative locking behaviour is
    /// specified against. A real <see cref="Farmer"/> cannot be constructed in a
    /// unit-test host — its constructor dereferences sprite data that only exists
    /// once the game has booted — so each simulated farmer carries just the state
    /// the concurrency tests observe: the multiplayer ID FR-019 locks on, the held
    /// item the deposit path consumes, a recording inventory, and the refusal
    /// messages the conflict sites emit.
    /// </summary>
    /// <remarks>
    /// T141 (Constitution III): this stub is async-only from the start — no
    /// <c>.Result</c>, <c>.Wait()</c> or <c>.GetAwaiter().GetResult()</c> anywhere.
    /// Synchronisation uses a barrier so "both players act at the same tick" is a
    /// real rendezvous, not a scheduling accident.
    /// </remarks>
    public sealed class MultiplayerFarmerStub
    {
        private readonly List<string> _refusalMessages = new();

        private MultiplayerFarmerStub(long farmerId, string role)
        {
            FarmerId = farmerId;
            Role = role;
        }

        /// <summary>Multiplayer ID the atomic lock keys on (FR-019).</summary>
        public long FarmerId { get; }

        /// <summary>
        /// T125 (FR-019): the multiplayer ID exactly as the production
        /// TryApplyCompostWithLock / TryInteractWithBin entry points key the
        /// host-authoritative lock on. An alias of <see cref="FarmerId"/> kept
        /// distinct so the simulation reads the way the game code does.
        /// </summary>
        public long UniqueMultiplayerID => FarmerId;

        /// <summary>"Host" or "Farmhand" — the role the ID belongs to.</summary>
        public string Role { get; }

        /// <summary>The farm-scoped recording inventory this farmer deposits into.</summary>
        public RecordingPlayerInventory Inventory { get; } = new();

        /// <summary>Item currently held, or null for empty hands.</summary>
        public Item? CurrentItem { get; private set; }

        /// <summary>Count of interactions this farmer recorded as successful.</summary>
        public int SuccessCount { get; private set; }

        /// <summary>Refusal translation keys this farmer was shown, in order.</summary>
        public IReadOnlyList<string> RefusalMessages => _refusalMessages;

        /// <summary>Count of refusals this farmer recorded.</summary>
        public int RefusedCount => _refusalMessages.Count;

        /// <summary>Sets the held item, as a player equipping a slot would.</summary>
        public void SetHeldItem(Item? item)
        {
            CurrentItem = item;
        }

        /// <summary>Empties the hands, as a player putting an item away would.</summary>
        public void ClearHeldItem()
        {
            CurrentItem = null;
        }

        /// <summary>Records a refusal translation key (FR-008a).</summary>
        public void RecordRefusal(string messageKey)
        {
            _refusalMessages.Add(messageKey);
        }

        /// <summary>Records a successful interaction (FR-019 first-wins).</summary>
        public void RecordSuccess()
        {
            SuccessCount++;
        }

        /// <summary>
        /// Creates a host + farmhand pair sharing one simulation context.
        /// </summary>
        /// <param name="hostId">Multiplayer ID of the host.</param>
        /// <param name="farmhandId">Multiplayer ID of the farmhand.</param>
        /// <returns>The pair and the context that drives them simultaneously.</returns>
        public static (MultiplayerFarmerStub host, MultiplayerFarmerStub farmhand, MultiplayerSimulationContext context) CreatePair(
            long hostId = 1L,
            long farmhandId = 2L)
        {
            var host = new MultiplayerFarmerStub(hostId, "Host");
            var farmhand = new MultiplayerFarmerStub(farmhandId, "Farmhand");
            return (host, farmhand, new MultiplayerSimulationContext(host, farmhand));
        }

        /// <summary>
        /// Creates a session of one host plus <paramref name="farmhandCount"/>
        /// farmhands, each with a distinct multiplayer ID.
        /// </summary>
        /// <param name="farmhandCount">Number of farmhands beyond the host.</param>
        public static MultiplayerSimulationContext CreateSession(int farmhandCount)
        {
            if (farmhandCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(farmhandCount), farmhandCount, "Farmhand count cannot be negative.");
            }

            var host = new MultiplayerFarmerStub(1L, "Host");
            var farmers = new List<MultiplayerFarmerStub> { host };
            for (var i = 0; i < farmhandCount; i++)
            {
                farmers.Add(new MultiplayerFarmerStub(i + 2L, "Farmhand"));
            }

            return new MultiplayerSimulationContext(farmers.ToArray());
        }
    }

    /// <summary>
    /// T002: the driver that runs the simulated farmers' actions at the same
    /// instant. Every action first waits on a barrier shared with all other
    /// participants, so "two players acted simultaneously" means the actions
    /// entered the controller within the same scheduling window — the closest
    /// deterministic equivalent of a simultaneous multiplayer tick available on
    /// a single-threaded test host.
    /// </summary>
    public sealed class MultiplayerSimulationContext : IPlayerProvider
    {
        private readonly MultiplayerFarmerStub[] _farmers;
        private readonly Barrier _barrier;

        internal MultiplayerSimulationContext(params MultiplayerFarmerStub[] farmers)
        {
            _farmers = farmers ?? throw new ArgumentNullException(nameof(farmers));
            if (_farmers.Length == 0)
            {
                throw new ArgumentException("At least one farmer is required.", nameof(farmers));
            }

            _barrier = new Barrier(_farmers.Length);
            _activeFarmer = _farmers[0];
        }

        /// <summary>The farmer the participant is currently acting as.</summary>
        private MultiplayerFarmerStub _activeFarmer;

        /// <summary>
        /// Selects the farmer whose context the next call resolves. The
        /// production entry points (<see cref="TryApplyCompostWithLock"/>,
        /// TryInteractWithBin) take a farmer ID but the services read the held
        /// item through <see cref="IPlayerProvider"/>, so the simulation must be
        /// told which actor is acting — otherwise a two-player test would watch
        /// one farmer's item being consumed for the other's action.
        /// </summary>
        /// <param name="farmer">The farmer about to act.</param>
        public void SetActiveFarmer(MultiplayerFarmerStub farmer)
        {
            _activeFarmer = farmer ?? throw new ArgumentNullException(nameof(farmer));
        }

        /// <summary>The farmer whose held item the provider currently resolves.</summary>
        public MultiplayerFarmerStub ActiveFarmer => _activeFarmer;

        /// <inheritdoc />
        /// <remarks>
        /// T125: the context is also the <see cref="IPlayerProvider"/> the
        /// production CompostApplicationService resolves held items through, so
        /// the SC-010 headline test drives the real service instead of an
        /// inline reimplementation. <see cref="IPlayerProvider.CurrentPlayer"/>
        /// returns a real <c>Farmer</c>, which cannot be constructed in a
        /// unit-test host, so it throws here — no production path in this test
        /// touches it.
        /// </remarks>
        public Farmer CurrentPlayer => throw new NotSupportedException(
            "A real Farmer cannot be constructed in a unit-test host; use the farmer stub's held item instead.");

        /// <inheritdoc />
        public Item? CurrentItem => _activeFarmer.CurrentItem;

        /// <summary>Every farmer participating in this simulation, in join order.</summary>
        public IReadOnlyList<MultiplayerFarmerStub> AllFarmers => _farmers;

        /// <summary>
        /// Runs <paramref name="action"/> for every farmer, released from a
        /// single barrier so all of them start together. Async-only (T141): the
        /// barrier uses a participant-limit timeout rather than any blocking
        /// wait on an uncompleted task.
        /// </summary>
        /// <typeparam name="T">Result type of one farmer's action.</typeparam>
        /// <param name="action">Action to run per farmer.</param>
        /// <returns>The results, in farmer order.</returns>
        public async Task<IReadOnlyList<T>> RunSimultaneouslyAsync<T>(Func<MultiplayerFarmerStub, Task<T>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // Clone the barrier each run so a context can be reused across acts
            // within one test while still rendezvousing every act.
            var barrier = new Barrier(_farmers.Length);
            var tasks = _farmers.Select(async farmer =>
            {
                // Rendezvous: every participant reaches this point before any
                // of them enters the action, so the actions overlap.
                await Task.Yield();
                barrier.SignalAndWait();

                return await action(farmer);
            });

            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Non-generic overload for void-ish actions: a lambda returning
        /// <see cref="Task.CompletedTask"/> binds here rather than failing T
        /// inference.
        /// </summary>
        /// <param name="action">Action to run per farmer.</param>
        public async Task RunSimultaneouslyAsync(Func<MultiplayerFarmerStub, Task> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var barrier = new Barrier(_farmers.Length);
            var tasks = _farmers.Select(async farmer =>
            {
                await Task.Yield();
                barrier.SignalAndWait();

                await action(farmer);
            });

            await Task.WhenAll(tasks);
        }
    }
}
