using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using LivingRoots.Services;
using LivingRoots.Tests.Fixtures;
using LivingRoots.Tests.Stubs;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Covers T117 and T118 from specs/004-playable-compost-loop/tasks.md.
    /// </summary>
    public class BinPickupTests
    {
        private readonly Mock<IModHelper> _mockHelper;
        private readonly Mock<IMonitor> _mockMonitor;
        private readonly Mock<IManifest> _mockManifest;
        private readonly Mock<ISoilHealthService> _mockSoilHealthService;
        private readonly Mock<ISaveIdProvider> _mockSaveIdProvider;
        private readonly Mock<ISoilDecayService> _mockSoilDecayService;
        private readonly TimeProviderStub _timeStub;

        public BinPickupTests()
        {
            GameStateFixture.Install();

            _mockHelper = new Mock<IModHelper>();
            _mockMonitor = new Mock<IMonitor>();
            _mockManifest = new Mock<IManifest>();
            _mockSoilHealthService = new Mock<ISoilHealthService>();
            _mockSaveIdProvider = new Mock<ISaveIdProvider>();
            _mockSoilDecayService = new Mock<ISoilDecayService>();

            _mockManifest.Setup(x => x.UniqueID).Returns("test.mod.id");
            _mockManifest.Setup(x => x.Version).Returns(new SemanticVersion(1, 0, 0));

            _timeStub = new TimeProviderStub { TotalDays = 1 };
        }

        private CompostingBinService CreateService()
        {
            var dataService = new Mock<IModDataService>();
            dataService.Setup(x => x.LoadData<CompostingBinData>(It.IsAny<string>())).Returns((CompostingBinData?)null);

            return new CompostingBinService(
                dataService.Object,
                _mockSaveIdProvider.Object,
                new OrganicWasteValidator(_mockMonitor.Object),
                _mockMonitor.Object,
                _timeStub,
                new CompostingBinFactory(),
                new RecordingPlayerInventory());
        }

        private ModController CreateController()
        {
            return new ModController(
                _mockHelper.Object,
                _mockMonitor.Object,
                _mockManifest.Object,
                _mockSoilHealthService.Object,
                _mockSaveIdProvider.Object,
                CreateService(),
                _mockSoilDecayService.Object);
        }

        /// <summary>
        /// T117 [P] [US1] — FR-005c: property-based invariant asserting that a
        /// progress-carrying bin, reconstructed through the production
        /// CreateDislodgedBinItem path, is unstackable (max stack 1), carries its
        /// batch modData, and resolves as the (BC) big craftable.
        /// </summary>
        [Property(MaxTest = 100)]
        public void T117_ProgressBin_EnforcesMaxStack1_ProductionPath(
            PositiveInt arbitraryTileX,
            PositiveInt arbitraryTileY)
        {
            var service = CreateService();
            var tile = new Vector2(arbitraryTileX.Get % 20, arbitraryTileY.Get % 20);

            // A batch is loaded and mid-processing, so the record carries progress.
            service.AddWaste("Farm", tile, ItemFactory.CreateValidWasteItem());
            Assert.Equal(CompostingBinState.Processing, service.GetBinState("Farm", tile));

            var dislodged = service.CreateDislodgedBinItem("Farm", tile);
            var dislodgedObject = Assert.IsAssignableFrom<StardewValley.Object>(dislodged);

            Assert.NotNull(dislodged);
            Assert.Equal(1, dislodged.Stack);                                          // FR-005c: max stack 1 while carrying progress
            Assert.True(dislodgedObject.bigCraftable.Value);                           // FR-002: resolves as (BC)LivingRoots.CompostingBin
            Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataInputIdKey));        // FR-005c: progress serialized
            Assert.True(dislodged.modData.ContainsKey(ModConstants.ModDataDaysRemainingKey));
        }

        /// <summary>
        /// T118 [P] [US1] — FR-005c: a dislodged bin with available inventory
        /// space returns to inventory through the production ModController
        /// dislodge path; debris is the full-inventory fallback only.
        /// </summary>
        [Fact]
        public void T118_DislodgedBin_WithInventorySpace_ReturnsToInventory()
        {
            var controller = CreateController();
            var fixture = new GameLocationFixture("Farm", isFarm: true);
            var tile = new Vector2(7, 7);

            var binItem = ItemFactory.CreateItem(ModConstants.CompostingBinItemId);
            var inventory = new List<Item?> { binItem };
            controller.TryPlaceBin(fixture.Location, tile, binItem, inventory);
            Assert.True(fixture.Location.Objects.ContainsKey(tile), "Precondition: placement must succeed.");

            var recordingInventory = new RecordingPlayerInventory();
            bool debrisDropped = false;

            var dislodged = controller.TryDislodgeBin(
                fixture.Location,
                tile,
                recordingInventory,
                (_, _, _) => debrisDropped = true);

            Assert.True(dislodged, "FR-005c (T118): dislodge must succeed for a placed bin.");
            Assert.False(debrisDropped, "FR-005c (T118): debris is only for a full inventory — space was available.");
            Assert.True(recordingInventory.AddedItems.Count == 1, "FR-005c (T118): the reconstructed bin returned to the player's inventory.");
            Assert.False(fixture.Location.Objects.ContainsKey(tile), "FR-002: the world object is gone after dislodge.");
        }
    }
}
