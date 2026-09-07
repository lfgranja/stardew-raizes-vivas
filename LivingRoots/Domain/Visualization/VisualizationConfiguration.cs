namespace LivingRoots.Domain.Visualization
{
    /// <summary>
    /// Stores user-configurable visualization settings.
    /// Persisted as JSON per-save.
    /// </summary>
    public class VisualizationConfiguration
    {
        public bool OverlaysEnabled { get; set; } = true;
        public bool TooltipsEnabled { get; set; } = true;
        public bool HoeFeedbackEnabled { get; set; } = true;
        public float Opacity { get; set; } = 0.5f;

        public ColorDTO PoorColor { get; set; } = new ColorDTO { R = 255, G = 0, B = 0, A = 255 };
        public ColorDTO ModerateColor { get; set; } = new ColorDTO { R = 255, G = 255, B = 0, A = 255 };
        public ColorDTO HealthyColor { get; set; } = new ColorDTO { R = 0, G = 255, B = 0, A = 255 };
        public ColorDTO UnknownColor { get; set; } = new ColorDTO { R = 128, G = 128, B = 128, A = 255 };

        public bool ShowPatterns { get; set; } = true;
        public string AccessibilityDegradation { get; set; } = "auto";
    }
}
