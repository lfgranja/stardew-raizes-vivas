using StardewModdingAPI;
using StardewModdingAPI.Events;
using Moq;
using LivingRoots.Controllers;
using LivingRoots.Services;
using Xunit;
using System;


namespace LivingRoots.Tests
{
    public class ModControllerTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;

        public ModControllerTests()
        {
            _mockHelper = new Mock<IModHelper>(MockBehavior.Strict);
            _mockMonitor = new Mock<IMonitor>(MockBehavior.Strict);
            _mockManifest = new Mock<IManifest>(MockBehavior.Strict);
        }

        private (Mock<IModEvents>, Mock<IGameLoopEvents>) SetupEventMocks()
        {
            var mockEvents = new Mock<IModEvents>(MockBehavior.Strict);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Strict);
            
            // Create a single manifest with a concrete UniqueID and Version
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            
            
            _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>())).Verifiable();
            
            return (mockEvents, mockGameLoopEvents);
        }

        [Fact]
        public void RegisterEvents_DoesNotThrowException()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
            
            // Verify that expected interactions occurred
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_RegistersGameLaunchedEvent()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act
            controller.RegisterEvents();
            
            // Assert
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_RegistersCorrectHandler()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            EventHandler<GameLaunchedEventArgs>? eventHandler = null;
            
            // Capture the event handler being registered
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(handler => { eventHandler = handler; });
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act
            controller.RegisterEvents();
            
            // Assert
            Assert.NotNull(eventHandler);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_IsIdempotent()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup subscription for duplicate registration scenario
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
                
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act - Register events twice
            controller.RegisterEvents();
            controller.RegisterEvents(); // This should skip registration since it's already registered
            
            // Assert
            // Verify that subscription happened only once due to the idempotent check
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeast(2)); // At least 2 log calls
        }
        
        [Fact]
        public void UnregisterEvents_RemovesGameLaunchedEvent()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // First register events to set _eventsRegistered to true
            controller.RegisterEvents();
            
            // Act
            controller.UnregisterEvents();
            
            // Assert
            // The event should be removed once during unregistration
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
    }
}