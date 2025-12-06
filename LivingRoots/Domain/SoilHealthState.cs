using System.Collections.Generic;

namespace LivingRoots.Domain
{
    /// <summary>
    /// Represents the persisted state of soil health for the entire save.
    /// Key: Location Name (e.g., "Farm")
    /// Value: Dictionary mapping Tile Coordinates "X,Y" to Health Value (float)
    /// </summary>
    public class SoilHealthState
    {
        public Dictionary<string, Dictionary<string, float>> LocationHealthData { get; set; } = new();
    }
}