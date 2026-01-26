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
        private float _overlayOpacity = 0.3f;

        /// <summary>
        /// Initializes a new instance of VisualizationConfig.
        /// </summary>
        /// <param name="monitor">Monitor for logging</param>
        /// <param name="modDataService">Service for data persistence</param>
        public VisualizationConfig(IMonitor monitor, IModDataService modDataService)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _modDataService =
                modDataService ?? throw new ArgumentNullException(nameof(modDataService));

            Load();
        }

        /// <inheritdoc/>
        public bool ShowTileOverlays { get; set; } = true;

        /// <inheritdoc/>
        public bool ShowOverlay { get; set; } = true;

        /// <inheritdoc/>
        public bool ShowHoverTooltips { get; set; } = true;

        /// <inheritdoc/>
        public bool ShowHoeFeedback { get; set; } = true;

        /// <inheritdoc/>
        public float OverlayOpacity
        {
            get => _overlayOpacity;
            set => _overlayOpacity = Math.Clamp(value, 0.0f, 1.0f);
        }

        /// <inheritdoc/>
        public bool UseCustomColors { get; set; } = false;

        /// <inheritdoc/>
        public Color PoorHealthColor { get; set; } = new Color(139, 69, 19); // SaddleBrown

        /// <inheritdoc/>
        public Color ModerateHealthColor { get; set; } = new Color(218, 165, 32); // GoldenRod

        /// <inheritdoc/>
        public Color HealthyHealthColor { get; set; } = new Color(85, 107, 47); // DarkOliveGreen

        /// <inheritdoc/>
        public void Load()
        {
            try
            {
                var configData = _modDataService.LoadData<VisualizationConfigData>(ConfigKey);

                if (configData == null)
                {
                    _monitor.Log("No visualization config found, using defaults.", LogLevel.Trace);
                    _monitor.Log(
                        $"[CONFIG] ShowTileOverlays: {ShowTileOverlays}, ShowHoverTooltips: {ShowHoverTooltips}, ShowHoeFeedback: {ShowHoeFeedback}, OverlayOpacity: {_overlayOpacity}, ShowOverlay: {ShowOverlay}",
                        LogLevel.Info
                    );
                    return;
                }

                // Load and validate configuration values
                ShowTileOverlays = configData.ShowTileOverlays;
                ShowHoverTooltips = configData.ShowHoverTooltips;
                ShowHoeFeedback = configData.ShowHoeFeedback;
                ShowOverlay = configData.ShowOverlay;

                _overlayOpacity = ClampOpacity(configData.OverlayOpacity);
                UseCustomColors = configData.UseCustomColors;

                PoorHealthColor = ValidateColor(configData.PoorHealthColor, PoorHealthColor);
                ModerateHealthColor = ValidateColor(
                    configData.ModerateHealthColor,
                    ModerateHealthColor
                );
                HealthyHealthColor = ValidateColor(
                    configData.HealthyHealthColor,
                    HealthyHealthColor
                );

                _monitor.Log("Visualization configuration loaded successfully.", LogLevel.Trace);
                _monitor.Log(
                    $"[CONFIG] ShowTileOverlays: {ShowTileOverlays}, ShowHoverTooltips: {ShowHoverTooltips}, ShowHoeFeedback: {ShowHoeFeedback}, OverlayOpacity: {_overlayOpacity}, ShowOverlay: {ShowOverlay}",
                    LogLevel.Info
                );
            }
            catch (Exception ex)
            {
                _monitor.Log(
                    "Error loading visualization configuration, using defaults.",
                    LogLevel.Error
                );
                _monitor.Log(
                    $"Load exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
                _monitor.Log(
                    $"[CONFIG] ShowTileOverlays: {ShowTileOverlays}, ShowHoverTooltips: {ShowHoverTooltips}, ShowHoeFeedback: {ShowHoeFeedback}, OverlayOpacity: {_overlayOpacity}, ShowOverlay: {ShowOverlay}",
                    LogLevel.Info
                );
            }
        }

        /// <inheritdoc/>
        public void Save()
        {
            try
            {
                var configData = new VisualizationConfigData
                {
                    ShowTileOverlays = ShowTileOverlays,
                    ShowHoverTooltips = ShowHoverTooltips,
                    ShowHoeFeedback = ShowHoeFeedback,
                    OverlayOpacity = _overlayOpacity,
                    UseCustomColors = UseCustomColors,
                    ShowOverlay = ShowOverlay,
                    PoorHealthColor = new ColorData(PoorHealthColor),
                    ModerateHealthColor = new ColorData(ModerateHealthColor),
                    HealthyHealthColor = new ColorData(HealthyHealthColor),
                };

                _modDataService.SaveData(configData, ConfigKey);
                _monitor.Log("Visualization configuration saved successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error saving visualization configuration.", LogLevel.Error);
                _monitor.Log(
                    $"Save exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
            }
        }

        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            try
            {
                ShowTileOverlays = true;
                ShowHoverTooltips = true;
                ShowHoeFeedback = true;
                ShowOverlay = true;
                _overlayOpacity = 0.3f;
                UseCustomColors = false;
                PoorHealthColor = new Color(139, 69, 19); // SaddleBrown
                ModerateHealthColor = new Color(218, 165, 32); // GoldenRod
                HealthyHealthColor = new Color(85, 107, 47); // DarkOliveGreen

                Save();
                _monitor.Log("Visualization configuration reset to defaults.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log("Error resetting visualization configuration.", LogLevel.Error);
                _monitor.Log(
                    $"Reset exception type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})",
                    LogLevel.Trace
                );
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
        public bool ShowOverlay { get; set; } = true;
        public ColorData? PoorHealthColor { get; set; }
        public ColorData? ModerateHealthColor { get; set; }
        public ColorData? HealthyHealthColor { get; set; }
    }

    /// <summary>
    /// Color data structure for JSON serialization.
    /// </summary>
    public class ColorData
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        // Parameterless constructor for deserialization
        public ColorData() { }

        public ColorData(Color color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
        }
    }
}
