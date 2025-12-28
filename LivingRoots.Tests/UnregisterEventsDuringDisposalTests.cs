using System;
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
    public class UnregisterEventsDuringDisposalTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;

        public UnregisterEventsDuringDisposalTests()
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
        public void UnregisterEvents_WhenCalledDuringDisposal_DoesNotReSubscribeHandlers()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions during registration
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup unsubscriptions that will fail to trigger rollback
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("GameLaunched unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Throws(new InvalidOperationException("Saving unsubscribe failed"));

            // Setup re-subscription for rollback - this should NOT happen during disposal
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => { });
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Manually set the disposed flag using reflection to simulate disposal state
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposedFlag = 1 << 2; // DisposedFlag constant value
            var currentState = (int)stateField!.GetValue(controller)!;
            stateField.SetValue(controller, currentState | disposedFlag);

            // Act - This should NOT attempt rollback because controller is disposed
            controller.UnregisterEvents();

            // Assert - Verify that rollback did NOT happen during disposal
            // Total invocations should be just 1 (from original registration) since rollback was skipped
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
            
            // Check that EventsRegisteredFlag is properly cleared during disposal
            var stateValue = (int)stateField.GetValue(controller)!;
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & disposedFlag) != 0, "Controller should still be marked as disposed");
            Assert.True((stateValue & eventsRegisteredFlag) == 0, "EventsRegisteredFlag should be cleared during disposal");
        }

        [Fact]
        public void UnregisterEvents_WhenCalledDuringDisposal_DoesNotReSubscribeHandlers_PartialUnsubscribeFailure()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions during registration
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup partial unsubscription failure (only SaveLoaded fails)
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            // Setup re-subscription for rollback - this should NOT happen during disposal
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Manually set the disposed flag using reflection to simulate disposal state
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposedFlag = 1 << 2; // DisposedFlag constant value
            var currentState = (int)stateField!.GetValue(controller)!;
            stateField.SetValue(controller, currentState | disposedFlag);

            // Act - This should NOT attempt rollback because controller is disposed
            controller.UnregisterEvents();

            // Assert - Verify that rollback did NOT happen during disposal
            // Total invocations should be just 1 (from original registration) since rollback was skipped
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);
            
            // Check that EventsRegisteredFlag is properly cleared during disposal
            var stateValue = (int)stateField.GetValue(controller)!;
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & disposedFlag) != 0, "Controller should still be marked as disposed");
            Assert.True((stateValue & eventsRegisteredFlag) == 0, "EventsRegisteredFlag should be cleared during disposal");
        }

        [Fact]
        public void UnregisterEvents_WhenNotDisposed_DoesReSubscribeHandlersOnPartialFailure()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions during registration
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup partial unsubscription: GameLaunched and Saving succeed, SaveLoaded fails
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            // Setup re-subscription for rollback - this SHOULD happen when not disposed
            // Only GameLaunched and Saving should be re-subscribed since SaveLoaded was never unsubscribed
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Ensure controller is NOT disposed (do not set disposed flag)
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposedFlag = 1 << 2; // DisposedFlag constant value
            var stateValue = (int)stateField!.GetValue(controller)!;
            Assert.True((stateValue & disposedFlag) == 0, "Controller should not be disposed initially");

            // Act - This should attempt rollback because controller is not disposed
            controller.UnregisterEvents();

            // Assert - Verify that rollback DID happen when not disposed for successfully unsubscribed handlers
            // Total invocations should be 2 for GameLaunched and Saving (1 from original registration + 1 from rollback)
            // SaveLoaded should remain at 1 since it was never unsubscribed
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Exactly(2));
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once); // No rollback for failed unsubscribe
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Exactly(2));
            
            // Check that EventsRegisteredFlag is restored after rollback when not disposed
            stateValue = (int)stateField.GetValue(controller)!;
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & eventsRegisteredFlag) != 0, "EventsRegisteredFlag should be restored after rollback when not disposed");
        }
    }
}