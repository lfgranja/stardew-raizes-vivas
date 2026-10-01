using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization;

/// <summary>
/// Data for hover tooltip rendering.
/// Defaults follow the spec: dark translucent background, white text.
/// </summary>
public class TooltipData
{
    /// <summary>Formatted tooltip text. Null until populated.</summary>
    public string? Text { get; set; }

    /// <summary>Screen position for the tooltip anchor.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Background color. Default dark translucent for ≥4.5:1 readability.</summary>
    public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 217);

    /// <summary>Text color. Default white for maximum readability.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>
    /// Formats known-state tooltip text per spec:
    /// "Soil Health: {percentage}% ({category})".
    /// </summary>
    /// <param name="percentage">Rounded health percentage (0-100).</param>
    /// <param name="category">Category display name.</param>
    /// <returns>Formatted tooltip text.</returns>
    public static string Format(int percentage, string category)
    {
        return $"Soil Health: {percentage}% ({category})";
    }

    /// <summary>
    /// Formats unknown-state tooltip text per FR-014.
    /// Emits no percentage so no specific health value is implied.
    /// </summary>
    /// <returns>Constant unknown-state tooltip text.</returns>
    public static string FormatUnknown()
    {
        return "Soil Health: Unknown";
    }
}
