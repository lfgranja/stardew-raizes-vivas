using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Manages loading, saving, and accessing visualization configuration.
    /// Configuration is persisted per-save via IModDataService.
    /// Invalid persisted values are replaced with defaults on load per FR-006.
    /// </summary>
    public class VisualizationConfigurationService : IVisualizationConfigurationService
    {
        private const string DataKeyPrefix = "visualization_config_";

        /// <summary>Degradation modes accepted by <see cref="VisualizationConfiguration.AccessibilityDegradation"/>.</summary>
        private static readonly string[] ValidDegradationModes = { "auto", "never", "notify" };

        private readonly IModDataService _dataService;
        private readonly IMonitor _monitor;
        private readonly IFileNameSanitizationService _sanitizationService;
        private VisualizationConfiguration _configuration = new();

        public VisualizationConfigurationService(
            IModDataService dataService,
            IMonitor monitor,
            IFileNameSanitizationService sanitizationService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _sanitizationService = sanitizationService ?? throw new ArgumentNullException(nameof(sanitizationService));
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
                _configuration = CreateDefaults();
                return;
            }

            try
            {
                var key = BuildDataKey(saveId);
                var loaded = _dataService.LoadData<VisualizationConfiguration>(key);
                _configuration = loaded == null
                    ? CreateDefaults()
                    : Validate(loaded);
            }
            catch (ArgumentException)
            {
                _monitor.Log("VisualizationConfigurationService: saveId rejected by sanitization. Using defaults.", LogLevel.Warn);
                _configuration = CreateDefaults();
            }
            catch (Exception)
            {
                _monitor.Log("VisualizationConfigurationService: Failed to load config. Using defaults.", LogLevel.Warn);
                _configuration = CreateDefaults();
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
                var key = BuildDataKey(saveId);
                _dataService.SaveData(_configuration, key);
            }
            catch (ArgumentException)
            {
                _monitor.Log("VisualizationConfigurationService: saveId rejected by sanitization, skipping save.", LogLevel.Warn);
            }
            catch (Exception)
            {
                _monitor.Log("VisualizationConfigurationService: Failed to save config.", LogLevel.Warn);
            }
        }

        /// <inheritdoc />
        public void ResetToDefaults()
        {
            _configuration = CreateDefaults();
        }

        /// <inheritdoc />
        public void UpdateConfiguration(VisualizationConfiguration configuration)
        {
            _configuration = Validate(configuration ?? throw new ArgumentNullException(nameof(configuration)));
        }

        /// <summary>
        /// Builds the persistence key for a save, sanitizing the save identifier first.
        /// </summary>
        /// <param name="saveId">Raw save identifier.</param>
        /// <returns>Sanitized, prefixed data key.</returns>
        /// <exception cref="ArgumentException">Thrown when the save identifier does not sanitize to a usable value.</exception>
        private string BuildDataKey(string saveId)
        {
            var sanitized = _sanitizationService.Sanitize(saveId);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                throw new ArgumentException("Sanitized saveId is empty.", nameof(saveId));
            }
            return DataKeyPrefix + sanitized;
        }

        /// <summary>
        /// Replaces invalid fields with defaults per FR-006, preserving valid ones.
        /// </summary>
        /// <param name="configuration">Configuration loaded from storage or supplied by a caller.</param>
        /// <returns>A validated configuration.</returns>
        private VisualizationConfiguration Validate(VisualizationConfiguration configuration)
        {
            var defaults = CreateDefaults();
            var result = new VisualizationConfiguration
            {
                OverlaysEnabled = configuration.OverlaysEnabled,
                TooltipsEnabled = configuration.TooltipsEnabled,
                HoeFeedbackEnabled = configuration.HoeFeedbackEnabled,
                ShowPatterns = configuration.ShowPatterns,
                Opacity = IsValidOpacity(configuration.Opacity) ? configuration.Opacity : defaults.Opacity,
                AccessibilityDegradation = IsValidDegradationMode(configuration.AccessibilityDegradation)
                    ? configuration.AccessibilityDegradation
                    : defaults.AccessibilityDegradation,

                // ColorDTO channels are bytes, so every stored value is already in range.
                PoorColor = configuration.PoorColor,
                ModerateColor = configuration.ModerateColor,
                HealthyColor = configuration.HealthyColor,
                UnknownColor = configuration.UnknownColor
            };

            if (configuration.Opacity != result.Opacity)
            {
                _monitor.Log(
                    $"VisualizationConfigurationService: invalid opacity {configuration.Opacity} replaced with default {result.Opacity}.",
                    LogLevel.Warn);
            }

            if (!string.Equals(configuration.AccessibilityDegradation, result.AccessibilityDegradation, StringComparison.Ordinal))
            {
                _monitor.Log(
                    $"VisualizationConfigurationService: invalid accessibility degradation '{configuration.AccessibilityDegradation}' replaced with default '{result.AccessibilityDegradation}'.",
                    LogLevel.Warn);
            }

            return result;
        }

        /// <summary>
        /// Checks whether opacity falls within the accepted 0.0-1.0 range.
        /// </summary>
        /// <param name="opacity">Opacity to check.</param>
        /// <returns>True when valid.</returns>
        private static bool IsValidOpacity(float opacity)
        {
            return !float.IsNaN(opacity) && opacity >= 0f && opacity <= 1f;
        }

        /// <summary>
        /// Checks whether the degradation mode is one of the accepted values.
        /// </summary>
        /// <param name="mode">Mode to check; null is treated as invalid.</param>
        /// <returns>True when valid.</returns>
        private static bool IsValidDegradationMode(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return false;
            }

            foreach (var valid in ValidDegradationModes)
            {
                if (string.Equals(mode, valid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a configuration populated with the canonical ModConstants defaults.
        /// </summary>
        /// <returns>Default configuration.</returns>
        private static VisualizationConfiguration CreateDefaults()
        {
            return new VisualizationConfiguration();
        }
    }
}
