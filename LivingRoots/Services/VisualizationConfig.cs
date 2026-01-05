using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LivingRoots.Services
{
    /// <summary>
    /// Implementation of visualization configuration with JSON-based persistence.
    /// Provides configuration management for soil health visualization features.
    /// </summary>
    public class VisualizationConfig : IVisualizationConfig
    {
        // Configuration key for storing visualization settings
        private const string ConfigKey = "visualization_config";

        // Dependencies
        private readonly IMonitor _monitor;
        private readonly IModDataService _modDataService;

        // Configuration values with defaults
        private bool _showTileOverlays = true;
        private bool _showHoverTooltips = true;
        private bool _showHoeFeedback = true;
        private float _overlayOpacity = 0.3f;
        private bool _useCustomColors = false;
        private Color _poorHealthColor = new Color(139, 69, 19);  // SaddleBrown
        private Color _moderateHealthColor = new Color(218, 165, 32);  // GoldenRod
        private Color _healthyHealthColor = new Color(85, 107, 47);  // DarkOliveGreen

        /// <summary>
        /// Initializes a new instance of VisualizationConfig.
        /// </summary>
        /// <param name="monitor">Monitor for logging</param>
        /// <param name="modDataService">Service for data persistence</param>
        public VisualizationConfig(IMonitor monitor, IModDataService modDataService)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _modDataService = modDataService ?? throw new ArgumentNullException(nameof(modDataService));

            Load();
        }

        /// <inheritdoc/>
        public bool ShowTileOverlays => _showTileOverlays;

        /// <inheritdoc/>
        public bool ShowHoverTooltips => _showHoverTooltips;

        /// <inheritdoc/>
        public bool ShowHoeFeedback => _showHoeFeedback;

        /// <inheritdoc/>
        public float OverlayOpacity => _overlayOpacity;

        /// <inheritdoc/>
        public bool UseCustomColors => _useCustomColors;

        /// <inheritdoc/>
        public Color PoorHealthColor => _poorHealthColor;

        /// <inheritdoc/>
        public Color ModerateHealthColor => _moderateHealthColor;

        /// <inheritdoc/>
        public Color HealthyHealthColor => _healthyHealthColor;

        /// <inheritdoc/>
        public void Load()
        {
            try
            {
                var configData = _modDataService.LoadData<VisualizationConfigData>(ConfigKey);

                if (configData == null)
                {
                    _monitor.Log("No visualization config found, using defaults.", LogLevel.Trace);
                    return;
                }

                // Load and validate configuration values
                _showTileOverlays = configData.ShowTileOverlays;
                _showHoverTooltips = configData.ShowHoverTooltips;
                _showHoeFeedback = configData.ShowHoeFeedback;

                _overlayOpacity = ClampOpacity(configData.OverlayOpacity);
                _useCustomColors = configData.UseCustomColors;

                _poorHealthColor = ValidateColor(configData.PoorHealthColor, _poorHealthColor);
                _moderateHealthColor = ValidateColor(configData.ModerateHealthColor, _moderateHealthColor);
                _healthyHealthColor = ValidateColor(configData.HealthyHealthColor, _healthyHealthColor);

                _monitor.Log("Visualization configuration loaded successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error loading visualization configuration, using defaults.", LogLevel.Error);
                _monitor.Log($"Load exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <inheritdoc/>
        public void Save()
        {
            try
            {
                var configData = new VisualizationConfigData
                {
                    ShowTileOverlays = _showTileOverlays,
                    ShowHoverTooltips = _showHoverTooltips,
                    ShowHoeFeedback = _showHoeFeedback,
                    OverlayOpacity = _overlayOpacity,
                    UseCustomColors = _useCustomColors,
                    PoorHealthColor = new ColorData(_poorHealthColor),
                    ModerateHealthColor = new ColorData(_moderateHealthColor),
                    HealthyHealthColor = new ColorData(_healthyHealthColor)
                };

                _modDataService.SaveData(configData, ConfigKey);
                _monitor.Log("Visualization configuration saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error saving visualization configuration.", LogLevel.Error);
                _monitor.Log($"Save exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            try
            {
                _showTileOverlays = true;
                _showHoverTooltips = true;
                _showHoeFeedback = true;
                _overlayOpacity = 0.3f;
                _useCustomColors = false;
                _poorHealthColor = new Color(139, 69, 19);  // SaddleBrown
                _moderateHealthColor = new Color(218, 165, 32);  // GoldenRod
                _healthyHealthColor = new Color(85, 107, 47);  // DarkOliveGreen

                Save();
                _monitor.Log("Visualization configuration reset to defaults.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error resetting visualization configuration.", LogLevel.Error);
                _monitor.Log($"Reset exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Clamps opacity value to valid range [0.0, 1.0].
        /// </summary>
        /// <param name="value">The opacity value to clamp</param>
        /// <returns>Clamped opacity value</returns>
        private static float ClampOpacity(float value)
        {
            return Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <inheritdoc/>
        public long GetHoeFeedbackDuration()
        {
            // Fixed duration for hoe action feedback (approximately 1 second)
            // In a full implementation, this could be made configurable
            return 1000; // 1000 milliseconds = 1 second
        }

        /// <summary>
        /// Validates and returns color, using default if invalid.
        /// </summary>
        /// <param name="colorData">The color data to validate</param>
        /// <param name="defaultColor">The default color to use if validation fails</param>
        /// <returns>Valid color or default</returns>
        private static Color ValidateColor(ColorData? colorData, Color defaultColor)
        {
            if (colorData == null)
                return defaultColor;

            try
            {
                return new Color(colorData.R, colorData.G, colorData.B, colorData.A);
            }
            catch
            {
                return defaultColor;
            }
        }
    }

    /// <summary>
    /// Data class for serialization of visualization configuration.
    /// </summary>
    public class VisualizationConfigData
    {
        public bool ShowTileOverlays { get; set; } = true;
        public bool ShowHoverTooltips { get; set; } = true;
        public bool ShowHoeFeedback { get; set; } = true;
        public float OverlayOpacity { get; set; } = 0.3f;
        public bool UseCustomColors { get; set; } = false;
        public ColorData? PoorHealthColor { get; set; }
        public ColorData? ModerateHealthColor { get; set; }
        public ColorData? HealthyHealthColor { get; set; }
    }

    /// <summary>
    /// Color data structure for JSON serialization.
    /// </summary>
    /// <remarks>
    /// Initializes a new ColorData instance from a Color.
    /// </remarks>
    /// <param name="color">The color to convert</param>
    public class ColorData(Color color)
    {
        public byte R { get; set; } = color.R;
        public byte G { get; set; } = color.G;
        public byte B { get; set; } = color.B;
        public byte A { get; set; } = color.A;
    }
}
