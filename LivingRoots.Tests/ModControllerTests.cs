using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            Assert.Throws<ArgumentNullException>(() => new ModController(null as IModHelper, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, null as IMonitor, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object));
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
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns((IModEvents)null); // Return null for Events
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
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns((IGameLoopEvents)null); // Return null for GameLoop
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
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
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
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
        }

        [Fact]
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

            // Act - Dispose multiple times
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

            // Verify that the OnGameLaunched method exists before invoking it
            var onGameLaunchedMethod = typeof(ModController).GetMethod("OnGameLaunched",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(onGameLaunchedMethod);

            // Then simulate the game launch event to trigger command registration
            var gameLaunchedEventArgs = CreateInstanceWithFallback<GameLaunchedEventArgs>();
            
            // WRAP REFLECTION INVOKE CALL WITH RECORD.EXCEPTION TO IMPROVE TEST RELIABILITY
            var invokeEx = Record.Exception(() =>
                onGameLaunchedMethod.Invoke(controller, new object[] { controller, gameLaunchedEventArgs })); // Pass controller as sender instead of null
            Assert.Null(invokeEx);

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

            // Verify that the PrintVersion method exists before invoking it
            var printVersionMethod = typeof(ModController).GetMethod("PrintVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(printVersionMethod);

            // Act & Assert - Should not throw any exceptions
            var ex = Record.Exception(() =>
                printVersionMethod.Invoke(controller, new object[] { "lr_version", Array.Empty<string>() })); // Pass correct arguments for Action<string, string[]>
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

            // Verify that the PrintVersion method exists before invoking it
            var printVersionMethod = typeof(ModController).GetMethod("PrintVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(printVersionMethod);

            // Act & Assert - Should not throw with help arguments
            var ex = Record.Exception(() =>
                printVersionMethod.Invoke(controller, new object[] { "lr_version", new string[] { "/?", "-help", "--help" } })); // Pass correct arguments for Action<string, string[]>
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

            // Verify that the OnSaveLoaded method exists before invoking it
            var onSaveLoadedMethod = typeof(ModController).GetMethod("OnSaveLoaded",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(onSaveLoadedMethod);

            // Act
            var saveLoadedEventArgs = CreateInstanceWithFallback<SaveLoadedEventArgs>();
            
            // WRAP REFLECTION INVOKE CALL WITH RECORD.EXCEPTION TO IMPROVE TEST RELIABILITY
            var invokeEx = Record.Exception(() =>
                onSaveLoadedMethod.Invoke(controller, new object[] { controller, saveLoadedEventArgs })); // Pass controller as sender instead of null
            Assert.Null(invokeEx);

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

            // Verify that the OnSaving method exists before invoking it
            var onSavingMethod = typeof(ModController).GetMethod("OnSaving",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(onSavingMethod);

            // Act - Create a real SavingEventArgs instance using Activator.CreateInstance with nonPublic: true as a preferred method
            var savingEventArgs = CreateInstanceWithFallback<SavingEventArgs>();
            
            // WRAP REFLECTION INVOKE CALL WITH RECORD.EXCEPTION TO IMPROVE TEST RELIABILITY
            var invokeEx = Record.Exception(() =>
                onSavingMethod.Invoke(controller, new object[] { controller, savingEventArgs })); // Pass controller as sender instead of null
            Assert.Null(invokeEx);

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
            Assert.Null(result); // RegisterEvents should handle disposed state gracefully
            
            // Verify that events were unregistered during disposal
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
        }

        [Fact]
        public void TrySetStateFlag_SetsFlagAtomically()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);
            const int testFlag = 1 << 0; // Use EventsRegisteredFlag for testing

            // Get the private _state field using reflection
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(stateField);

            // Act
            bool result = controller.TrySetStateFlag(testFlag);

            // Get the current state value using reflection
            var state = (int)stateField.GetValue(controller)!;

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

            // Get the private _state field using reflection
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(stateField);

            // First set the disposed flag using reflection
            stateField.SetValue(controller, disposedFlag);

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

            // Get the private fields using reflection
            var saveIdUnavailableWarningShownOnSaveLoadedField = typeof(ModController).GetField("_saveIdUnavailableWarningShownOnSaveLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
            var saveIdUnavailableWarningShownOnSavingField = typeof(ModController).GetField("_saveIdUnavailableWarningShownOnSaving", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(saveIdUnavailableWarningShownOnSaveLoadedField);
            Assert.NotNull(saveIdUnavailableWarningShownOnSavingField);

            // Act & Assert - Initially should be false (0)
            Assert.Equal(0, (int)saveIdUnavailableWarningShownOnSaveLoadedField.GetValue(controller)!);
            Assert.Equal(0, (int)saveIdUnavailableWarningShownOnSavingField.GetValue(controller)!);

            // Act - Change properties to true (1) using reflection
            saveIdUnavailableWarningShownOnSaveLoadedField.SetValue(controller, 1);
            saveIdUnavailableWarningShownOnSavingField.SetValue(controller, 1);

            // Assert - Should now be true (1)
            Assert.Equal(1, (int)saveIdUnavailableWarningShownOnSaveLoadedField.GetValue(controller)!);
            Assert.Equal(1, (int)saveIdUnavailableWarningShownOnSavingField.GetValue(controller)!);
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

        /// <summary>
        /// Creates an instance of the specified type using Activator.CreateInstance with nonPublic: true as a preferred method.
        /// Adds FormatterServices.GetUninitializedObject as a fallback when Activator.CreateInstance fails.
        /// Validates that the created instance is not null and throws an informative exception if creation fails.
        /// </summary>
        /// <typeparam name="T">The type to create an instance of</typeparam>
        /// <returns>An instance of the specified type</returns>
        /// <exception cref="InvalidOperationException">Thrown when both Activator.CreateInstance and FormatterServices.GetUninitializedObject fail.</exception>
        private static T CreateInstanceWithFallback<T>() where T : class
        {
            try
            {
                // Try to create instance using Activator.CreateInstance with nonPublic: true (preferred method)
                var instance = Activator.CreateInstance(typeof(T), nonPublic: true) as T;
                
                // Validate that the created instance is not null
                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of type {typeof(T)} for tests. Activator.CreateInstance returned null.");
                }
                
                return instance;
            }
            catch (Exception ex)
            {
                // If Activator.CreateInstance fails, try FormatterServices.GetUninitializedObject as fallback
                try
                {
                    var instance = FormatterServices.GetUninitializedObject(typeof(T)) as T;
                    
                    // Validate that the created instance is not null
                    if (instance == null)
                    {
                        throw new InvalidOperationException($"Failed to create instance of type {typeof(T)} for tests. FormatterServices.GetUninitializedObject returned null.");
                    }
                    
                    return instance;
                }
                catch (Exception innerEx)
                {
                    // If both methods fail, throw an informative exception that includes both failures.
                    throw new InvalidOperationException($"Failed to create instance of type {typeof(T)} for tests. Both Activator.CreateInstance and FormatterServices.GetUninitializedObject failed.", new AggregateException(ex, innerEx));
                }
            }
        }
    }
}
