using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
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
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
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

            // Setup to throw an exception when trying to add the event
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
            onGameLaunchedMethod.Invoke(controller, new object[] { controller, gameLaunchedEventArgs }); // Pass controller as sender instead of null

            // Assert - Command should have been added to the console commands
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
                printVersionMethod.Invoke(controller, new object[] { controller, new string[] { } })); // Pass controller as sender instead of null
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
                printVersionMethod.Invoke(controller, new object[] { controller, new string[] { "/?", "-help", "--h" } })); // Pass controller as sender instead of null
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
            onSaveLoadedMethod.Invoke(controller, new object[] { controller, saveLoadedEventArgs }); // Pass controller as sender instead of null

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
            onSavingMethod.Invoke(controller, new object[] { controller, savingEventArgs }); // Pass controller as sender instead of null

            // Assert - Soil health service should have been called to save data
            _mockSoilHealthService.Verify(x => x.SaveData("test_save_id"), Times.Once);
        }

        [Fact]
        public void IsDisposed_ReturnsCorrectState()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Initially should return false
            Assert.False(PrivateMethodHelper.IsDisposed(controller));

            // Act & Assert - After disposal should return true
            controller.Dispose();
            Assert.True(PrivateMethodHelper.IsDisposed(controller));
        }

        [Fact]
        public void TrySetStateFlag_SetsFlagAtomically()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);
            const int testFlag = 1 << 0; // Use EventsRegisteredFlag for testing

            // Act
            bool result = PrivateMethodHelper.TrySetStateFlag(controller, testFlag);

            // Assert - Should successfully set the flag
            Assert.True(result);
            Assert.True(PrivateMethodHelper.HasStateFlag(controller, testFlag));

            // Act - Try to set the same flag again
            bool result2 = PrivateMethodHelper.TrySetStateFlag(controller, testFlag);

            // Assert - Should return false since flag is already set
            Assert.False(result2);
        }

        [Fact]
        public void TrySetStateFlag_WhenDisposed_ReturnsFalseForOtherFlags()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);
            const int disposedFlag = 1 << 2; // Use DisposedFlag
            const int eventsRegisteredFlag = 1 << 0; // Use EventsRegisteredFlag

            // First set the disposed flag
            PrivateMethodHelper.SetStateFlag(controller, disposedFlag);

            // Act - Try to set another flag when disposed
            bool result = PrivateMethodHelper.TrySetStateFlag(controller, eventsRegisteredFlag);

            // Assert - Should return false when trying to set other flags when disposed
            Assert.False(result);
        }

        [Fact]
        public void GetSaveIdUnavailableWarningShownOnSaveLoadedProperty_WorksCorrectly()
        {
            // Arrange
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Act & Assert - Initially should be false (0)
            Assert.Equal(0, PrivateMethodHelper.GetSaveIdUnavailableWarningShownOnSaveLoaded(controller));
            Assert.Equal(0, PrivateMethodHelper.GetSaveIdUnavailableWarningShownOnSaving(controller));

            // Act - Change the properties to true (1)
            PrivateMethodHelper.SetSaveIdUnavailableWarningShownOnSaveLoaded(controller, 1);
            PrivateMethodHelper.SetSaveIdUnavailableWarningShownOnSaving(controller, 1);

            // Assert - Should now be true (1)
            Assert.Equal(1, PrivateMethodHelper.GetSaveIdUnavailableWarningShownOnSaveLoaded(controller));
            Assert.Equal(1, PrivateMethodHelper.GetSaveIdUnavailableWarningShownOnSaving(controller));
        }
        
        /// <summary>
        /// Creates an instance of the specified type using Activator.CreateInstance with nonPublic: true as a preferred method.
        /// </summary>
        /// <typeparam name="T">The type to create an instance of</typeparam>
        /// <returns>An instance of the specified type</returns>
        private static T CreateInstanceWithFallback<T>() where T : class
        {
            try
            {
                // Try to create instance using Activator.CreateInstance with nonPublic: true (preferred method)
                return (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            }
            catch (Exception ex)
            {
                // If creation fails, throw an informative exception
                throw new InvalidOperationException($"Failed to create instance of type {typeof(T)} using Activator.CreateInstance: {ex.Message}", ex);
            }
        }
    }
    
    // Helper class to access private methods and properties for testing
    internal static class PrivateMethodHelper
    {
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // Helper methods for accessing private methods/fields/properties
        public static bool IsDisposed(object controller)
        {
            var method = controller.GetType().GetMethod("IsDisposed", Flags);
            if (method == null)
                throw new InvalidOperationException("Expected private method 'IsDisposed' was not found.");
            return (bool)method.Invoke(controller, Array.Empty<object>());
        }

        public static bool TrySetStateFlag(object controller, int flag)
        {
            var method = controller.GetType().GetMethod("TrySetStateFlag", Flags);
            if (method == null)
                throw new InvalidOperationException("Expected private method 'TrySetStateFlag' was not found.");
            return (bool)method.Invoke(controller, new object[] { flag });
        }

        public static void SetStateFlag(object controller, int flag)
        {
            // Use reflection to access the _state field directly to set the flag
            var stateField = controller.GetType().GetField("_state", Flags);
            if (stateField == null)
                throw new InvalidOperationException("Expected private field '_state' was not found.");
            var currentValue = (int)stateField.GetValue(controller);
            stateField.SetValue(controller, currentValue | flag);
        }

        public static bool HasStateFlag(object controller, int flag)
        {
            // Use reflection to access the _state field directly to check the flag
            var stateField = controller.GetType().GetField("_state", Flags);
            if (stateField == null)
                throw new InvalidOperationException("Expected private field '_state' was not found.");
            var currentValue = (int)stateField.GetValue(controller);
            return (currentValue & flag) != 0;
        }

        public static int GetSaveIdUnavailableWarningShownOnSaveLoaded(object controller)
        {
            // Access the private field _saveIdUnavailableWarningShownOnSaveLoaded
            var field = controller.GetType().GetField("_saveIdUnavailableWarningShownOnSaveLoaded", Flags);
            if (field == null)
                throw new InvalidOperationException("Expected private field '_saveIdUnavailableWarningShownOnSaveLoaded' was not found.");
            return (int)field.GetValue(controller);
        }

        public static int GetSaveIdUnavailableWarningShownOnSaving(object controller)
        {
            // Access the private field _saveIdUnavailableWarningShownOnSaving
            var field = controller.GetType().GetField("_saveIdUnavailableWarningShownOnSaving", Flags);
            if (field == null)
                throw new InvalidOperationException("Expected private field '_saveIdUnavailableWarningShownOnSaving' was not found.");
            return (int)field.GetValue(controller);
        }

        public static void SetSaveIdUnavailableWarningShownOnSaveLoaded(object controller, int value)
        {
            // Set the private field _saveIdUnavailableWarningShownOnSaveLoaded
            var field = controller.GetType().GetField("_saveIdUnavailableWarningShownOnSaveLoaded", Flags);
            if (field == null)
                throw new InvalidOperationException("Expected private field '_saveIdUnavailableWarningShownOnSaveLoaded' was not found.");
            field.SetValue(controller, value);
        }

        public static void SetSaveIdUnavailableWarningShownOnSaving(object controller, int value)
        {
            // Set the private field _saveIdUnavailableWarningShownOnSaving
            var field = controller.GetType().GetField("_saveIdUnavailableWarningShownOnSaving", Flags);
            if (field == null)
                throw new InvalidOperationException("Expected private field '_saveIdUnavailableWarningShownOnSaving' was not found.");
            field.SetValue(controller, value);
        }
    }
}
