using System;
using LivingRoots.Controllers;
using LivingRoots.Domain;
using LivingRoots.Domain.Interfaces;
using LivingRoots.Services;
using Moq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class ModControllerSoilGrowthEventTests
    {
        [Fact]
        public void ModController_Constructs_With_ModApi()
        {
            var mockHelper = new Mock<IModHelper>();
            var mockMonitor = new Mock<IMonitor>();
            var mockManifest = new Mock<IManifest>();
            var mockEvents = new Mock<IModEvents>();
            var mockGameLoop = new Mock<IGameLoopEvents>();
            mockEvents.Setup(x => x.GameLoop).Returns(mockGameLoop.Object);
            mockHelper.Setup(x => x.Events).Returns(mockEvents.Object);
            mockHelper.Setup(x => x.ConsoleCommands).Returns(new Mock<ICommandHelper>().Object);

            var mockSoilHealthService = new Mock<ISoilHealthService>();
            var mockSaveIdProvider = new Mock<ISaveIdProvider>();
            var mockCompostingBinService = new Mock<ICompostingBinService>();
            var mockSoilDecayService = new Mock<ISoilDecayService>();
            var modApi = new LivingRootsModApi();

            var controller = new ModController(
                mockHelper.Object,
                mockMonitor.Object,
                mockManifest.Object,
                mockSoilHealthService.Object,
                mockSaveIdProvider.Object,
                mockCompostingBinService.Object,
                mockSoilDecayService.Object,
                modApi: modApi);

            Assert.NotNull(controller);
            controller.Dispose();
        }
    }
}
