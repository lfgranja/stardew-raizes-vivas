using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Manages loading, saving, and accessing visualization configuration.
    /// Configuration is persisted per-save via IModDataService.
    /// </summary>
    public class VisualizationConfigurationService : IVisualizationConfigurationService
    {
        private readonly IModDataService _dataService;
        private readonly IMonitor _monitor;
        private VisualizationConfiguration _configuration = new();

        public VisualizationConfigurationService(IModDataService dataService, IMonitor monitor)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        /// <inheritdoc />
        public VisualizationConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <inheritdoc />
        public void LoadConfiguration(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("VisualizationConfigurationService: saveId is null or empty, using defaults.", LogLevel.Trace);
                _configuration = new VisualizationConfiguration();
                return;
            }

            try
            {
                var loaded = _dataService.LoadData<VisualizationConfiguration>($"viz_config_{saveId}");
                _configuration = loaded ?? new VisualizationConfiguration();
            }
            catch (Exception ex)
            {
                _monitor.Log($"VisualizationConfigurationService: Failed to load config: {ex.Message}. Using defaults.", LogLevel.Warn);
                _configuration = new VisualizationConfiguration();
            }
        }

        /// <inheritdoc />
        public void SaveConfiguration(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("VisualizationConfigurationService: saveId is null or empty, skipping save.", LogLevel.Trace);
                return;
            }

            try
            {
                _dataService.SaveData(_configuration, $"viz_config_{saveId}");
            }
            catch (Exception ex)
            {
                _monitor.Log($"VisualizationConfigurationService: Failed to save config: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <inheritdoc />
        public void ResetToDefaults()
        {
            _configuration = new VisualizationConfiguration();
        }

        /// <inheritdoc />
        public void UpdateConfiguration(VisualizationConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}
