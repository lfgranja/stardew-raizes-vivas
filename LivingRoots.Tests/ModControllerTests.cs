using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
    public class ModControllerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;

        public ModControllerTests()
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
        public void Constructor_WithNullHelper_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(null!, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, null!, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullManifest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, (IManifest)null!, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullModDataService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, (IModDataService)null!, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullSoilHealthService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, (ISoilHealthService)null!, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullSaveIdProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, (ISaveIdProvider)null!));
        }

        [Fact]
        public void RegisterEvents_WithValidController_DoesNotThrow()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Should not throw when controller is properly constructed
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
        }

        [Fact]
        public void RegisterEvents_WithNullEvents_DoesNotThrow()
        {
            // Arrange
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns((IModEvents)null!); // Return null for Events
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Should not throw when Events is null
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
        }

        [Fact]
        public void RegisterEvents_WithNullGameLoop_DoesNotThrow()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns((IGameLoopEvents)null!); // Return null for GameLoop
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Should not throw when GameLoop is null
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterEvents_IsThreadSafe()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var threadSafeGameLoopEvents = new ThreadSafeGameLoopEventsStub();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(threadSafeGameLoopEvents);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Create a single ModController instance to be shared across all tasks
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act - Simulate concurrent registration attempts on the same instance
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    // All tasks call RegisterEvents on the same controller instance
                    controller.RegisterEvents();
                });
                tasks.Add(task);
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // Verify that events were registered only once despite multiple concurrent calls
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedAddCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingAddCount);
        }

        [Fact]
        public void RegisterEvents_WhenExceptionOccurs_HandlingIsSecure()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup to throw an exception when trying to add an event
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new Exception("Test exception"));

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Should handle exception gracefully and not propagate
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex); // Exception should be caught and handled, not thrown
        }

        [Fact]
        public void UnregisterEvents_RemovesAllEvents()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // SetupAdd and SetupRemove for events to ensure VerifyRemove works reliably
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>());

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to set up the controller
            controller.RegisterEvents();

            // Act
            controller.UnregisterEvents();

            // Assert
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
        }

        [Fact]
        public void UnregisterEvents_WhenNotRegistered_DoesNotUnsubscribe()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act
            controller.UnregisterEvents();

            // Assert - Events should not be unsubscribed if they were never registered
            // Verify that no unsubscription methods were called since events were never registered
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Never);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Never);
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task UnregisterEvents_IsThreadSafe()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var threadSafeGameLoopEvents = new ThreadSafeGameLoopEventsStub();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(threadSafeGameLoopEvents);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to set up the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent unregistration attempts
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    controller.UnregisterEvents();
                });
                tasks.Add(task);
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // Additionally, verify that all events were properly removed (only once due to thread safety)
            Assert.Equal(1, threadSafeGameLoopEvents.GameLaunchedRemoveCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SaveLoadedRemoveCount);
            Assert.Equal(1, threadSafeGameLoopEvents.SavingRemoveCount);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("csharpsquid", "S3966", Justification = "Testing idempotency - multiple Dispose calls are intentional")]
        public void Dispose_IsIdempotent_CanBeCalledMultipleTimes()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Add explicit SetupAdd and SetupRemove for all events to ensure Moq reliably tracks event subscriptions and unsubscriptions
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>());

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to initialize the controller
            controller.RegisterEvents();

            // Act - Dispose multiple times to verify idempotency
            // Note: Not using 'using' statement here to explicitly test multiple Dispose() calls
            controller.Dispose();
            controller.Dispose(); // Second call should not cause issues
            controller.Dispose(); // Third call should not cause issues

            // Assert - No exceptions should be thrown
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }

        [Fact]
        public async System.Threading.Tasks.Task Dispose_IsThreadSafe_WithConcurrentDisposal()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Add explicit SetupAdd and SetupRemove for all events to ensure Moq reliably tracks event subscriptions and unsubscriptions
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>());

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // First register events to initialize the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent disposal from multiple threads
            var tasks = new List<System.Threading.Tasks.Task>();
            for (int i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() => controller.Dispose());
                tasks.Add(task);
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // The event should be removed only once despite multiple concurrent disposal attempts
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }

        [Fact]
        public void RegisterConsoleCommand_AddsCommandSuccessfully()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Act - Trigger the OnGameLaunched event by raising it on the mock
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, new GameLaunchedEventArgs());

            // Assert - Command should have been added to console commands
            mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
        }

        [Fact]
        public void PrintVersion_ExecutesWithoutErrors()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Arrange - Capture the registered command action BEFORE raising event
            Action<string, string[]>? printVersionAction = null;
            mockCommandHelper
                .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
                .Callback<string, string, Action<string, string[]>>((name, desc, action) => printVersionAction = action);

            // Act - Trigger the OnGameLaunched event to register the command
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, new GameLaunchedEventArgs());

            Assert.NotNull(printVersionAction);

            // Act - Execute the captured action
            var ex = Record.Exception(() => printVersionAction("lr_version", Array.Empty<string>()));

            // Assert - Should not throw any exceptions
            Assert.Null(ex);
        }

        [Fact]
        public void PrintVersion_WithHelpArguments_PrintsUsage()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Arrange - Capture the registered command action BEFORE raising event
            Action<string, string[]>? printVersionAction = null;
            mockCommandHelper
                .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
                .Callback<string, string, Action<string, string[]>>((name, desc, action) => printVersionAction = action);

            // Act - Trigger the OnGameLaunched event to register the command
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, new GameLaunchedEventArgs());

            Assert.NotNull(printVersionAction);

            // Act - Execute the captured action with help arguments
            var ex = Record.Exception(() => printVersionAction("lr_version", new string[] { "/?", "-help", "--help" }));

            // Assert - Should not throw with help arguments
            Assert.Null(ex);
        }

        [Fact]
        public void OnSaveLoaded_WithValidSaveId_LoadsData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock the save ID provider to return a valid save ID
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns("test_save_id");

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events to ensure proper setup before calling OnSaveLoaded
            controller.RegisterEvents();

            // Act - Raise the SaveLoaded event to trigger OnSaveLoaded
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());

            // Assert - Soil health service should have been called to load data
            _mockSoilHealthService.Verify(x => x.LoadData("test_save_id"), Times.Once);
        }

        [Fact]
        public void OnSaving_WithValidSaveId_SavesData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock the save ID provider to return a valid save ID
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns("test_save_id");

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events to ensure proper setup before calling OnSaving
            controller.RegisterEvents();

            // Act - Raise the Saving event to trigger OnSaving
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Soil health service should have been called to save data
            _mockSoilHealthService.Verify(x => x.SaveData("test_save_id"), Times.Once);
        }


        [Fact]
        public void IsDisposed_ReturnsCorrectState()
        {
            // Arrange
            var mockCommandHelper = new Mock<ICommandHelper>();
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Initially should not be disposed
            // Verify that controller can be used before disposal
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);

            // Add explicit SetupAdd and SetupRemove for GameLaunched event to ensure Moq reliably tracks event subscriptions
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());

            // Register events to verify controller is functional before disposal
            controller.RegisterEvents();
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);

            // Act - Dispose the controller using public Dispose method
            controller.Dispose();

            // Assert - After disposal, attempting to register events should not succeed
            // This verifies that the controller is in a disposed state
            var result = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(result); // Should not throw

            // Verify that events were unregistered during disposal using reflection to access internal state
            var stateField = typeof(ModController).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var stateValue = (int)(stateField?.GetValue(controller) ?? 0); // Using null-coalescing to handle potential null
            var disposedFlag = 1 << 2; // DisposedFlag constant value
            Assert.True((stateValue & disposedFlag) != 0, "Controller should be marked as disposed after calling Dispose()");

            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
        }

        [Fact]
        public void TrySetStateFlag_SetsFlagAtomically()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);
            const int testFlag = 1; // Use EventsRegisteredFlag for testing

            // Act
            bool result = controller.TrySetStateFlag(testFlag);

            // Get the current state value directly (internal member accessible via InternalsVisibleTo)
            var state = controller._state;

            // Assert - Should successfully set the flag
            Assert.True(result);
            Assert.True((state & testFlag) != 0);

            // Act - Try to set the same flag again
            bool result2 = controller.TrySetStateFlag(testFlag);

            // Assert - Should return false since the flag is already set
            Assert.False(result2);
        }

        [Fact]
        public void TrySetStateFlag_WhenDisposed_ReturnsFalseForOtherFlags()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);
            const int disposedFlag = 1 << 2; // Use DisposedFlag
            const int eventsRegisteredFlag = 1 << 0; // Use EventsRegisteredFlag

            // First set the disposed flag directly (internal member accessible via InternalsVisibleTo)
            controller._state = disposedFlag;

            // Act - Try to set another flag when disposed
            bool result = controller.TrySetStateFlag(eventsRegisteredFlag);

            // Assert - Should return false when trying to set other flags when disposed
            Assert.False(result);
        }

        [Fact]
        public void GetSaveIdUnavailableWarningShownOnSaveLoadedProperty_WorksCorrectly()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Initially should be false (0)
            Assert.Equal(0, controller._saveIdUnavailableWarningShownOnSaveLoaded);
            Assert.Equal(0, controller._saveIdUnavailableWarningShownOnSaving);

            // Act - Change properties to true (1) directly (internal members accessible via InternalsVisibleTo)
            controller._saveIdUnavailableWarningShownOnSaveLoaded = 1;
            controller._saveIdUnavailableWarningShownOnSaving = 1;

            // Assert - Should now be true (1)
            Assert.Equal(1, controller._saveIdUnavailableWarningShownOnSaveLoaded);
            Assert.Equal(1, controller._saveIdUnavailableWarningShownOnSaving);
        }

    }
}
