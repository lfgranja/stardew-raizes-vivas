using System;
using System.Threading;
using StardewModdingAPI.Events;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Thread-safe stub implementation of IGameLoopEvents for concurrency testing.
    /// Uses Interlocked operations to track event additions and removals in a thread-safe manner.
    /// This ensures tests reliably validate application code's thread safety without
    /// relying on Moq's internal state which is not thread-safe.
    /// </summary>
    public sealed class ThreadSafeGameLoopEventsStub : IGameLoopEvents
    {
        private EventHandler<GameLaunchedEventArgs>? _gameLaunched;
        private EventHandler<SaveLoadedEventArgs>? _saveLoaded;
        private EventHandler<SavingEventArgs>? _saving;

        // Thread-safe counters using Interlocked operations
        private int _gameLaunchedAddCount = 0;
        private int _saveLoadedAddCount = 0;
        private int _savingAddCount = 0;
        private int _gameLaunchedRemoveCount = 0;
        private int _saveLoadedRemoveCount = 0;
        private int _savingRemoveCount = 0;

        public int GameLaunchedAddCount => Volatile.Read(ref _gameLaunchedAddCount);
        public int SaveLoadedAddCount => Volatile.Read(ref _saveLoadedAddCount);
        public int SavingAddCount => Volatile.Read(ref _savingAddCount);
        public int GameLaunchedRemoveCount => Volatile.Read(ref _gameLaunchedRemoveCount);
        public int SaveLoadedRemoveCount => Volatile.Read(ref _saveLoadedRemoveCount);
        public int SavingRemoveCount => Volatile.Read(ref _savingRemoveCount);

        public event EventHandler<GameLaunchedEventArgs>? GameLaunched
        {
            add
            {
                Interlocked.Increment(ref _gameLaunchedAddCount);
                EventHandler<GameLaunchedEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _gameLaunched);
                    updated = (EventHandler<GameLaunchedEventArgs>?)Delegate.Combine(current, value);
                }
                while (Interlocked.CompareExchange(ref _gameLaunched, updated, current) != current);
            }
            remove
            {
                Interlocked.Increment(ref _gameLaunchedRemoveCount);
                EventHandler<GameLaunchedEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _gameLaunched);
                    updated = (EventHandler<GameLaunchedEventArgs>?)Delegate.Remove(current, value);
                }
                while (Interlocked.CompareExchange(ref _gameLaunched, updated, current) != current);
            }
        }

        public event EventHandler<SaveLoadedEventArgs>? SaveLoaded
        {
            add
            {
                Interlocked.Increment(ref _saveLoadedAddCount);
                EventHandler<SaveLoadedEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _saveLoaded);
                    updated = (EventHandler<SaveLoadedEventArgs>?)Delegate.Combine(current, value);
                }
                while (Interlocked.CompareExchange(ref _saveLoaded, updated, current) != current);
            }
            remove
            {
                Interlocked.Increment(ref _saveLoadedRemoveCount);
                EventHandler<SaveLoadedEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _saveLoaded);
                    updated = (EventHandler<SaveLoadedEventArgs>?)Delegate.Remove(current, value);
                }
                while (Interlocked.CompareExchange(ref _saveLoaded, updated, current) != current);
            }
        }

        public event EventHandler<SavingEventArgs>? Saving
        {
            add
            {
                Interlocked.Increment(ref _savingAddCount);
                EventHandler<SavingEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _saving);
                    updated = (EventHandler<SavingEventArgs>?)Delegate.Combine(current, value);
                }
                while (Interlocked.CompareExchange(ref _saving, updated, current) != current);
            }
            remove
            {
                Interlocked.Increment(ref _savingRemoveCount);
                EventHandler<SavingEventArgs>? current, updated;
                do
                {
                    current = Volatile.Read(ref _saving);
                    updated = (EventHandler<SavingEventArgs>?)Delegate.Remove(current, value);
                }
                while (Interlocked.CompareExchange(ref _saving, updated, current) != current);
            }
        }

        // Other IGameLoopEvents members not used in tests - implemented as no-ops
        public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add => _ = value; remove => _ = value; }
        public event EventHandler<UpdateTickingEventArgs>? UpdateTicking { add => _ = value; remove => _ = value; }
        public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add => _ = value; remove => _ = value; }
        public event EventHandler<OneSecondUpdateTickingEventArgs>? OneSecondUpdateTicking { add => _ = value; remove => _ = value; }
        public event EventHandler<DayStartedEventArgs>? DayStarted
        {
            add => _ = value;
            remove => _ = value;
        }
        public event EventHandler<DayEndingEventArgs>? DayEnding { add => _ = value; remove => _ = value; }
        public event EventHandler<TimeChangedEventArgs>? TimeChanged
        {
            add => _ = value;
            remove => _ = value;
        }
        public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle
        {
            add => _ = value;
            remove => _ = value;
        }
        public event EventHandler<SaveCreatingEventArgs>? SaveCreating { add => _ = value; remove => _ = value; }
        public event EventHandler<SaveCreatedEventArgs>? SaveCreated { add => _ = value; remove => _ = value; }
        public event EventHandler<SavedEventArgs>? Saved { add => _ = value; remove => _ = value; }
        public event EventHandler<LoadStageChangedEventArgs>? LoadStageChanged { add => _ = value; remove => _ = value; }
    }
}
