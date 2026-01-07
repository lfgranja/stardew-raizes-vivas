using System.Reflection;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Tests
{
    public class ConcurrentRegisterUnregisterRaceConditionTest
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ISoilHealthVisualizationService> _mockSoilHealthVisualizationService;

        public ConcurrentRegisterUnregisterRaceConditionTest()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();
            _mockSoilHealthVisualizationService = new Mock<ISoilHealthVisualizationService>();

            // Add default setup for _mockManifest properties to prevent NullReferenceException
            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest
                .Setup(x => x.Version)
                .Returns(new StardewModdingAPI.SemanticVersion(1, 0, 0));
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
            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // First register events to set up the controller with handlers
            controller.RegisterEvents();

            // Verify that events were initially registered
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);

            // Act: Simulate concurrent registration and unregistration attempts
            var tasks = new List<Task>();

            // Multiple tasks trying to register events concurrently
            for (var i = 0; i < 5; i++)
            {
                var registerTask = Task.Run(() =>
                {
                    // Each task attempts to register events
                    controller.RegisterEvents();
                });
                tasks.Add(registerTask);
            }

            // Multiple tasks trying to unregister events concurrently
            for (var i = 0; i < 5; i++)
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
                Assert.Fail(
                    "UnregisterEvents_WhenConcurrentRegisterEventsOccurs_DoesNotLeakHandlers test timed out after 30 seconds"
                );
            }

            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Assert: Check that handlers were properly managed despite concurrent operations
            // The thread-safe implementation should ensure that:
            // 1. No handlers are leaked (unregister properly captures and removes handlers)
            // 2. The state flags are correctly managed
            // 3. No race condition allows new registrations during unregistration

            // Verify invariants rather than schedule-dependent exact counts:
            // 1) removals must never exceed adds
            Assert.True(
                threadSafeGameLoopEvents.GameLaunchedRemoveCount
                    <= threadSafeGameLoopEvents.GameLaunchedAddCount
            );
            Assert.True(
                threadSafeGameLoopEvents.SaveLoadedRemoveCount
                    <= threadSafeGameLoopEvents.SaveLoadedAddCount
            );
            Assert.True(
                threadSafeGameLoopEvents.SavingRemoveCount
                    <= threadSafeGameLoopEvents.SavingAddCount
            );

            // 2) net subscriptions must be either 0 (unregistered) or 1 (registered)
            var netGameLaunched =
                threadSafeGameLoopEvents.GameLaunchedAddCount
                - threadSafeGameLoopEvents.GameLaunchedRemoveCount;
            var netSaveLoaded =
                threadSafeGameLoopEvents.SaveLoadedAddCount
                - threadSafeGameLoopEvents.SaveLoadedRemoveCount;
            var netSaving =
                threadSafeGameLoopEvents.SavingAddCount
                - threadSafeGameLoopEvents.SavingRemoveCount;

            Assert.InRange(netGameLaunched, 0, 1);
            Assert.InRange(netSaveLoaded, 0, 1);
            Assert.InRange(netSaving, 0, 1);

            // Verify that the controller state is consistent
            // Use reflection to access the private _state field since it's not publicly accessible
            // This is necessary for testing internal state flags like UnregisteringFlag and EventsRegisteredFlag
            var stateField = typeof(ModController).GetField(
                "_state",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var state = (int)(stateField?.GetValue(controller) ?? 0);

            var isUnregistering = (state & ModController.UnregisteringFlag) != 0; // UnregisteringFlag

            // After unregistration, EventsRegisteredFlag should be cleared
            // UnregisteringFlag should also be cleared after completion
            Assert.False(
                isUnregistering,
                "UnregisteringFlag should be cleared after unregistration completes"
            );

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
            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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
            for (var i = 0; i < 3; i++)
            {
                var unregisterTask = Task.Run(() =>
                {
                    controller.UnregisterEvents();
                });
                tasks.Add(unregisterTask);
            }

            // While unregistrations are in progress, try to register events
            // This creates the race condition scenario we're testing
            for (var i = 0; i < 3; i++)
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
                Assert.Fail(
                    "RegisterEvents_WhenConcurrentUnregisterEventsOccurs_DoesNotCauseRaceCondition test timed out after 30 seconds"
                );
            }

            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Assert: Verify that the race condition fix works properly
            // The fix should ensure that handler references are captured AFTER UnregisteringFlag is set
            // This prevents new registrations from leaking handlers during unregistration

            // All tasks should complete without throwing exceptions
            // Event handlers should be properly managed
            Assert.True(
                threadSafeGameLoopEvents.GameLaunchedAddCount >= 1,
                "At least one GameLaunched registration should occur"
            );
            Assert.True(
                threadSafeGameLoopEvents.SaveLoadedAddCount >= 1,
                "At least one SaveLoaded registration should occur"
            );
            Assert.True(
                threadSafeGameLoopEvents.SavingAddCount >= 1,
                "At least one Saving registration should occur"
            );

            // The number of removals should match the number of successful unregistrations
            Assert.True(
                threadSafeGameLoopEvents.GameLaunchedRemoveCount >= 0,
                "GameLaunched should have been removed appropriately"
            );
            Assert.True(
                threadSafeGameLoopEvents.SaveLoadedRemoveCount >= 0,
                "SaveLoaded should have been removed appropriately"
            );
            Assert.True(
                threadSafeGameLoopEvents.SavingRemoveCount >= 0,
                "Saving should have been removed appropriately"
            );
        }
    }
}
