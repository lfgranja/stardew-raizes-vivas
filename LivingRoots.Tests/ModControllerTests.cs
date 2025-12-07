using StardewModdingAPI;
using StardewModdingAPI.Events;
using Moq;
using LivingRoots.Controllers;
using LivingRoots.Services;
using LivingRoots.Domain;
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
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saving to Saved // NEW
            
            var controller = CreateController(); // Use helper method
            
            // Act & Assert
            var ex = Record.Exception(() => controller.RegisterEvents());
            Assert.Null(ex);
            
            // Verify that expected interactions occurred
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saving to Saved // NEW
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
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
            var controller = CreateController(); // Use helper method
            
            // Act
            controller.RegisterEvents();
            
            // Assert
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void RegisterEvents_RegistersCorrectHandlers()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            EventHandler<GameLaunchedEventArgs>? gameLaunchedHandler = null;
            EventHandler<SaveLoadedEventArgs>? saveLoadedHandler = null; // NEW
            EventHandler<SavingEventArgs>? savingHandler = null; // CHANGED FROM SavedEventArgs to SavingEventArgs // NEW
            
            // Capture the event handlers being registered
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents // NEW
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents // NEW
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h); // CHANGED FROM SavedEventArgs to SavingEventArgs
            
            var controller = CreateController(); // Use helper method
            
            // Act
            controller.RegisterEvents();
            
            // Assert
            Assert.NotNull(gameLaunchedHandler);
            Assert.NotNull(saveLoadedHandler); // NEW
            Assert.NotNull(savingHandler); // CHANGED FROM savedHandler to savingHandler // NEW
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
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
                
            var controller = CreateController(); // Use helper method
            
            // Act - Register events twice
            controller.RegisterEvents();
            controller.RegisterEvents(); // This should skip registration since it's already registered
            
            // Assert
            // Verify that subscription happened only once due to the idempotent check
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving // NEW
            _mockMonitor.Verify(x => x.Log("Events are already registered, skipping registration.", LogLevel.Trace), Times.Once);
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
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
            var controller = CreateController(); // Use helper method
            
            // First register events to set up the controller
            controller.RegisterEvents();
            
            // Act
            controller.UnregisterEvents();
            
            // Assert
            // The events should be removed once during unregistration
            mockGameLoopEvents.VerifyRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving // NEW
            _mockMonitor.Verify(x => x.Log(It.IsAny<string>(), It.IsAny<LogLevel>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public void UnregisterEvents_WhenExceptionOccurs_LogsErrorMessageWithoutStackTrace()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription and unsubscription expectations with exception throwing
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception for unregister"));
            mockGameLoopEvents // NEW
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("Test exception for unregister"));
            mockGameLoopEvents // NEW
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Throws(new InvalidOperationException("Test exception for unregister")); // CHANGED FROM Saved to Saving
            
            var controller = CreateController(); // Use helper method
            
            // First register events to set up the handler
            controller.RegisterEvents();
            
            // Act & Assert - No exception should be thrown despite the handler removal failure
            var ex = Record.Exception(() => controller.UnregisterEvents());
            Assert.Null(ex); // Exception should be caught and logged, not thrown
            
            // Verify that error was logged without the raw exception details
            _mockMonitor.Verify(x => x.Log("Error occurred while unregistering game events.", LogLevel.Error), Times.AtLeastOnce);
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
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
            var controller = CreateController(); // Use helper method
            
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
        public async System.Threading.Tasks.Task RegisterEvents_IsThreadSafe_WithConcurrentOperations()
        {
            // Arrange
            var (mockEvents, mockGameLoopEvents) = SetupEventMocks();
            
            // Setup event subscription expectations
            mockGameLoopEvents
                .SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
            var controller = CreateController(); // Use helper method
            
            // Act - Simulate concurrent registration attempts
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() => controller.RegisterEvents());
            }
            
            // Wait for all tasks to complete
            await System.Threading.Tasks.Task.WhenAll(tasks);
            
            // Assert - Only one registration should have succeeded, others should have been skipped
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving // NEW
            _mockMonitor.Verify(x => x.Log("Events are already registered, skipping registration.", LogLevel.Trace), Times.AtLeastOnce);
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
                .SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
            var controller = CreateController(); // Use helper method
            
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
            mockGameLoopEvents.VerifyRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // NEW
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving // NEW
            _mockMonitor.Verify(x => x.Log("Controller is already disposed.", LogLevel.Trace), Times.Once);
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
                .SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>());
            mockGameLoopEvents
                .SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>()); // NEW
            mockGameLoopEvents
                .SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>()); // CHANGED FROM Saved to Saving // NEW
            
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
            mockGameLoopEvents.VerifyRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once); // CHANGED FROM Saved to Saving // NEW
            _mockMonitor.Verify(x => x.Log("Controller is already disposed.", LogLevel.Trace), Times.Once);
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
    }
}
