using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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

        public ModControllerTests()
        {
            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockModDataService = new Mock<IModDataService>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
        }

        [Fact]
        public void Constructor_WithNullHelper_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(null, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullMonitor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, null, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullManifest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, null, _mockModDataService.Object, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullModDataService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, null, _mockSoilHealthService.Object));
        }

        [Fact]
        public void Constructor_WithNullSoilHealthService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, null));
        }

        [Fact]
        public void RegisterEvents_DoesNotThrowException()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
        }

        [Fact]
        public void RegisterEvents_RegistersAllEvents()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act
            controller.RegisterEvents();

            // Assert
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
        }

        [Fact]
        public void RegisterEvents_IsIdempotent()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act - Register events twice
            controller.RegisterEvents();
            controller.RegisterEvents(); // This should skip registration since it's already registered

            // Assert - Events should only be registered once
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
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

            // Act - Simulate concurrent registration attempts
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);
                    controller.RegisterEvents();
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
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

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

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

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act
            controller.UnregisterEvents();

            // Assert - Events should not be unsubscribed if they were never registered
            // Should log message indicating events were not registered
            _mockMonitor.Verify(m => m.Log("Events were not registered, skipping unregistration.", LogLevel.Trace), Times.Once);
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

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // First register events to set up the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent unregistration attempts
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    controller.UnregisterEvents();
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
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

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act - Dispose multiple times
            controller.Dispose();
            controller.Dispose(); // Second call should not cause issues
            controller.Dispose(); // Third call should not cause issues

            // Assert - No exceptions should be thrown
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
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

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act - Simulate concurrent disposal from multiple threads
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() => controller.Dispose());
            }

            // Wait for all tasks to complete
            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert - No exceptions should be thrown due to race conditions
            // The event should be removed only once despite multiple concurrent disposal attempts
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }

        [Fact]
        public void UnregisterEvents_WhenDisposed_DoesNotUnregister()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object);

            // Act - Dispose the controller
            controller.Dispose();

            // Assert - Verify the disposal message was logged
            _mockMonitor.Verify(m => m.Log("Controller disposed successfully.", LogLevel.Trace), Times.Once);
            
            // Additional unregistration attempts should not attempt to unsubscribe
            _mockMonitor.Invocations.Clear(); // Reset previous calls to check new ones
            
            // Try to unregister events after disposal
            controller.UnregisterEvents();
            
            // The method should handle this gracefully without throwing
            var ex = Record.Exception(() => controller.UnregisterEvents());
            Assert.Null(ex);
        }
    }
}
