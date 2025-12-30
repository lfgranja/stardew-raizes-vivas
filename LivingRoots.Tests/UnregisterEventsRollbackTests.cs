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
    public class UnregisterEventsRollbackTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<IModDataService> _mockModDataService;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;

        public UnregisterEventsRollbackTests()
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
        public void UnregisterEvents_WhenSaveLoadedUnsubscribeFails_RollsBackSuccessfully()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            // Track subscription counts for verification
            var gameLaunchedAddCount = 0;
            var saveLoadedAddCount = 0;
            var savingAddCount = 0;

            // Setup successful subscriptions during registration
            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h =>
                {
                    gameLaunchedAddCount++;
                    if (gameLaunchedAddCount == 1)
                        gameLaunchedHandler = h;
                });

            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h =>
                {
                    saveLoadedAddCount++;
                    if (saveLoadedAddCount == 1)
                        saveLoadedHandler = h;
                });

            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h =>
                {
                    savingAddCount++;
                    if (savingAddCount == 1)
                        savingHandler = h;
                });

            // Setup failing unsubscription for SaveLoaded, successful for others
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act - This should trigger the rollback mechanism
            controller.UnregisterEvents();

            // Assert - Verify that rollback was attempted for the successfully unsubscribed handlers
            // During registration: 1 add for each event
            // During rollback: 1 additional add for GameLaunched and Saving (the ones that were successfully unsubscribed)
            // Total expected: 2 for GameLaunched and Saving, 1 for SaveLoaded (no rollback since unsubscription failed)
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Exactly(2));
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Exactly(2));
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);

            // Check that EventsRegisteredFlag is restored in the state after rollback
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateValue = (int)(stateField?.GetValue(controller) ?? 0);
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & eventsRegisteredFlag) != 0, "EventsRegisteredFlag should be restored after rollback");
        }

        [Fact]
        public void UnregisterEvents_WhenMultipleUnsubscribesFail_RollsBackSuccessfully()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup failing unsubscriptions for all events - no unsubscriptions are successful
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Throws(new InvalidOperationException("GameLaunched unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Throws(new InvalidOperationException("Saving unsubscribe failed"));

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act - This should trigger the rollback mechanism
            controller.UnregisterEvents();

            // Assert - No re-subscription should occur since no unsubscriptions were successful
            // The total count should just be the original registration (1)
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);

            // Check that EventsRegisteredFlag remains in the appropriate state
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateValue = (int)(stateField?.GetValue(controller) ?? 0);
            var eventsRegisteredFlag = 1 << 0;
            // Since no unsubscriptions were successful, the state should reflect the original registered state
            Assert.True((stateValue & eventsRegisteredFlag) != 0, "EventsRegisteredFlag should remain after failed unregistration");
        }

        [Fact]
        public void UnregisterEvents_WhenRollbackPartiallyFails_StillMaintainsConsistentState()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup failing unsubscription for SaveLoaded
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Throws(new InvalidOperationException("SaveLoaded unsubscribe failed"));
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act - This should trigger the rollback mechanism
            controller.UnregisterEvents();

            // Assert - Rollback should be attempted for successfully unsubscribed handlers
            // Total invocations: 1 from original registration + 1 from rollback = 2
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Exactly(2));
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Exactly(2));

            // Check that EventsRegisteredFlag is restored in the state after rollback
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateValue = (int)(stateField?.GetValue(controller) ?? 0);
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & eventsRegisteredFlag) != 0, "EventsRegisteredFlag should be restored after rollback");
        }

        [Fact]
        public void UnregisterEvents_WhenAllUnsubscribesSucceed_DoesNotAttemptRollback()
        {
            // Arrange
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoopEvents = new Mock<IGameLoopEvents>();
            var mockCommandHelper = new Mock<ICommandHelper>();

            _mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoopEvents.Object);
            _mockHelper.Setup(x => x.ConsoleCommands).Returns(mockCommandHelper.Object);

            // Setup successful subscriptions
            EventHandler<GameLaunchedEventArgs> gameLaunchedHandler = null!;
            EventHandler<SaveLoadedEventArgs> saveLoadedHandler = null!;
            EventHandler<SavingEventArgs> savingHandler = null!;

            mockGameLoopEvents.SetupAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => gameLaunchedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => saveLoadedHandler = h);
            mockGameLoopEvents.SetupAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => savingHandler = h);

            // Setup successful unsubscriptions
            mockGameLoopEvents.SetupRemove(x => x.GameLaunched -= It.IsAny<EventHandler<GameLaunchedEventArgs>>())
                .Callback<EventHandler<GameLaunchedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.SaveLoaded -= It.IsAny<EventHandler<SaveLoadedEventArgs>>())
                .Callback<EventHandler<SaveLoadedEventArgs>>(h => { });
            mockGameLoopEvents.SetupRemove(x => x.Saving -= It.IsAny<EventHandler<SavingEventArgs>>())
                .Callback<EventHandler<SavingEventArgs>>(h => { });

            var controller = new ModController(_mockHelper.Object, _mockMonitor.Object, _mockManifest.Object, _mockModDataService.Object, _mockSoilHealthService.Object, _mockSaveIdProvider.Object);

            // Register events first to set up the controller
            controller.RegisterEvents();

            // Act
            controller.UnregisterEvents();

            // Assert - No additional re-subscription should occur since all unsubscriptions succeeded
            // The total count should just be the original registration (1)
            mockGameLoopEvents.VerifyAdd(x => x.GameLaunched += It.IsAny<EventHandler<GameLaunchedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.SaveLoaded += It.IsAny<EventHandler<SaveLoadedEventArgs>>(), Times.Once);
            mockGameLoopEvents.VerifyAdd(x => x.Saving += It.IsAny<EventHandler<SavingEventArgs>>(), Times.Once);

            // Check that EventsRegisteredFlag is cleared after successful unregistration
            var stateField = typeof(ModController).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
            var stateValue = (int)(stateField?.GetValue(controller) ?? 0);
            var eventsRegisteredFlag = 1 << 0;
            Assert.True((stateValue & eventsRegisteredFlag) == 0, "EventsRegisteredFlag should be cleared after successful unregistration");
        }
    }
}
