namespace LivingRoots.Domain.Visualization;

/// <summary>
/// User-configurable visualization settings.
/// Defaults are sourced from <c>ModConstants</c> (earth-tone palette, clarification 127).
/// Invalid persisted values are replaced with defaults on load per FR-006.
/// </summary>
public class VisualizationConfiguration
{
    /// <summary>Master toggle for overlay rendering.</summary>
    public bool OverlaysEnabled { get; set; } = ModConstants.OverlaysEnabledDefault;

    /// <summary>Master toggle for hover tooltips.</summary>
    public bool TooltipsEnabled { get; set; } = ModConstants.TooltipsEnabledDefault;

    /// <summary>Master toggle for hoe action feedback.</summary>
    public bool HoeFeedbackEnabled { get; set; } = ModConstants.HoeFeedbackEnabledDefault;

    /// <summary>Overlay transparency level (0.0-1.0).</summary>
    public float Opacity { get; set; } = ModConstants.DefaultOpacity;

    /// <summary>Base color for the Poor category.</summary>
    public ColorDTO PoorColor { get; set; } = ToColorDTO(ModConstants.PoorColor);

    /// <summary>Base color for the Moderate category.</summary>
    public ColorDTO ModerateColor { get; set; } = ToColorDTO(ModConstants.ModerateColor);

    /// <summary>Base color for the Healthy category.</summary>
    public ColorDTO HealthyColor { get; set; } = ToColorDTO(ModConstants.HealthyColor);

    /// <summary>Color used when health data is unavailable.</summary>
    public ColorDTO UnknownColor { get; set; } = ToColorDTO(ModConstants.UnknownColor);

    /// <summary>Enable pattern overlays for accessibility.</summary>
    public bool ShowPatterns { get; set; } = true;

    /// <summary>
    /// Behavior when the visible tile count exceeds the degradation threshold.
    /// One of "auto", "never", or "notify". Null is treated as "auto".
    /// </summary>
    public string? AccessibilityDegradation { get; set; } = "auto";

    /// <summary>
    /// Converts an XNA color constant into the serializable domain representation.
    /// </summary>
    /// <param name="color">Color constant from <c>ModConstants</c>.</param>
    /// <returns>Equivalent <see cref="ColorDTO"/>.</returns>
    private static ColorDTO ToColorDTO(Microsoft.Xna.Framework.Color color)
    {
        return new ColorDTO(color.R, color.G, color.B, color.A);
    }
}
