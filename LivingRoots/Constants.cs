using System;

namespace LivingRoots
{
    public static class ModConstants
    {
        // Soil Health Constants
        public const string KeyPrefix = "soil_health_data_";
        public const float MinSoilHealth = 0f;
        public const float MaxSoilHealth = 100f;
        
        // Security Constants
        public const int MaxTilesPerLocation = 500; // Maximum number of tiles allowed per location to prevent DoS attacks
        public const int MaxLocationsPerSave = 50; // Maximum number of locations allowed per save to prevent DoS attacks
    }
}