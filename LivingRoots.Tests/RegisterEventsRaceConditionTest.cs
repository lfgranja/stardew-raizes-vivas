using System.Reflection;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Tests
{
    public class RegisterEventsRaceConditionTest
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ISoilHealthVisualizationService> _mockSoilHealthVisualizationService;

        public RegisterEventsRaceConditionTest()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();
            _mockSoilHealthVisualizationService = new Mock<ISoilHealthVisualizationService>();

            // Add default setup for _mockManifest properties to prevent NullReferenceException
            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest.Setup(x => x.Version).Returns(new StardewModdingAPI.SemanticVersion(1, 0, 0));
        }

        [Fact]
        public async Task RegisterEvents_WhenConcurrentRegisterEventsAndUnregisterEventsRaceConditionExists_LeadsToInconsistentState()
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
                _mockSoilHealthService.Object, _mockSaveIdProvider.Object, _mockSoilHealthVisualizationService.Object);

            // First register events to set up the controller with handlers
            controller.RegisterEvents();

            // Verify that events were initially registered
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);

            // Use reflection to access the private _state field to verify race condition
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act: Simulate a specific race condition scenario
            // This test demonstrates the race condition that can occur between checking UnregisteringFlag
            // and setting EventsRegisteredFlag in RegisterEvents
            var tasks = new List<Task>();

            // Create a scenario where multiple RegisterEvents calls happen concurrently
            // This can expose the race condition between the non-atomic checks
            for (var i = 0; i < 5; i++)
            {
                var task = Task.Run(() =>
                {
                    controller.RegisterEvents();
                });
                tasks.Add(task);
            }

            // Add some unregistration tasks as well to create mixed operations
            for (var i = 0; i < 3; i++)
            {
                var task = Task.Run(() =>
                {
                    controller.UnregisterEvents();
                });
                tasks.Add(task);
            }

            // Add timeout to prevent hanging indefinitely
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var whenAllTask = Task.WhenAll(tasks);

            var completedTask = await Task.WhenAny(whenAllTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Assert.Fail("RegisterEvents_WhenConcurrentRegisterEventsAndUnregisterEventsRaceConditionExists_LeadsToInconsistentState test timed out after 30 seconds");
            }

            // Wait for the actual tasks to complete if they haven't already
            await whenAllTask;

            // Get the final state
            var finalState = (int)(stateField?.GetValue(controller) ?? 0);
            var isEventsRegistered = (finalState & (1 << 0)) != 0; // EventsRegisteredFlag
            var isUnregistering = (finalState & (1 << 5)) != 0; // UnregisteringFlag

            // Invariants: unregistration must not be left "in-progress"
            Assert.False(isUnregistering, "UnregisteringFlag should be cleared after operations complete");

            // Handler leak invariant: we should never end up with removals exceeding adds
            Assert.True(threadSafeGameLoopEvents.GameLaunchedRemoveCount <= threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.True(threadSafeGameLoopEvents.SaveLoadedRemoveCount <= threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.True(threadSafeGameLoopEvents.SavingRemoveCount <= threadSafeGameLoopEvents.SavingAddCount);

            // If events are marked registered, each handler should only be registered once in the end-state contract.
            if (isEventsRegistered)
            {
                Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount - threadSafeGameLoopEvents.GameLaunchedRemoveCount);
                Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount - threadSafeGameLoopEvents.SaveLoadedRemoveCount);
                Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount - threadSafeGameLoopEvents.SavingRemoveCount);
            }
        }

        [Fact]
        public async Task RegisterEvents_WithMultipleConcurrentCalls_MayCauseDuplicateRegistrations()
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
                _mockSoilHealthService.Object, _mockSaveIdProvider.Object, _mockSoilHealthVisualizationService.Object);

            // Act: Simulate multiple concurrent registration attempts
            // This should reveal the race condition where multiple threads pass the
            // UnregisteringFlag check but before the EventsRegisteredFlag is set
            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                var task = Task.Run(() =>
                {
                    controller.RegisterEvents();
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // With the race condition, multiple registrations might occur
            // The current implementation should only register once due to TrySetStateFlag
            // but the race condition still exists in the pre-check logic

            // The issue is that the check for UnregisteringFlag and the attempt to set
            // EventsRegisteredFlag are not atomic, creating a potential race condition
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);
        }

    }
}
