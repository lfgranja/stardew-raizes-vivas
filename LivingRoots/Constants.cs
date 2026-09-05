using System;
using Microsoft.Xna.Framework;

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

        // Additional constants for security and performance
        public const int MaxDataKeyLength = 200; // Maximum length for generated data keys (prefix + sanitized saveId)
        public const int MaxTilesPerSave = 30000; // Global limit for tile processing across all locations to prevent DoS attacks (slightly above theoretical max of 50 * 500 = 25,000)

        // Additional constants for security and performance
        public const int MaxPathSegmentLength = 100; // Maximum length for individual path segments to prevent DoS attacks
        public const int MaxPathDepth = 10; // Maximum depth for path traversal to prevent DoS attacks
        public const int MaxFileNameLength = 255; // Standard maximum file name length to prevent OS issues
        public const int MaxDataValueSizeBytes = 1024 * 1024; // 1MB maximum size for data values to prevent memory exhaustion

        // Visualization Constants
        public static readonly Color PoorColor = new Color(255, 0, 0, 255);     // Red for Poor category (0-33)
        public static readonly Color ModerateColor = new Color(255, 255, 0, 255);  // Yellow for Moderate category (34-66)
        public static readonly Color HealthyColor = new Color(0, 255, 0, 255);   // Green for Healthy category (67-100)
        public static readonly Color UnknownColor = new Color(128, 128, 128, 255);   // Gray for unknown/unavailable data
        public const float DefaultOpacity = 0.5f;
        public const float PatternMinOpacity = 0.7f;
        public const int FlashDurationMs = 300;
        public const int TextDurationMs = 1000;
        public const double MaxRenderTimeMs = 16.67;
        public const int TooltipDebounceMs = 50;
        public const bool OverlaysEnabledDefault = true;
        public const bool TooltipsEnabledDefault = true;
        public const bool HoeFeedbackEnabledDefault = true;
    }
}
