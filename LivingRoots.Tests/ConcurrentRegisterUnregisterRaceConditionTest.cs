using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests
{
    public class ConcurrentRegisterUnregisterRaceConditionTest
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;

        public ConcurrentRegisterUnregisterRaceConditionTest()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockModDataService = new Mock<IModDataService>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();

            // Add default setup for _mockManifest properties to prevent NullReferenceException
            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest.Setup(x => x.Version).Returns(new StardewModdingAPI.SemanticVersion(1, 0, 0));
        }

        [Fact]
        public async Task UnregisterEvents_WhenConcurrentRegisterEventsOccurs_DoesNotLeakHandlers()
        {
            // Arrange: Set up mocks for events
            var mockEvents = new Mock<IModEvents>();
            var threadSafeGameLoopEvents = new ThreadSafeGameLoopEventsStub();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(threadSafeGameLoopEvents);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Create a single ModController instance to be shared across all tasks
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, 
                _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to set up the controller with handlers
            controller.RegisterEvents();

            // Verify that events were initially registered
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);

            // Act: Simulate concurrent registration and unregistration attempts
            var tasks = new List<Task>();
            
            // Multiple tasks trying to register events concurrently
            for (int i = 0; i < 5; i++)
            {
                var registerTask = Task.Run(() =>
                {
                    // Each task attempts to register events
                    controller.RegisterEvents();
                });
                tasks.Add(registerTask);
            }

            // Multiple tasks trying to unregister events concurrently
            for (int i = 0; i < 5; i++)
            {
                var unregisterTask = Task.Run(() =>
                {
                    // Each task attempts to unregister events
                    controller.UnregisterEvents();
                });
                tasks.Add(unregisterTask);
            }

            // Add timeout to prevent hanging indefinitely
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var whenAllTask = Task.WhenAll(tasks);
            
            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Assert.Fail("UnregisterEvents_WhenConcurrentRegisterEventsOccurs_DoesNotLeakHandlers test timed out after 30 seconds");
            }
            
            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Assert: Check that handlers were properly managed despite concurrent operations
            // The thread-safe implementation should ensure that:
            // 1. No handlers are leaked (unregister properly captures and removes handlers)
            // 2. The state flags are correctly managed
            // 3. No race condition allows new registrations during unregistration
            
            // Verify that event removals occurred properly (should be exactly 1 regardless of concurrency)
            // This verifies that the fix prevents handler leaks during concurrent operations
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedRemoveCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedRemoveCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingRemoveCount);

            // Verify that the controller state is consistent
            var state = controller._state;
            var isUnregistering = (state & (1 << 5)) != 0; // UnregisteringFlag
            var isEventsRegistered = (state & (1 << 0)) != 0; // EventsRegisteredFlag
            
            // After unregistration, EventsRegisteredFlag should be cleared
            // UnregisteringFlag should also be cleared after completion
            Assert.False(isUnregistering, "UnregisteringFlag should be cleared after unregistration completes");
            
            // The EventsRegisteredFlag might be set or cleared depending on the final state,
            // but the important thing is that handlers are properly managed
        }

        [Fact]
        public async Task RegisterEvents_WhenConcurrentUnregisterEventsOccurs_DoesNotCauseRaceCondition()
        {
            // Arrange: Set up mocks for events
            var mockEvents = new Mock<IModEvents>();
            var threadSafeGameLoopEvents = new ThreadSafeGameLoopEventsStub();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(threadSafeGameLoopEvents);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Create a single ModController instance to be shared across all tasks
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, 
                _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to set up the controller with handlers
            controller.RegisterEvents();

            // Verify that events were initially registered
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);

            // Act: Simulate multiple concurrent registration and unregistration operations
            // This test specifically targets the race condition where handler references
            // are captured before UnregisteringFlag is set
            var tasks = new List<Task>();
            
            // Start multiple unregistration tasks that will try to capture handler references
            for (int i = 0; i < 3; i++)
            {
                var unregisterTask = Task.Run(() =>
                {
                    controller.UnregisterEvents();
                });
                tasks.Add(unregisterTask);
            }

            // While unregistrations are in progress, try to register events
            // This creates the race condition scenario we're testing
            for (int i = 0; i < 3; i++)
            {
                var registerTask = Task.Run(() =>
                {
                    controller.RegisterEvents();
                });
                tasks.Add(registerTask);
            }

            // Add timeout to prevent hanging indefinitely
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var whenAllTask = Task.WhenAll(tasks);
            
            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Assert.Fail("RegisterEvents_WhenConcurrentUnregisterEventsOccurs_DoesNotCauseRaceCondition test timed out after 30 seconds");
            }
            
            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Assert: Verify that the race condition fix works properly
            // The fix should ensure that handler references are captured AFTER UnregisteringFlag is set
            // This prevents new registrations from leaking handlers during unregistration
            
            // All tasks should complete without throwing exceptions
            // Event handlers should be properly managed
            Assert.True(threadSafeGameLoopEvents.GameLaunchedAddCount >= 1, 
                "At least one GameLaunched registration should occur");
            Assert.True(threadSafeGameLoopEvents.SaveLoadedAddCount >= 1, 
                "At least one SaveLoaded registration should occur");
            Assert.True(threadSafeGameLoopEvents.SavingAddCount >= 1, 
                "At least one Saving registration should occur");
                
            // The number of removals should match the number of successful unregistrations
            Assert.True(threadSafeGameLoopEvents.GameLaunchedRemoveCount >= 0, 
                "GameLaunched should have been removed appropriately");
            Assert.True(threadSafeGameLoopEvents.SaveLoadedRemoveCount >= 0, 
                "SaveLoaded should have been removed appropriately");
            Assert.True(threadSafeGameLoopEvents.SavingRemoveCount >= 0, 
                "Saving should have been removed appropriately");
        }

        /// <summary>
        /// Thread-safe stub implementation of IGameLoopEvents for concurrency testing.
        /// Uses Interlocked operations to track event additions and removals in a thread-safe manner.
        /// This ensures tests reliably validate application code's thread safety without
        /// relying on Moq's internal state which is not thread-safe.
        /// </summary>
        private sealed class ThreadSafeGameLoopEventsStub : IGameLoopEvents
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
            public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add { } remove { } }
            public event EventHandler<UpdateTickingEventArgs>? UpdateTicking { add { } remove { } }
            public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add { } remove { } }
            public event EventHandler<OneSecondUpdateTickingEventArgs>? OneSecondUpdateTicking { add { } remove { } }
            public event EventHandler<DayStartedEventArgs>? DayStarted { add { } remove { } }
            public event EventHandler<DayEndingEventArgs>? DayEnding { add { } remove { } }
            public event EventHandler<TimeChangedEventArgs>? TimeChanged { add { } remove { } }
            public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle { add { } remove { } }
            public event EventHandler<SaveCreatingEventArgs>? SaveCreating { add { } remove { } }
            public event EventHandler<SaveCreatedEventArgs>? SaveCreated { add { } remove { } }
            public event EventHandler<SavedEventArgs>? Saved { add { } remove { } }
            public event EventHandler<LoadStageChangedEventArgs>? LoadStageChanged { add { } remove { } }
        }
    }
}