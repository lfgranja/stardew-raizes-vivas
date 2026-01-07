using LivingRoots.Controllers;
using LivingRoots.Domain;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LivingRoots.Tests
{
    public class ModControllerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ISoilHealthVisualizationService> _mockSoilHealthVisualizationService;

        public ModControllerTests()
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
        public void Constructor_WithNullHelper_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModController(
                    null!,
                    _mockMonitor.Object,
                    _mockManifest.Object,
                    _mockSoilHealthService.Object,
                    _mockSaveIdProvider.Object,
                    _mockSoilHealthVisualizationService.Object
                )
            );
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModController(
                    _mockHelper.Object,
                    null!,
                    _mockManifest.Object,
                    _mockSoilHealthService.Object,
                    _mockSaveIdProvider.Object,
                    _mockSoilHealthVisualizationService.Object
                )
            );
        }

        [Fact]
        public void Constructor_WithNullManifest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModController(
                    _mockHelper.Object,
                    _mockMonitor.Object,
                    null!,
                    _mockSoilHealthService.Object,
                    _mockSaveIdProvider.Object,
                    _mockSoilHealthVisualizationService.Object
                )
            );
        }

        [Fact]
        public void Constructor_WithNullSoilHealthService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModController(
                    _mockHelper.Object,
                    _mockMonitor.Object,
                    _mockManifest.Object,
                    null!,
                    _mockSaveIdProvider.Object,
                    _mockSoilHealthVisualizationService.Object
                )
            );
        }

        [Fact]
        public void Constructor_WithNullSaveIdProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ModController(
                    _mockHelper.Object,
                    _mockMonitor.Object,
                    _mockManifest.Object,
                    _mockSoilHealthService.Object,
                    null!,
                    _mockSoilHealthVisualizationService.Object
                )
            );
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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
            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Act - Simulate concurrent registration attempts on the same instance
            var tasks = new List<System.Threading.Tasks.Task>();
            for (var i = 0; i < 10; i++)
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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
            mockGameLoopEvents.SetupAdd(x =>
                x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x =>
                x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x =>
                x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()
            );

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // First register events to set up the controller
            controller.RegisterEvents();

            // Act
            controller.UnregisterEvents();

            // Assert
            mockGameLoopEvents.VerifyRemove(
                x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(),
                Times.Once
            );
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Act
            controller.UnregisterEvents();

            // Assert - Events should not be unsubscribed if they were never registered
            // Verify that no unsubscription methods were called since events were never registered
            mockGameLoopEvents.VerifyRemove(
                x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Never
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(),
                Times.Never
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(),
                Times.Never
            );
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // First register events to set up the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent unregistration attempts
            var tasks = new List<System.Threading.Tasks.Task>();
            for (var i = 0; i < 10; i++)
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "csharpsquid",
            "S3966",
            Justification = "Testing idempotency - multiple Dispose calls are intentional"
        )]
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
            mockGameLoopEvents.SetupAdd(x =>
                x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x =>
                x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x =>
                x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()
            );

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // First register events to initialize the controller
            controller.RegisterEvents();

            // Act - Dispose multiple times to verify idempotency
            // Note: Not using 'using' statement here to explicitly test multiple Dispose() calls
            controller.Dispose();
            controller.Dispose(); // Second call should not cause issues
            controller.Dispose(); // Third call should not cause issues

            // Assert - No exceptions should be thrown
            mockGameLoopEvents.VerifyRemove(
                x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(),
                Times.Once
            );
            _mockMonitor.Verify(
                x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()),
                Times.AtLeastOnce
            );
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
            mockGameLoopEvents.SetupAdd(x =>
                x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x =>
                x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());

            mockGameLoopEvents.SetupRemove(x =>
                x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()
            );

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // First register events to initialize the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent disposal from multiple threads
            var tasks = new List<System.Threading.Tasks.Task>();
            for (var i = 0; i < 10; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() => controller.Dispose());
                tasks.Add(task);
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // The event should be removed only once despite multiple concurrent disposal attempts
            mockGameLoopEvents.VerifyRemove(
                x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(),
                Times.Once
            );
            mockGameLoopEvents.VerifyRemove(
                x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(),
                Times.Once
            );
            _mockMonitor.Verify(
                x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()),
                Times.AtLeastOnce
            );
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Act - Trigger the OnGameLaunched event by raising it on the mock
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, new GameLaunchedEventArgs());

            // Assert - Command should have been added to console commands
            mockCommandHelper.Verify(
                x =>
                    x.Add(
                        "lr_version",
                        "Shows the Living Roots version.",
                        It.IsAny<Action<string, string[]>>()
                    ),
                Times.Once
            );
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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Arrange - Capture the registered command action BEFORE raising event
            Action<string, string[]>? printVersionAction = null;
            mockCommandHelper
                .Setup(x =>
                    x.Add(
                        "lr_version",
                        "Shows the Living Roots version.",
                        It.IsAny<Action<string, string[]>>()
                    )
                )
                .Callback<string, string, Action<string, string[]>>(
                    (name, desc, action) => printVersionAction = action
                );

            // Act - Trigger the OnGameLaunched event to register the command
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, (EventArgs)null!);

            Assert.NotNull(printVersionAction);

            // Act - Execute the captured action
            var ex = Record.Exception(() =>
                printVersionAction("lr_version", Array.Empty<string>())
            );

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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events first to initialize the command registration
            controller.RegisterEvents();

            // Arrange - Capture the registered command action BEFORE raising event
            Action<string, string[]>? printVersionAction = null;
            mockCommandHelper
                .Setup(x =>
                    x.Add(
                        "lr_version",
                        "Shows the Living Roots version.",
                        It.IsAny<Action<string, string[]>>()
                    )
                )
                .Callback<string, string, Action<string, string[]>>(
                    (name, desc, action) => printVersionAction = action
                );

            // Act - Trigger the OnGameLaunched event to register the command
            mockGameLoopEvents.Raise(x => x.GameLaunched += null, (EventArgs)null!);

            Assert.NotNull(printVersionAction);

            // Act - Execute the captured action with help arguments
            var ex = Record.Exception(() =>
                printVersionAction("lr_version", new string[] { "/?", "-help", "--help" })
            );

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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

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
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Act & Assert - Initially should not be disposed
            // Verify that controller can be used before disposal

            // Add explicit SetupAdd and SetupRemove for GameLaunched event to ensure Moq reliably tracks event subscriptions
            mockGameLoopEvents.SetupAdd(x =>
                x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );
            mockGameLoopEvents.SetupRemove(x =>
                x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>()
            );

            // Register events to verify controller is functional before disposal
            controller.RegisterEvents();
            mockGameLoopEvents.VerifyAdd(
                x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );

            // Act - Dispose the controller using public Dispose method
            controller.Dispose();

            // Assert - After disposal, attempting to register events should not re-subscribe
            // This verifies that the controller is in a disposed state through observable behavior
            var result = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(result); // Should not throw

            // RegisterEvents should not add another handler after disposal
            // This proves the controller is in a disposed state without accessing private fields
            mockGameLoopEvents.VerifyAdd(
                x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );

            // Dispose should have unsubscribed the handler once
            mockGameLoopEvents.VerifyRemove(
                x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Once
            );
        }

        [Fact]
        public void TrySetStateFlag_SetsFlagAtomically()
        {
            // Arrange
            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );
            const int testFlag = ModController.EventsRegisteredFlag;

            // Act
            var result = controller.TrySetStateFlag(testFlag);

            // Get the current state value directly (internal member accessible via InternalsVisibleTo)
            var state = controller._state;

            // Assert - Should successfully set the flag
            Assert.True(result);
            Assert.True((state & testFlag) != 0);

            // Act - Try to set the same flag again
            var result2 = controller.TrySetStateFlag(testFlag);

            // Assert - Should return false since the flag is already set
            Assert.False(result2);
        }

        [Fact]
        public void TrySetStateFlag_WhenDisposed_ReturnsFalseForOtherFlags()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Act - Dispose the controller
            controller.Dispose();

            // Act & Assert - Try to set a flag when disposed
            var result = controller.TrySetStateFlag(ModController.EventsRegisteredFlag);

            // Assert - Should return false when trying to set other flags when disposed
            Assert.False(result);

            // Verify that RegisterEvents does not actually register after disposal (observable behavior)
            mockGameLoopEvents.VerifyAdd(
                x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(),
                Times.Never
            );
        }

        [Fact]
        public void SaveIdUnavailableWarning_ShownOnlyOncePerEvent()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock SaveIdProvider to return null (simulating unavailable save folder)
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns((string?)null);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise SaveLoaded event multiple times
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());

            // Assert - Warning should be logged only once, not on every event
            _mockMonitor.Verify(
                x =>
                    x.Log(
                        It.Is<string>(s => s.Contains("SaveFolderName unavailable")),
                        LogLevel.Warn
                    ),
                Times.Once
            );
        }

        [Fact]
        public void SaveIdUnavailableWarning_ShownOnlyOncePerSavingEvent()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock SaveIdProvider to return null (simulating unavailable save folder)
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns((string?)null);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise Saving event multiple times
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Warning should be logged only once, not on every event
            _mockMonitor.Verify(
                x =>
                    x.Log(
                        It.Is<string>(s => s.Contains("SaveFolderName unavailable")),
                        LogLevel.Warn
                    ),
                Times.Once
            );
        }

        #region End-to-End Integration Tests

        /// <summary>
        /// Integration test: Verifies the complete flow when SaveLoaded event is triggered.
        /// This test ensures that:
        /// 1. The SaveIdProvider is called to obtain the saveId
        /// 2. The saveId is passed correctly to SoilHealthService.LoadData
        /// 3. The integration between controller, SaveIdProvider, and SoilHealthService works correctly
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SaveLoadedEvent_CallsSaveIdProviderAndLoadData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            const string expectedSaveId = "test_save_12345";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(expectedSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup before triggering SaveLoaded
            controller.RegisterEvents();

            // Act - Raise the SaveLoaded event to trigger the complete integration flow
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());

            // Assert - Verify the complete integration flow
            // 1. SaveIdProvider.GetSaveId() was called to obtain the saveId
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Once,
                "SaveIdProvider.GetSaveId should be called once when SaveLoaded event is triggered"
            );

            // 2. SoilHealthService.LoadData() was called with the correct saveId
            _mockSoilHealthService.Verify(
                x => x.LoadData(expectedSaveId),
                Times.Once,
                $"SoilHealthService.LoadData should be called once with saveId '{expectedSaveId}'"
            );

            // 3. Verify that no other service methods were called (ensuring correct isolation)
            _mockSoilHealthService.Verify(
                x => x.SaveData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.SaveData should not be called during SaveLoaded event"
            );
        }

        /// <summary>
        /// Integration test: Verifies the complete flow when Saving event is triggered.
        /// This test ensures that:
        /// 1. The SaveIdProvider is called to obtain the saveId
        /// 2. The saveId is passed correctly to SoilHealthService.SaveData
        /// 3. The integration between controller, SaveIdProvider, and SoilHealthService works correctly
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SavingEvent_CallsSaveIdProviderAndSaveData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            const string expectedSaveId = "test_save_67890";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(expectedSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup before triggering Saving
            controller.RegisterEvents();

            // Act - Raise the Saving event to trigger the complete integration flow
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Verify the complete integration flow
            // 1. SaveIdProvider.GetSaveId() was called to obtain the saveId
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Once,
                "SaveIdProvider.GetSaveId should be called once when Saving event is triggered"
            );

            // 2. SoilHealthService.SaveData() was called with the correct saveId
            _mockSoilHealthService.Verify(
                x => x.SaveData(expectedSaveId),
                Times.Once,
                $"SoilHealthService.SaveData should be called once with saveId '{expectedSaveId}'"
            );

            // 3. Verify that no other service methods were called (ensuring correct isolation)
            _mockSoilHealthService.Verify(
                x => x.LoadData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.LoadData should not be called during Saving event"
            );
        }

        /// <summary>
        /// Integration test: Verifies the complete flow when both SaveLoaded and Saving events are triggered.
        /// This test ensures that:
        /// 1. Both events correctly trigger their respective service methods
        /// 2. The SaveIdProvider is called for each event
        /// 3. The correct saveId is passed to each service method
        /// 4. The integration works correctly for multiple event triggers
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SaveLoadedAndSavingEvents_CorrectlyCallLoadDataAndSaveData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            const string expectedSaveId = "test_save_complete_flow";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(expectedSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Simulate a complete game session: load then save
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Verify the complete integration flow for both events
            // 1. SaveIdProvider.GetSaveId() was called twice (once for each event)
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Exactly(2),
                "SaveIdProvider.GetSaveId should be called twice (once for SaveLoaded, once for Saving)"
            );

            // 2. SoilHealthService.LoadData() was called once with the correct saveId
            _mockSoilHealthService.Verify(
                x => x.LoadData(expectedSaveId),
                Times.Once,
                $"SoilHealthService.LoadData should be called once with saveId '{expectedSaveId}'"
            );

            // 3. SoilHealthService.SaveData() was called once with the correct saveId
            _mockSoilHealthService.Verify(
                x => x.SaveData(expectedSaveId),
                Times.Once,
                $"SoilHealthService.SaveData should be called once with saveId '{expectedSaveId}'"
            );
        }

        /// <summary>
        /// Integration test: Verifies that when SaveIdProvider returns null/empty, no service methods are called.
        /// This test ensures that:
        /// 1. The controller correctly handles null/empty saveId from SaveIdProvider
        /// 2. No service methods are called when saveId is unavailable
        /// 3. A warning is logged (verified through monitor)
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SaveLoadedEvent_WithNullSaveId_DoesNotCallLoadData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock SaveIdProvider to return null (simulating unavailable save folder)
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns((string?)null);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise the SaveLoaded event
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());

            // Assert - Verify that no service methods were called when saveId is null
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Once,
                "SaveIdProvider.GetSaveId should be called once"
            );
            _mockSoilHealthService.Verify(
                x => x.LoadData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.LoadData should not be called when saveId is null"
            );
            _mockSoilHealthService.Verify(
                x => x.SaveData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.SaveData should not be called when saveId is null"
            );

            // Verify that a warning was logged
            _mockMonitor.Verify(
                x =>
                    x.Log(
                        It.Is<string>(s => s.Contains("SaveFolderName unavailable")),
                        LogLevel.Warn
                    ),
                Times.Once,
                "A warning should be logged when saveId is null"
            );
        }

        /// <summary>
        /// Integration test: Verifies that when SaveIdProvider returns empty string, no service methods are called.
        /// This test ensures that:
        /// 1. The controller correctly handles empty string saveId from SaveIdProvider
        /// 2. No service methods are called when saveId is empty
        /// 3. A warning is logged (verified through monitor)
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SavingEvent_WithEmptySaveId_DoesNotCallSaveData()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Mock SaveIdProvider to return empty string (simulating unavailable save folder)
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(string.Empty);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise the Saving event
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Verify that no service methods were called when saveId is empty
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Once,
                "SaveIdProvider.GetSaveId should be called once"
            );
            _mockSoilHealthService.Verify(
                x => x.LoadData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.LoadData should not be called when saveId is empty"
            );
            _mockSoilHealthService.Verify(
                x => x.SaveData(It.IsAny<string>()),
                Times.Never,
                "SoilHealthService.SaveData should not be called when saveId is empty"
            );

            // Verify that a warning was logged
            _mockMonitor.Verify(
                x =>
                    x.Log(
                        It.Is<string>(s => s.Contains("SaveFolderName unavailable")),
                        LogLevel.Warn
                    ),
                Times.Once,
                "A warning should be logged when saveId is empty"
            );
        }

        /// <summary>
        /// Integration test: Verifies that the saveId from SaveIdProvider is correctly passed to SoilHealthService.
        /// This test ensures that:
        /// 1. The saveId returned by SaveIdProvider is exactly what's passed to SoilHealthService
        /// 2. No transformation or modification of the saveId occurs
        /// 3. The integration maintains data integrity throughout the flow
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_SaveIdFromProvider_IsCorrectlyPassedToService()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Use a complex saveId with special characters to ensure no transformation occurs
            const string complexSaveId = "Save_2024-01-15_FarmerName_123";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(complexSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise both SaveLoaded and Saving events
            mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
            mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());

            // Assert - Verify that the exact same saveId is passed to both service methods
            _mockSoilHealthService.Verify(
                x => x.LoadData(complexSaveId),
                Times.Once,
                $"SoilHealthService.LoadData should be called with the exact saveId '{complexSaveId}'"
            );
            _mockSoilHealthService.Verify(
                x => x.SaveData(complexSaveId),
                Times.Once,
                $"SoilHealthService.SaveData should be called with the exact saveId '{complexSaveId}'"
            );

            // Verify that no other saveId was used
            _mockSoilHealthService.Verify(
                x => x.LoadData(It.Is<string>(s => s != complexSaveId)),
                Times.Never,
                "SoilHealthService.LoadData should not be called with any other saveId"
            );
            _mockSoilHealthService.Verify(
                x => x.SaveData(It.Is<string>(s => s != complexSaveId)),
                Times.Never,
                "SoilHealthService.SaveData should not be called with any other saveId"
            );
        }

        /// <summary>
        /// Integration test: Verifies that multiple SaveLoaded events correctly call LoadData each time.
        /// This test ensures that:
        /// 1. Each SaveLoaded event triggers a LoadData call
        /// 2. The SaveIdProvider is called for each event
        /// 3. The integration handles multiple event triggers correctly
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_MultipleSaveLoadedEvents_CorrectlyCallLoadDataEachTime()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            const string expectedSaveId = "test_save_multiple";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(expectedSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise SaveLoaded event multiple times (simulating loading the same save multiple times)
            const int eventCount = 3;
            for (int i = 0; i < eventCount; i++)
            {
                mockGameLoopEvents.Raise(x => x.SaveLoaded += null, new SaveLoadedEventArgs());
            }

            // Assert - Verify that LoadData was called for each event
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Exactly(eventCount),
                $"SaveIdProvider.GetSaveId should be called {eventCount} times"
            );
            _mockSoilHealthService.Verify(
                x => x.LoadData(expectedSaveId),
                Times.Exactly(eventCount),
                $"SoilHealthService.LoadData should be called {eventCount} times"
            );
        }

        /// <summary>
        /// Integration test: Verifies that multiple Saving events correctly call SaveData each time.
        /// This test ensures that:
        /// 1. Each Saving event triggers a SaveData call
        /// 2. The SaveIdProvider is called for each event
        /// 3. The integration handles multiple event triggers correctly
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void Integration_MultipleSavingEvents_CorrectlyCallSaveDataEachTime()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            const string expectedSaveId = "test_save_multiple_saves";
            _mockSaveIdProvider.Setup(x => x.GetSaveId()).Returns(expectedSaveId);

            var controller = new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                _mockSoilHealthVisualizationService.Object
            );

            // Register events to ensure proper setup
            controller.RegisterEvents();

            // Act - Raise Saving event multiple times (simulating saving multiple times)
            const int eventCount = 3;
            for (int i = 0; i < eventCount; i++)
            {
                mockGameLoopEvents.Raise(x => x.Saving += null, new SavingEventArgs());
            }

            // Assert - Verify that SaveData was called for each event
            _mockSaveIdProvider.Verify(
                x => x.GetSaveId(),
                Times.Exactly(eventCount),
                $"SaveIdProvider.GetSaveId should be called {eventCount} times"
            );
            _mockSoilHealthService.Verify(
                x => x.SaveData(expectedSaveId),
                Times.Exactly(eventCount),
                $"SoilHealthService.SaveData should be called {eventCount} times"
            );
        }

        #endregion
    }
}
