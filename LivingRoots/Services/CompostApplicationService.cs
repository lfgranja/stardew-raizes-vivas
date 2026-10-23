using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services;

public class CompostApplicationService(
    ISoilHealthService soilHealthService,
    IPlayerProvider playerProvider,
    IMonitor monitor,
    ITranslationHelper? translationHelper = null) : ICompostApplicationService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IPlayerProvider _playerProvider = playerProvider ?? throw new ArgumentNullException(nameof(playerProvider));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

    // T054 (FR-008a): SMAPI translation helper for refusal messages.
    // Optional so headless test doubles (which have no translation
    // pipeline) keep constructing the service; a null helper falls
    // back to the raw key, which is the pre-T054 behaviour.
    private readonly ITranslationHelper? _translationHelper = translationHelper;

    public bool TryApplyCompost(GameLocation location, Vector2 tile)
    {
        var locationName = location.Name ?? "Unknown";

        if (!location.IsFarm && locationName != "Greenhouse")
        {
            _monitor.Log($"Compost rejected: {locationName} is not farm or Greenhouse.", LogLevel.Trace);
            Game1.playSound("cancel");
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostInvalidTile));
            return false;
        }

        if (!location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
            || terrainFeature is not HoeDirt)
        {
            _monitor.Log("Compost rejected: target tile is not tilled soil.", LogLevel.Trace);
            Game1.playSound("cancel");
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostInvalidTile));
            return false;
        }

        var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
        // FR-014: an absent record is the unknown sentinel (float.NaN) — "no data", not a
        // 0% reading. Resolve it to the same 0 baseline the producer's UpdateHealth applies
        // to an absent record (the first application on unmeasured soil lands at exactly the
        // restoration amount), so the sentinel never reaches the arithmetic or the summary:
        // NaN >= MaxSoilHealth is false, so the maximum-health rejection would not catch it,
        // and the applied health and the "{currentHealth:F1} -> {newHealth:F1}" line would
        // otherwise interpolate NaN.
        if (float.IsNaN(currentHealth))
        {
            currentHealth = 0f;
        }
        if (currentHealth >= ModConstants.MaxSoilHealth)
        {
            _monitor.Log("Compost rejected: tile already at maximum health.", LogLevel.Trace);
            Game1.playSound("cancel");
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostMaxHealth));
            return false;
        }

        var heldItem = _playerProvider.CurrentItem;
        if (heldItem == null ||
            (heldItem.QualifiedItemId != ModConstants.CompostItemId &&
             heldItem.QualifiedItemId != ModConstants.QualifiedCompostItemId &&
             heldItem.ItemId != ModConstants.CompostItemId))
        {
            _monitor.Log("Compost rejected: player not holding compost.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var newHealth = Math.Min(currentHealth + ModConstants.RestorationAmount, ModConstants.MaxSoilHealth);
        _soilHealthService.UpdateHealth(locationName, tile, ModConstants.RestorationAmount);

        heldItem.Stack--;

        var worldPosition = new Vector2(tile.X * (float)ModConstants.TileSize + 32f, tile.Y * (float)ModConstants.TileSize);
        var sprite = new TemporaryAnimatedSprite(
            null,
            Rectangle.Empty,
            1000f,
            1,
            1,
            worldPosition,
            false,
            false);
        sprite.text = $"+{ModConstants.RestorationAmount:F0}";
        sprite.color = ModConstants.CompostSpriteColor;
        sprite.alphaFade = 0.02f;
        sprite.motion = new Vector2(0, -0.5f);
        location.TemporarySprites.Add(sprite);

        _monitor.Log($"Compost applied at ({tile.X}, {tile.Y}) in {locationName}: " +
                     $"{currentHealth:F1} -> {newHealth:F1} (+{ModConstants.RestorationAmount})",
                     LogLevel.Trace);
        return true;
    }

    /// <summary>
    /// Resolves a refusal translation key (T054, FR-008a) through the
    /// injected <see cref="ITranslationHelper"/> so the red HUD message
    /// renders in the player's language. Falls back to the raw key when
    /// no helper was injected (headless test doubles), which preserves
    /// the pre-T054 behaviour for those hosts. Resolved per call, never
    /// cached — the locale can change mid-session.
    /// </summary>
    private string TranslateRefusalMessage(string messageKey)
    {
        if (_translationHelper == null)
        {
            return messageKey;
        }

        return _translationHelper.Get(messageKey);
    }
}
