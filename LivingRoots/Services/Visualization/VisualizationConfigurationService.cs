using LivingRoots.Domain;
using LivingRoots.Domain.Visualization;
using StardewModdingAPI;

namespace LivingRoots.Services.Visualization
{
    /// <summary>
    /// Manages loading, saving, and accessing visualization configuration.
    /// The configuration scope is the mod folder — one config.json per
    /// installation, shared by every save (true-global per spec Session
    /// 2026-10-06), never keyed by save ID. The validated configuration is
    /// cached in memory after the initial load and frames are served from
    /// the cache without re-reading the persisted JSON; the cache is
    /// invalidated only by the file-change signal (
    /// <see cref="NotifyConfigurationChanged"/>) per T083 (F2, FR-005,
    /// FR-019, FR-020). Invalid persisted values are replaced with defaults
    /// on load per FR-006.
    /// </summary>
    public class VisualizationConfigurationService : IVisualizationConfigurationService
    {
        /// <summary>
        /// The persistence key of the single mod-folder config.json.
        /// The key never incorporates the save identifier, so every save
        /// of the installation resolves the same configuration
        /// (true-global scope per spec Session 2026-10-06).
        /// </summary>
        private const string ConfigDataKey = "visualization_config_modfolder";

        /// <summary>Degradation modes accepted by <see cref="VisualizationConfiguration.AccessibilityDegradation"/>.</summary>
        private static readonly string[] ValidDegradationModes = { "auto", "never", "notify" };

        private readonly IModDataService _dataService;
        private readonly IMonitor _monitor;
        private readonly IFileNameSanitizationService _sanitizationService;
        private VisualizationConfiguration _configuration = new();

        /// <summary>
        /// Cache-coordination flag for the game loop: 1 when the in-memory
        /// configuration does not reflect the persisted store and the next
        /// <see cref="LoadConfiguration"/> must re-read it (the initial
        /// state, and every file-change signal); 0 when the cache is current
        /// and frames are served from memory. Flipped with
        /// <see cref="System.Threading.Interlocked"/> so the signalling
        /// thread and the render-thread reload coordinate without a lock.
        /// </summary>
        private int _configurationCacheInvalidated = 1;

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

            // Serve frames from the in-memory cache (T083, F2, FR-020):
            // the persisted store is re-read only when the cache was
            // invalidated — the initial load, or an explicit file-change
            // signal. Re-reading on every call would be a blocking disk
            // hit inside ModConstants.MaxRenderTimeMs on every rendered
            // frame, the exact cost T083 removes. The flag is consumed
            // atomically so concurrent callers never double-read.
            if (System.Threading.Interlocked.Exchange(ref _configurationCacheInvalidated, 0) == 0)
            {
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
                // Keep the cache invalidated so the next load retries
                // instead of serving defaults indefinitely.
                System.Threading.Interlocked.Exchange(ref _configurationCacheInvalidated, 1);
            }
            catch (Exception)
            {
                _monitor.Log("VisualizationConfigurationService: Failed to load config. Using defaults.", LogLevel.Warn);
                _configuration = CreateDefaults();
                // Keep the cache invalidated so the next load retries
                // instead of serving defaults indefinitely.
                System.Threading.Interlocked.Exchange(ref _configurationCacheInvalidated, 1);
            }
        }

        /// <summary>
        /// Signals that the persisted configuration file changed out of
        /// band (an external edit of the mod-folder config.json), so the
        /// next <see cref="LoadConfiguration"/> re-reads the persisted
        /// store once and applies the change within the next render frame
        /// (FR-005 hot-reload, FR-019, SC-003). The render frame calls
        /// <see cref="LoadConfiguration"/> every frame; this signal is
        /// what makes that call re-read instead of serving the in-memory
        /// cache. Lock-free: the flag is flipped with
        /// <see cref="System.Threading.Interlocked"/>, so the game-loop
        /// thread that signals and the render thread that reloads
        /// coordinate without a lock (T083).
        /// </summary>
        public void NotifyConfigurationChanged()
        {
            System.Threading.Interlocked.Exchange(ref _configurationCacheInvalidated, 1);
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
        /// Validates the save identifier and returns the persistence key
        /// of the mod-folder config.json. The key is the installation-
        /// wide constant (<see cref="ConfigDataKey"/>) — the
        /// configuration scope is the mod folder, shared by every save,
        /// so the key never incorporates the save identifier (true-global
        /// per spec Session 2026-10-06). The identifier is still
        /// validated so a garbage identifier fails fast exactly as the
        /// per-save keying did.
        /// </summary>
        /// <param name="saveId">Raw save identifier, validated but not keyed.</param>
        /// <returns>The mod-folder configuration persistence key.</returns>
        /// <exception cref="ArgumentException">Thrown when the save identifier does not sanitize to a usable value.</exception>
        private string BuildDataKey(string saveId)
        {
            var sanitized = _sanitizationService.Sanitize(saveId);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                throw new ArgumentException("Sanitized saveId is empty.", nameof(saveId));
            }
            return ConfigDataKey;
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

                PoorColor = ValidateColor(configuration.PoorColor, defaults.PoorColor, nameof(VisualizationConfiguration.PoorColor)),
                ModerateColor = ValidateColor(configuration.ModerateColor, defaults.ModerateColor, nameof(VisualizationConfiguration.ModerateColor)),
                HealthyColor = ValidateColor(configuration.HealthyColor, defaults.HealthyColor, nameof(VisualizationConfiguration.HealthyColor)),
                UnknownColor = ValidateColor(configuration.UnknownColor, defaults.UnknownColor, nameof(VisualizationConfiguration.UnknownColor))
            };

            if (!IsValidOpacity(configuration.Opacity))
            {
                _monitor.Log(
                    $"VisualizationConfigurationService: invalid opacity {configuration.Opacity} replaced with default {result.Opacity}.",
                    LogLevel.Warn);
            }

            if (!IsValidDegradationMode(configuration.AccessibilityDegradation))
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
        /// Replaces an invalid color with its ModConstants default, warning once per field.
        /// </summary>
        /// <param name="color">Color to validate.</param>
        /// <param name="defaultColor">Default color to substitute when invalid.</param>
        /// <param name="fieldName">Configuration field name, used in the warning.</param>
        /// <returns>The original color when valid; otherwise the default color.</returns>
        private ColorDTO ValidateColor(ColorDTO color, ColorDTO defaultColor, string fieldName)
        {
            if (IsValidColor(color))
            {
                return color;
            }

            _monitor.Log(
                $"VisualizationConfigurationService: invalid {fieldName} ({color.R}, {color.G}, {color.B}, {color.A}) replaced with default ({defaultColor.R}, {defaultColor.G}, {defaultColor.B}, {defaultColor.A}).",
                LogLevel.Warn);
            return defaultColor;
        }

        /// <summary>
        /// Checks whether a color is usable for rendering.
        /// ColorDTO channels are bytes, so out-of-range channels cannot occur;
        /// the invalid states are the all-zero "unset" struct and zero alpha,
        /// which would render an invisible overlay.
        /// </summary>
        /// <param name="color">Color to check.</param>
        /// <returns>True when the color carries a non-zero alpha channel.</returns>
        private static bool IsValidColor(ColorDTO color)
        {
            return color.A != 0;
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
