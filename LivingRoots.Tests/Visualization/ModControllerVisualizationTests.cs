using LivingRoots.Domain;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class ModControllerVisualizationTests
    {
        [Fact]
        public void Constructor_WithVisualizationServices_DoesNotThrow()
        {
            var mockHelper = new Mock<IModHelper>();
            var mockEvents = new Mock<IModEvents>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockSoilService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();
            var mockVisService = new Mock<IVisualizationService>();
            var mockConfigService = new Mock<IVisualizationConfigurationService>();

            var gameLoopStub = new ThreadSafeGameLoopEventsStub();
            mockEvents.Setup(e => e.GameLoop).Returns(gameLoopStub);
            mockHelper.Setup(h => h.Events).Returns(mockEvents.Object);

            var controller = new Controllers.ModController(
                mockHelper.Object, mockMonitor.Object, mockManifest.Object,
                mockSoilService.Object, mockSaveIdProvider.Object,
                mockCompostingBinService.Object, mockSoilDecayService.Object,
                mockVisService.Object, mockConfigService.Object);
        }
    }
}
