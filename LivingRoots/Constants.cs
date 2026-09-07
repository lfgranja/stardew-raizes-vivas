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
        public static readonly Color PoorColor = Color.Red; // #FF0000
        public static readonly Color ModerateColor = Color.Yellow; // #FFFF00
        public static readonly Color HealthyColor = Color.Green; // #00FF00
        public static readonly Color UnknownColor = Color.Gray; // #808080
        public const float DefaultOpacity = 0.5f;
        public const float PatternMinOpacity = 0.7f;
        public const int FlashDurationMs = 300;
        public const int TextDurationMs = 1000;
        public const double MaxRenderTimeMs = 16.67;
        public const int TooltipDebounceMs = 50;
        public const bool OverlaysEnabledDefault = true;
        public const bool TooltipsEnabledDefault = true;
        public const bool HoeFeedbackEnabledDefault = true;

        // Decay & Compost Constants
        public const string CompostingBinKeyPrefix = "composting_bins_";
        public const float DailyDecayRate = 2f;
        public const float RestorationAmount = 15f;
        public const int ProcessingDurationMinutes = 2880; // 2 full days × 1440 min/day
        public const int MaturationMaxLevel = 5;
        public const int MaturationIdleResetDays = 14;
        public const int MaturationIncrementDays = 7; // days of continuous operation per level
        public const string CompostItemId = "LivingRoots.Compost";
        public const string CompostingBinItemId = "LivingRoots.CompostingBin";
        public const string CompostingBinRecipeId = "LivingRoots_CompostingBin";
        public const string CompostCraftingTab = "Home";
        public const int CompostingBinWoodCost = 50;
        public const int CompostingBinStoneCost = 25;
        public const int CompostingBinFiberCost = 15;
        public const string CompostCraftingSound = "axe";
        public const int CompostCategoryId = -26;
        public const int CompostStackMax = 999;
    }
}
