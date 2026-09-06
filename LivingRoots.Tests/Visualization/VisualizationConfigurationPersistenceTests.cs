using LivingRoots.Domain.Visualization;
using LivingRoots.Services;
using LivingRoots.Services.Visualization;
using Moq;
using StardewModdingAPI;
using Xunit;

namespace LivingRoots.Tests.Visualization
{
    public class VisualizationConfigurationPersistenceTests
    {
        private readonly Services.Visualization.VisualizationConfigurationService _service;

        public VisualizationConfigurationPersistenceTests()
        {
            var mockDataService = new Moq.Mock<IModDataService>();
            var mockMonitor = new Moq.Mock<IMonitor>();
            _service = new Services.Visualization.VisualizationConfigurationService(mockDataService.Object, mockMonitor.Object);
        }

        [Fact]
        public void LoadConfiguration_MissingFile_ReturnsDefaults()
        {
            _service.LoadConfiguration("test-save");
            var config = _service.GetConfiguration();
            Assert.NotNull(config);
        }

        [Fact]
        public void SaveConfiguration_PersistsData()
        {
            _service.SaveConfiguration("test-save");
        }
    }
}
