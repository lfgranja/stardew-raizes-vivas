using System;

namespace LivingRoots
{
    public static class ModConstants
    {
        // Soil Health Constants
        public const string KeyPrefix = "soil_health_data_";
        public const float MinSoilHealth = 0f;
        public const float MaxSoilHealth = 100f;  // Changed from 10 to 100 to align with documentation
        
        // Security Constants
        public const int MaxTilesPerLocation = 500; // Maximum number of tiles allowed per location to prevent DoS attacks
        public const int MaxLocationsPerSave = 50; // Maximum number of locations allowed per save to prevent DoS attacks
        public const int MaxAbsoluteTileCoordinate = 10000; // Maximum absolute value for tile coordinates to prevent malicious save files
        public const int MaxSaveIdLength = 200; // Maximum length for save IDs to prevent overlong filenames
        public const int MaxLocationNameLength = 100; // Maximum length for location names to prevent overlong strings
        
        // New constants from code review feedback
        public const int MaxDataKeyLength = 200; // Maximum length for generated data keys (prefix + sanitized saveId)
        public const int MaxTilesPerSave = 100000; // Global limit for tile processing across all locations to prevent DoS attacks
    }
}