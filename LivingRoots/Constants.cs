using System;

namespace LivingRoots
{
    public static class ModConstants
    {
        // Soil Health Constants
        public const string KeyPrefix = "soil_health_data_";
        public const float MinSoilHealth = 0f;
        public const float MaxSoilHealth = 100f;  // Changed from 10 to 100 to align with documentation

        // Visualization defaults
        public const float DefaultOverlayOpacity = 0.3f;
        public const int PoorHealthThreshold = 33;
        public const int ModerateHealthThreshold = 66;
        public const int HealthyHealthThreshold = 100;

        // Security Constants
        public const int MaxTilesPerLocation = 5000; // Maximum number of tiles allowed per location to prevent DoS attacks
        public const int MaxLocationsPerSave = 50; // Maximum number of locations allowed per save to prevent DoS attacks
        public const int MaxAbsoluteTileCoordinate = 10000; // Maximum absolute value for tile coordinates to prevent malicious save files
        public const int MaxSaveIdLength = 200; // Maximum length for save IDs to prevent overlong filenames
        public const int MaxLocationNameLength = 100; // Maximum length for location names to prevent overlong strings

        // Additional constants for security and performance
        public const int MaxDataKeyLength = 200; // Maximum length for generated data keys (prefix + sanitized saveId)
        public const int MaxTilesPerSave = 300000; // Global limit for tile processing across all locations to prevent DoS attacks (slightly above theoretical max of 50 * 500 = 25,000)

        // Additional constants for security and performance
        public const int MaxPathSegmentLength = 100; // Maximum length for individual path segments to prevent DoS attacks
        public const int MaxPathDepth = 10; // Maximum depth for path traversal to prevent DoS attacks
        public const int MaxFileNameLength = 255; // Standard maximum file name length to prevent OS issues
        public const int MaxDataValueSizeBytes = 1024 * 1024; // 1MB maximum size for data values to prevent memory exhaustion

        // Performance settings
        public const int TileHealthCacheSize = 100; // Maximum cache size for tile health values
        public const int CacheClearIntervalSeconds = 5; // Interval in seconds to clear the health cache
        public const int PerformanceLogIntervalMinutes = 1; // Interval in minutes to log performance metrics
    }
}
