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
        
        [Fact]
        public async System.Threading.Tasks.Task UnregisterEvents_IsThreadSafe_WithConcurrentOperations()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act - Simulate concurrent registration and unregistration
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks[taskId] = System.Threading.Tasks.Task.Run(() =>
                {
                    if (taskId % 2 == 0)
                    {
                        controller.RegisterEvents(); // Even tasks register
                    }
                    else
                    {
                        controller.UnregisterEvents(); // Odd tasks unregister
                    }
                });
            }
            
            // Wait for all tasks to complete
            await System.Threading.Tasks.Task.WhenAll(tasks);
            
            // Assert - No exceptions should be thrown due to race conditions
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_WhenExceptionOccurs_LogsErrorMessageWithoutStackTrace()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>(MockBehavior.Strict);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Strict);
            
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            // Setup to throw an exception when trying to register the event
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception"));
            
            string loggedMessage = "";
            LogLevel loggedLevel = LogLevel.Info;
            _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((message, level) =>
                {
                    loggedMessage = message;
                    loggedLevel = level;
                })
                .Verifiable();
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Once);
            Assert.Contains("Test exception", loggedMessage);
            Assert.DoesNotContain("System.InvalidOperationException", loggedMessage); // Should not contain full type name
            Assert.Equal(LogLevel.Error, loggedLevel);
        }
        
        [Fact]
        public void UnregisterEvents_WhenExceptionOccurs_LogsErrorMessageWithoutStackTrace()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>(MockBehavior.Strict);
            var mockGameLoopEvents = new Mock<IGameLoopEvents>(MockBehavior.Strict);
            
            _mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            _mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            // Setup to throw an exception when trying to unregister the event
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception for unregister"));
            
            string loggedMessage = "";
            LogLevel loggedLevel = LogLevel.Info;
            _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((message, level) =>
                {
                    loggedMessage = message;
                    loggedLevel = level;
                })
                .Verifiable();
            
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object);
            
            // First register events to set up the handler
            controller.RegisterEvents();
            
            // Act & Assert
            var ex = Record.Exception(() => controller.UnregisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            // Verify logging occurred with the exception message but not the full stack trace
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
            Assert.Contains("Test exception for unregister", loggedMessage);
            Assert.DoesNotContain("System.InvalidOperationException", loggedMessage); // Should not contain full type name
            Assert.Equal(LogLevel.Error, loggedLevel);
        }
    }
}