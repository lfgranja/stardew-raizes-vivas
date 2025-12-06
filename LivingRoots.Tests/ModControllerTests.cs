using StardewModdingAPI;
using StardewModdingAPI.Events;
using Moq;
using LivingRoots.Controllers;
using LivingRoots.Services;
using LivingRoots.Domain;
using Xunit;
using System;
using System.Collections.Generic; // Adicionando esta importação

namespace LivingRoots.Tests
{
    public class ModControllerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;

        public ModControllerTests()
        {
            _mockHelper = new Mock<IModHelper>(MockBehavior.Loose);
            _mockMonitor = new Mock<IMonitor>(MockBehavior.Loose);
            _mockManifest = new Mock<IManifest>(MockBehavior.Loose);
        }

        // Helper method to create controller with all dependencies
        private ModController CreateController(IModDataService? modDataService = null, ISoilHealthService? soilHealthService = null)
        {
            var mockModDataService = modDataService ?? new Mock<IModDataService>().Object;
            var mockSoilHealthService = soilHealthService ?? new Mock<ISoilHealthService>().Object;
            
            return new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, 
                                   mockModDataService, mockSoilHealthService);
        }

        [Fact]
        public void RegisterEvents_DoesNotThrowException()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // NEW
            
            var controller = CreateController(); // Use helper method
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
            
            // Verify that expected interactions occurred
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // NEW
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_RegistersAllEvents()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>());
            
            var controller = CreateController(); // Use helper method
            
            // Act
            controller.RegisterEvents();
            
            // Assert
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void UnregisterEvents_RemovesAllEvents()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // NEW
            
            var controller = CreateController(); // Use helper method
            
            // First register events to set up the controller
            controller.RegisterEvents();
            
            // Act
            controller.UnregisterEvents();
            
            // Assert
            // The events should be removed once during unregistration
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // NEW
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_WhenExceptionOccurs_LogsErrorMessageWithoutStackTrace()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>(MockBehavior.Loose);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Loose);
            
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            // Setup to throw an exception when trying to register the event
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception"));
            mockGameLoopEvents // NEW
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception"));
            mockGameLoopEvents // NEW
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Throws(new InvalidOperationException("Test exception"));
            
            var loggedMessages = new List<(string message, LogLevel level)>();
            _mockMonitor
                .Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((msg, level) => loggedMessages.Add((msg, level)));
            
            var controller = CreateController(); // Use helper method
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            // Verify that error message was logged without the raw exception details
            Assert.Contains(loggedMessages, log => log.message.Contains("Error occurred while registering game events") && log.level == LogLevel.Error);
            Assert.DoesNotContain(loggedMessages, log => log.message.Contains("Test exception")); // Should not contain raw exception message for security
        }
        
        [Fact]
        public void RegisterEvents_WhenDisposed_ShouldLogAndReturnWithoutThrowing()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>(MockBehavior.Loose);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Loose);
            
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            var loggedMessages = new List<(string message, LogLevel level)>();
            _mockMonitor
                .Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((msg, level) => loggedMessages.Add((msg, level)));
            
            var controller = CreateController(); // Use helper method
            
            // Register events first to set up the controller
            controller.RegisterEvents();
            
            // Dispose the controller
            controller.Dispose();
            
            // Act - Try to register events after disposal (should not throw)
            var ex = Record.Exception(() => controller.RegisterEvents());
            
            // Assert
            Assert.Null(ex); // No exception should be thrown
            Assert.Contains(loggedMessages, log => log.message.Contains("Attempted to register events after disposal") && log.level == LogLevel.Trace);
        }
        
        [Fact]
        public void Dispose_IsIdempotent_CanBeCalledMultipleTimes()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // NEW
            
            var controller = CreateController(); // Use helper method
            
            // Register events first to set up the controller
            controller.RegisterEvents();
            
            // Act - Dispose multiple times
            controller.Dispose();
            controller.Dispose(); // Second call should not cause issues
            controller.Dispose(); // Third call should not cause issues
            
            // Assert - No exceptions should be thrown
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // NEW
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        private (Mock<IModEvents>, Mock<IGameLoopEvents>) SetupEventMocks()
        {
            var mockEvents = new Mock<IModEvents>(MockBehavior.Loose);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Loose);
            
            // Create a single manifest with a concrete UniqueID and Version
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            return (mockEvents, mockGameLoopEvents);
        }
    }
}
