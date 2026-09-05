using System;
using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Service for managing visualization configuration persistence.
    /// Implements forward-compatible deserialization and per-field validation fallback per FR-004, FR-005, FR-006.
    /// </summary>
    public class VisualizationConfigurationService(
        IModDataService modDataService,
        IMonitor monitor,
        IFileNameSanitizationService fileNameSanitizationService) : IVisualizationConfigurationService
    {
        private const string ConfigKey = "visualization_config";
        private static readonly string[] ValidAccessibilityDegradationValues = { "auto", "never", "notify" };

        private readonly IModDataService _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));
        private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        private readonly IFileNameSanitizationService _fileNameSanitizationService = fileNameSanitizationService ?? throw new ArgumentNullException(nameof(fileNameSanitizationService));

        private VisualizationConfiguration _configuration = CreateDefaultConfiguration();
        private readonly object _lock = new();

        /// <inheritdoc />
        public VisualizationConfiguration GetConfiguration()
        {
            lock (_lock)
            {
                return _configuration;
            }
        }

        /// <inheritdoc />
        public void LoadConfiguration(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("LoadConfiguration aborted: invalid saveId.", LogLevel.Warn);
                return;
            }

            var dataKey = BuildKey(saveId);
            if (dataKey == null)
            {
                _monitor.Log("LoadConfiguration aborted: saveId sanitization failed.", LogLevel.Error);
                return;
            }

            try
            {
                var loaded = _modDataService.LoadData<VisualizationConfiguration>(dataKey);
                if (loaded == null)
                {
                    _monitor.Log("No saved visualization configuration found; using defaults.", LogLevel.Trace);
                    return;
                }

                lock (_lock)
                {
                    _configuration = SanitizeConfiguration(loaded);
                }

                _monitor.Log("Visualization configuration loaded successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error occurred while loading visualization configuration.", LogLevel.Error);
                _monitor.Log($"LoadConfiguration exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
#if DEBUG
                _monitor.Log(ex.StackTrace ?? "LoadConfiguration stack trace unavailable.", LogLevel.Trace);
#endif
            }
        }

        /// <inheritdoc />
        public void SaveConfiguration(string saveId)
        {
            if (string.IsNullOrWhiteSpace(saveId))
            {
                _monitor.Log("SaveConfiguration aborted: invalid saveId.", LogLevel.Warn);
                return;
            }

            var dataKey = BuildKey(saveId);
            if (dataKey == null)
            {
                _monitor.Log("SaveConfiguration aborted: saveId sanitization failed.", LogLevel.Error);
                return;
            }

            VisualizationConfiguration snapshot;
            lock (_lock)
            {
                snapshot = _configuration;
            }

            try
            {
                _modDataService.SaveData(snapshot, dataKey);
                _monitor.Log("Visualization configuration saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error occurred while saving visualization configuration.", LogLevel.Error);
                _monitor.Log($"SaveConfiguration exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
#if DEBUG
                _monitor.Log(ex.StackTrace ?? "SaveConfiguration stack trace unavailable.", LogLevel.Trace);
#endif
            }
        }

        /// <inheritdoc />
        public void ResetToDefaults()
        {
            lock (_lock)
            {
                _configuration = CreateDefaultConfiguration();
            }
            _monitor.Log("Visualization configuration reset to defaults.", LogLevel.Info);
        }

        /// <inheritdoc />
        public void UpdateConfiguration(VisualizationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var sanitized = SanitizeConfiguration(configuration);

            lock (_lock)
            {
                _configuration = sanitized;
            }

            _monitor.Log("Visualization configuration updated.", LogLevel.Trace);
        }

        /// <summary>
        /// Creates a default visualization configuration using ModConstants values.
        /// </summary>
        private static VisualizationConfiguration CreateDefaultConfiguration()
        {
            return new VisualizationConfiguration
            {
                OverlaysEnabled = ModConstants.OverlaysEnabledDefault,
                TooltipsEnabled = ModConstants.TooltipsEnabledDefault,
                HoeFeedbackEnabled = ModConstants.HoeFeedbackEnabledDefault,
                Opacity = ModConstants.DefaultOpacity,
                ShowPatterns = true,
                AccessibilityDegradation = "auto",
                PoorColor = new ColorDTO(255, 0, 0, 255),
                ModerateColor = new ColorDTO(255, 255, 0, 255),
                HealthyColor = new ColorDTO(0, 255, 0, 255),
                UnknownColor = new ColorDTO(128, 128, 128, 255)
            };
        }

        /// <summary>
        /// Validates configuration fields and replaces invalid values with defaults.
        /// Valid values from the input are preserved; only invalid fields fall back to defaults.
        /// </summary>
        /// <param name="config">The configuration to sanitize.</param>
        /// <returns>A new VisualizationConfiguration with valid values preserved and invalid ones replaced.</returns>
        private static VisualizationConfiguration SanitizeConfiguration(VisualizationConfiguration config)
        {
            var defaults = CreateDefaultConfiguration();

            return new VisualizationConfiguration
            {
                // Boolean properties: any value is valid (true/false), preserve as-is
                OverlaysEnabled = config.OverlaysEnabled,
                TooltipsEnabled = config.TooltipsEnabled,
                HoeFeedbackEnabled = config.HoeFeedbackEnabled,
                ShowPatterns = config.ShowPatterns,

                // Opacity: must be between 0.0 and 1.0 inclusive
                Opacity = IsValidOpacity(config.Opacity) ? config.Opacity : defaults.Opacity,

                // AccessibilityDegradation: must be one of the valid values
                AccessibilityDegradation = IsValidAccessibilityDegradation(config.AccessibilityDegradation)
                    ? config.AccessibilityDegradation
                    : defaults.AccessibilityDegradation,

                // ColorDTO is a struct with byte fields — always valid by construction
                PoorColor = config.PoorColor,
                ModerateColor = config.ModerateColor,
                HealthyColor = config.HealthyColor,
                UnknownColor = config.UnknownColor
            };
        }

        /// <summary>
        /// Validates that opacity is within the valid range [0.0, 1.0].
        /// Rejects NaN and Infinity values.
        /// </summary>
        private static bool IsValidOpacity(float opacity)
        {
            return !float.IsNaN(opacity) && !float.IsInfinity(opacity) && opacity >= 0.0f && opacity <= 1.0f;
        }

        /// <summary>
        /// Validates that the accessibility degradation value is one of the allowed options.
        /// </summary>
        private static bool IsValidAccessibilityDegradation(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var valid in ValidAccessibilityDegradationValues)
            {
                if (string.Equals(value, valid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Builds the storage key by combining the saveId with the configuration key.
        /// Returns null if the saveId cannot be sanitized.
        /// </summary>
        private string? BuildKey(string saveId)
        {
            try
            {
                var sanitized = _fileNameSanitizationService.Sanitize(saveId);
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    return null;
                }

                return $"{ConfigKey}_{sanitized}";
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
