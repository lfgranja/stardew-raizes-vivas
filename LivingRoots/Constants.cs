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
        public static readonly Color PoorColor = new Color(185, 28, 28); // #B91C1C
        public static readonly Color ModerateColor = new Color(217, 119, 6); // #D97706
        public static readonly Color HealthyColor = new Color(21, 128, 61); // #15803D
        public static readonly Color UnknownColor = new Color(107, 114, 128); // #6B7280
        // FR-008 (T185): the soil-health category band
        // edges the color-interpolation service resolves
        // on — Poor [0, 34), Moderate [34, 67), Healthy
        // [67, 100] — and the Moderate band width that
        // normalizes the Moderate→Healthy interpolation
        // ramp. Single source of truth for
        // ColorInterpolationService's category resolution
        // and both interpolation ramps; the edges pair
        // with the category palette above.
        public const float PoorModerateBandEdge = 34f;
        public const float ModerateHealthyBandEdge = 67f;
        public const float ModerateBandWidth = 33f;
        public const int TileSize = 64;
        public const int ViewportMarginTiles = 1; // FR-018: tiles rendered beyond each viewport edge to prevent pop-in
        public const int DegradationTileThreshold = 1000;
        // T100 / Q6 / FR-009: lower edge of the asymmetric degradation
        // hysteresis band. EXTRAPOLATED — NOT A STANDARD: no published
        // hysteresis band exists for discrete tile counts. 20 tiles
        // (~2% of the 1,000-tile threshold) follows the ~2% cross-domain
        // convention cited in Unreal engine quality-tier blogs and DEIF
        // control-system defaults; the number is a judgement call pending
        // in-game measurement, and this constant is the single tuning seam.
        // Degradation activates above DegradationTileThreshold but
        // deactivates only at or below this edge, so panning across the
        // threshold (e.g. 999 -> 1001 -> 999) cannot oscillate the
        // degradation decision or re-spam the one-time degradation toast.
        public const int DegradationRecoveryThreshold = 980;
        public const float DefaultOpacity = 0.5f;
        // Minimum pattern opacity for accessibility (FR-001). Pattern tones
        // are opaque at PatternAlphaPinned = 1.0 regardless of this value.
        public const float PatternMinOpacity = 0.7f;
        // FR-001 (T131/T139): pattern element opacity is a pinned
        // constant 1.0, independent of the configured overlay
        // opacity — the tones are opaque and are never composited
        // with the fill, so no configuration can make a pattern
        // vanish. Single source of truth for the pinned pattern
        // alpha.
        public const float PatternAlphaPinned = 1.0f;
        // FR-001 (T131/T139): the accessibility pattern tones —
        // opaque white and opaque black at the pinned pattern
        // alpha, never tinted in the category color. At least one
        // tone always meets the 3:1 non-text contrast minimum
        // against any fill composite: the tones' thresholds are
        // complementary (white qualifies when the fill composite
        // luminance is <= 0.30, black when it is >= 0.10, and
        // 0.10 < 0.30).
        public static readonly Color PatternWhiteTone = new(255, 255, 255, (int)(PatternAlphaPinned * 255f));
        public static readonly Color PatternBlackTone = new(0, 0, 0, (int)(PatternAlphaPinned * 255f));
        // FR-002 (T139): the tooltip panel — #000000 at alpha 217,
        // the dark translucent background the tooltip readability
        // spec pins (>=4.5:1 over bright scenes). Single source of
        // truth for the TooltipData default panel.
        public static readonly Color TooltipPanelColor = new(0, 0, 0, 217);
        // FR-002 (T139): the tooltip text drop shadow — #000000 at
        // alpha 128, drawn 1px below-right of the text.
        public static readonly Color TooltipShadowColor = new(0, 0, 0, 128);
        // FR-002 (T185): the tooltip text color — opaque
        // white, the readability pole over the dark panel.
        // Single source of truth for the TooltipRenderer's
        // known-state and unknown-state text color (the
        // TooltipData default stays the same white).
        public static readonly Color TooltipTextColor = new(255, 255, 255);
        // FR-002 (T139): the tooltip panel's offset from the
        // cursor hotspot, in UI-scaled pixels — cursor-adjacent
        // without occluding the cursor hotspot or the hovered
        // tile. Single source of truth for the TooltipRenderer
        // placement offset (ComputeTooltipPosition).
        public const float TooltipOffsetXPixels = 16f;
        public const float TooltipOffsetYPixels = 16f;
        public const int FlashDurationMs = 300;
        public const int TextDurationMs = 1000;
        // FR-003 (T183): the floating text's upward drift
        // rate, in screen pixels per millisecond — a
        // screen-space rate subtracted from the derived
        // screen position (Utility.ModifyCoordinatesForUIScale(
        // Game1.GlobalToLocal(tileCenter))) over the text
        // window. Single source of truth for the drift
        // HoeFeedbackRenderer.ComputeFloatingTextAnchor
        // applies; the zoom-anchor oracle pins the same
        // value (FloatingTextDriftPixelsPerMs).
        public const float FloatingTextDriftPixelsPerMs = 0.02f;
        // FR-003 (T185): the floating health text's base
        // tones — opaque white for the text and opaque
        // black for its 1px drop shadow. The drawn alpha
        // is the fade-window opacity scaled to the byte
        // range at the draw site (HoeFeedbackRenderer),
        // so the text and the shadow fade out together
        // over the text window.
        public static readonly Color FloatingTextColor = new(255, 255, 255);
        public static readonly Color FloatingTextShadowColor = new(0, 0, 0);
        public const double MaxRenderTimeMs = 16.67;
        public const int TooltipDebounceMs = 50;
        public const bool OverlaysEnabledDefault = true;
        public const bool TooltipsEnabledDefault = true;
        public const bool HoeFeedbackEnabledDefault = true;

        // Extended palette for composting, environment
        public static readonly Color CompostEmptyColor = new Color(120, 113, 108);    // #78716C
        public static readonly Color CompostProcessingColor = new Color(161, 98, 7);  // #A16207
        public static readonly Color CompostReadyColor = new Color(22, 101, 52);      // #166534
        public static readonly Color WaterColor = new Color(37, 99, 235);             // #2563EB
        public static readonly Color SunColor = new Color(250, 204, 21);              // #FACC15
        public static readonly Color MulchColor = new Color(146, 64, 14);             // #92400E
        // FR-012 (T185): the compost-application feedback
        // sprite's green — the "+N" restoration text drawn
        // above the treated tile (a text-only
        // TemporaryAnimatedSprite). The vanilla stat-change
        // convention green, kept beside the compost palette
        // but distinct from the soil-overlay states above:
        // it is a sprite color, not an overlay color.
        public static readonly Color CompostSpriteColor = new Color(0, 128, 0);       // #008000

        // Decay & Compost Constants
        public const string CompostingBinKeyPrefix = "composting_bins_";
        public const float DailyDecayRate = 2f;
        public const float RestorationAmount = 15f;
        public const int MaturationDays = 2; // 2 full days for Processing → Ready transition
        public const int MaturationMaxLevel = 5;
        public const int MaturationIdleResetDays = 14;
        public const int MaturationIncrementDays = 7; // days of continuous operation per level
        public const string CompostItemId = "LivingRoots.Compost";
        public const string QualifiedCompostItemId = "(O)LivingRoots.Compost";
        public const string CompostingBinItemId = "LivingRoots.CompostingBin";
        public const string MachineCompostingBinId = "LivingRoots.CompostingBin";
        public const string CompostingBinRecipeId = "LivingRoots.CompostingBinRecipe";
        public const string CompostCraftingTab = "Home";
        public const int CompostingBinWoodCost = 50;
        public const int CompostingBinStoneCost = 25;
        public const int CompostingBinFiberCost = 15;
        public const string CompostCraftingSound = "axe";
        public const int CompostCategoryId = -26;
        public const int CompostStackMax = 999;
        // T078 (FR-005): the compost item icon,
        // served as a named game asset so
        // ObjectData.Texture can point at it.
        // Convention: Mods/<UniqueID>/<name>
        // (manifest UniqueID lfgranja.LivingRoots).
        public const string CompostTextureAssetName = "Mods/lfgranja.LivingRoots/Compost";
        // T099 (FR-001a): the composting bin's
        // texture and its two progress sprites,
        // served as named game assets so the
        // native machine interface renders the
        // mod's bin instead of falling back to
        // the vanilla big-craftable sheet. Same
        // Mods/<UniqueID>/<name> convention; the
        // three names are the asset keys ModEntry
        // serves through e.LoadFromModFile against
        // the three state PNGs in Assets/ (the
        // empty texture plus the processing and
        // ready progress sprites).
        public const string CompostingBinTextureAssetName = "Mods/lfgranja.LivingRoots/CompostingBin";
        public const string CompostingBinProcessingSpriteAssetName = "Mods/lfgranja.LivingRoots/CompostingBin/Processing";
        public const string CompostingBinReadySpriteAssetName = "Mods/lfgranja.LivingRoots/CompostingBin/Ready";

        // ModData Keys
        public const string ModDataInputIdKey = "LivingRoots.InputId";
        public const string ModDataDaysRemainingKey = "LivingRoots.DaysRemaining";

        // Translation keys (i18n/default.json, i18n/en.json) — refusal
        // messages rendered through Game1.showRedMessage (FR-008a, T054).
        // Single source of truth so the services, the controller refusal
        // closure, and the i18n files cannot drift apart.
        public const string TranslationKeyCompostInvalidTile = "message.compost.invalid-tile";
        public const string TranslationKeyCompostMaxHealth = "message.compost.max-health";
        public const string TranslationKeyCompostActionConflict = "message.compost.action-conflict";
        public const string TranslationKeyHarvestInventoryFull = "message.harvest.inventory-full";

        // T136 (FR-008a): the native machine rejection cue for an invalid
        // drop-in — vanilla audio cancel cue plus machine shake. Applies
        // worldObject.shakeTimer directly so no second shake path exists.
        public const int BinInvalidDropInShakeTimerMs = 750;
    }
}
