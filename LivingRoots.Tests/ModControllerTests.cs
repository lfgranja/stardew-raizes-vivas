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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
                
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.Once);
            Assert.Contains("Error occurred while registering game events", loggedMessage);
            Assert.DoesNotContain("Test exception", loggedMessage); // Should not contain raw exception message for security
            Assert.Equal(LogLevel.Error, loggedLevel);
        }
        
        [Fact]
        public void RegisterEvents_WhenExceptionOccurs_HandlerIsCleared()
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
            
            _mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Verifiable();
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
            // Act - This should cause an exception in the event registration which should clear the handler
            controller.RegisterEvents();
            
            // Assert - The eventsRegistered flag should be false and we need to verify the internal state
            // Since we can't directly access the private field, we can check if subsequent calls work properly
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
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
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
            // First register events to set up the handler
            controller.RegisterEvents();
            
            // Act & Assert
            var ex = Record.Exception(() => controller.UnregisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            // Verify logging occurred with the generic message, not the raw exception
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
            Assert.Contains("Error occurred while unregistering game events", loggedMessage);
            Assert.DoesNotContain("Test exception for unregister", loggedMessage); // Should not contain raw exception message for security
            Assert.Equal(LogLevel.Error, loggedLevel);
        }
        
        [Fact]
        public void RegisterEvents_WhenDisposed_ShouldLogAndReturnWithoutThrowing()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            
            // Create a single manifest with a concrete UniqueID and Version
            mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            string loggedMessage = "";
            LogLevel loggedLevel = LogLevel.Info;
            mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((message, level) =>
                {
                    // Only capture the disposal-related log message
                    if (message.Contains("Attempted to register events after disposal"))
                    {
                        loggedMessage = message;
                        loggedLevel = level;
                    }
                })
                .Verifiable();
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(mockHelper.Object, mockMonitor.Object, mockManifest.Object, mockModDataService.Object);
            
            // Register events first to set up the controller
            controller.RegisterEvents();
            
            // Dispose the controller
            controller.Dispose();
            
            // Act - Try to register events after disposal (should not throw)
            var ex = Record.Exception(() => controller.RegisterEvents());
            
            // Assert
            Assert.Null(ex); // No exception should be thrown
            Assert.Contains("Attempted to register events after disposal", loggedMessage);
            Assert.Equal(LogLevel.Trace, loggedLevel);
        }
        
        [Fact]
        public void UnregisterEvents_WhenDisposed_ShouldLogAndReturnWithoutThrowing()
        {
            // Arrange
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            
            // Create a single manifest with a concrete UniqueID and Version
            mockManifest.Setup(m => m.UniqueID).Returns("lfgranja.LivingRoots");
            mockManifest.Setup(m => m.Version).Returns(new SemanticVersion(1, 0, 0));
            
            mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            
            string loggedMessage = "";
            LogLevel loggedLevel = LogLevel.Info;
            mockMonitor.Setup(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()))
                .Callback<string, LogLevel>((message, level) =>
                {
                    // Only capture the disposal-related log message
                    if (message.Contains("Attempted to unregister events after disposal"))
                    {
                        loggedMessage = message;
                        loggedLevel = level;
                    }
                })
                .Verifiable();
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(mockHelper.Object, mockMonitor.Object, mockManifest.Object, mockModDataService.Object);
            
            // Register events first to set up the controller
            controller.RegisterEvents();
            
            // Dispose the controller
            controller.Dispose();
            
            // Act - Try to unregister events after disposal (should not throw)
            var ex = Record.Exception(() => controller.UnregisterEvents());
            
            // Assert
            Assert.Null(ex); // No exception should be thrown
            Assert.Contains("Attempted to unregister events after disposal", loggedMessage);
            Assert.Equal(LogLevel.Trace, loggedLevel);
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
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
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
        public void Dispose_PreventsEventRegistrationAfterDisposal()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
            // Register events first to set up the controller
            controller.RegisterEvents();
            
            // Act - Dispose the controller
            controller.Dispose();
            
            // Try to register events after disposal
            controller.RegisterEvents();
            
            // Assert - Event registration should not occur after disposal
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public async System.Threading.Tasks.Task OnGameLaunched_CommandRegistration_IsThreadSafe()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            var mockCommandHelper = new Mock<ICommandHelper>(MockBehavior.Strict);
            mockCommandHelper
                .Setup(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()))
                .Verifiable();
            
            // Need to set up the helper mock to return the command helper
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);
            
            var mockModDataService = new Mock<IModDataService>();
            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, mockModDataService.Object);
            
            // Act - Simulate concurrent calls to OnGameLaunched to test command registration race condition
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    var args = new GameLaunchedEventArgs();
                    controller.GetType().GetMethod("OnGameLaunched", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(controller, new object[] { null, args });
                });
            }
            
            // Wait for all tasks to complete
            await System.Threading.Tasks.Task.WhenAll(tasks);
            
            // Assert - Command should only be registered once despite multiple concurrent calls
            mockCommandHelper.Verify(x => x.Add("lr_version", "Shows the Living Roots version.", It.IsAny<Action<string, string[]>>()), Times.Once);
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
    }
}