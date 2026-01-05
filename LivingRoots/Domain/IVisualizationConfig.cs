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
        /// Gets whether tile color overlays are enabled.
        /// </summary>
        bool ShowTileOverlays { get; }

        /// <summary>
        /// Gets whether hover tooltips are enabled.
        /// </summary>
        bool ShowHoverTooltips { get; }

        /// <summary>
        /// Gets whether hoe action feedback is enabled.
        /// </summary>
        bool ShowHoeFeedback { get; }

        /// <summary>
        /// Gets the opacity of tile overlays (0.0 to 1.0).
        /// </summary>
        float OverlayOpacity { get; }

        /// <summary>
        /// Gets whether to use custom colors.
        /// </summary>
        bool UseCustomColors { get; }

        /// <summary>
        /// Gets the color for poor soil health (0-33).
        /// </summary>
        Color PoorHealthColor { get; }

        /// <summary>
        /// Gets the color for moderate soil health (34-66).
        /// </summary>
        Color ModerateHealthColor { get; }

        /// <summary>
        /// Gets the color for healthy soil health (67-100).
        /// </summary>
        Color HealthyHealthColor { get; }

        /// <summary>
        /// Loads configuration from the config file.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves configuration to the config file.
        /// </summary>
        void Save();

        /// <summary>
        /// Resets configuration to default values.
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Gets the duration for hoe action feedback display.
        /// </summary>
        /// <returns>Feedback duration in milliseconds</returns>
        long GetHoeFeedbackDuration();
    }
}
