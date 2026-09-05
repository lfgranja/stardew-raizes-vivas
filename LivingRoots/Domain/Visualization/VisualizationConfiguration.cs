using Microsoft.Xna.Framework;

namespace LivingRoots.Domain.Visualization
{
    public class VisualizationConfiguration
    {
        public bool OverlaysEnabled { get; set; } = true;
        public bool TooltipsEnabled { get; set; } = true;
        public bool HoeFeedbackEnabled { get; set; } = true;
        public float Opacity { get; set; } = 0.5f;
        public bool ShowPatterns { get; set; } = true;
        public string AccessibilityDegradation { get; set; } = "auto";
        public ColorDTO PoorColor { get; set; } = new ColorDTO(255, 0, 0, 255);
        public ColorDTO ModerateColor { get; set; } = new ColorDTO(255, 255, 0, 255);
        public ColorDTO HealthyColor { get; set; } = new ColorDTO(0, 255, 0, 255);
        public ColorDTO UnknownColor { get; set; } = new ColorDTO(128, 128, 128, 255);
    }
}
