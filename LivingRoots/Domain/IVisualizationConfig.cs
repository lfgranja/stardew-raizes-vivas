using Microsoft.Xna.Framework;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Interface for visualization configuration.
    /// Provides access to visualization settings and preferences.
    /// </summary>
    public interface IVisualizationConfig
    {
        /// <summary>
        /// Gets or sets whether tile color overlays are enabled.
        /// </summary>
        bool ShowTileOverlays { get; set; }

        /// <summary>
        /// Gets or sets whether hover tooltips are enabled.
        /// </summary>
        bool ShowHoverTooltips { get; set; }

        /// <summary>
        /// Gets or sets whether hoe action feedback is enabled.
        /// </summary>
        bool ShowHoeFeedback { get; set; }

        /// <summary>
        /// Gets or sets opacity of tile overlays (0.0 to 1.0).
        /// </summary>
        float OverlayOpacity { get; set; }

        /// <summary>
        /// Gets or sets whether to use custom colors.
        /// </summary>
        bool UseCustomColors { get; set; }

        /// <summary>
        /// Gets or sets whether soil health overlay is enabled.
        /// </summary>
        bool ShowOverlay { get; set; }

        /// <summary>
        /// Gets or sets color for poor soil health (0-33).
        /// </summary>
        Color PoorHealthColor { get; set; }

        /// <summary>
        /// Gets or sets color for moderate soil health (34-66).
        /// </summary>
        Color ModerateHealthColor { get; set; }

        /// <summary>
        /// Gets or sets color for healthy soil health (67-100).
        /// </summary>
        Color HealthyHealthColor { get; set; }

        /// <summary>
        /// Loads configuration from config file.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves configuration to config file.
        /// </summary>
        void Save();

        /// <summary>
        /// Resets configuration to default values.
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Gets duration for hoe action feedback display.
        /// </summary>
        /// <returns>Feedback duration in milliseconds</returns>
        long GetHoeFeedbackDuration();
    }
}
