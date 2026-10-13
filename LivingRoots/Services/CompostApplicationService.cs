using LivingRoots.Domain;
using LivingRoots.Domain.Services;
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
    /// <summary>
    /// T054/T149 (FR-008a): SMAPI's translation helper. Null only when a
    /// caller constructs the service without it (legacy test doubles);
    /// <see cref="TranslateRefusalMessage"/> then falls back to the key so
    /// no refusal site ever silently drops its message.
    /// </summary>
    private readonly ITranslationHelper? _translationHelper = translationHelper;

    /// <summary>
    /// Resolves a refusal message through SMAPI's translation helper so the
    /// player sees their own language (FR-008a). The helper is resolved per
    /// call, never cached, because the locale can change mid-session.
    /// </summary>
    /// <param name="messageKey">Translation key registered in i18n/default.json.</param>
    /// <returns>The localized text, or the key itself when no helper is wired.</returns>
    private string TranslateRefusalMessage(string messageKey)
    {
        if (_translationHelper == null)
        {
            return messageKey;
        }

        return _translationHelper.Get(messageKey);
    }

    /// <summary>
    /// T057/FR-007: the player is holding compost when the held item resolves
    /// to it in any of the forms the item takes — the qualified id the game
    /// composes while a type definition is registered ("(O)LivingRoots.Compost"),
    /// the fully qualified constant, or the bare ItemId a test host (or an
    /// item created without a registry) reports. Checking one form only
    /// silently refuses a legitimately held compost.
    /// </summary>
    private static bool HoldsCompost(Item heldItem)
    {
        return heldItem.QualifiedItemId == ModConstants.CompostItemId
            || heldItem.QualifiedItemId == ModConstants.QualifiedCompostItemId
            || heldItem.ItemId == ModConstants.CompostItemId
            || heldItem.ItemId == ModConstants.QualifiedCompostItemId;
    }

    /// <summary>
    /// T197 (FR-002): the location gate for compost application, using the same
    /// greenhouse-like acceptance rule <c>ModController.TryPlaceBin</c> accepts
    /// placement with (T105) — a modded greenhouse is a greenhouse too. The farm
    /// always accepts, a greenhouse-like location accepts, and every other
    /// location is refused with the invalid-tile refusal.
    /// </summary>
    /// <remarks>
    /// The rule itself lives in <see cref="GreenhouseLocationRule"/> so placement
    /// and application can never disagree: a bin placed in a modded greenhouse
    /// must have its compost applicable there too.
    /// </remarks>
    /// <param name="location">The location compost is being applied in.</param>
    /// <param name="locationName">The resolved name used in the refusal text.</param>
    /// <returns>
    /// <c>false</c> when the location refuses application and the invalid-tile
    /// refusal has been rendered, sounded and logged — <c>true</c> when the caller
    /// may proceed.
    /// </returns>
    private bool ValidateLocation(GameLocation location, string locationName)
    {
        if (!location.IsFarm && !GreenhouseLocationRule.IsGreenhouseLike(location))
        {
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostInvalidTile));
            _monitor.Log($"Compost rejected: {locationName} is not farm or greenhouse-like.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        return true;
    }

    public bool TryApplyCompost(GameLocation location, Vector2 tile)
    {
        var locationName = location.Name ?? "Unknown";

        if (!ValidateLocation(location, locationName))
        {
            return false;
        }

        if (!location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
            || terrainFeature is not HoeDirt)
        {
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostInvalidTile));
            _monitor.Log("Compost rejected: target tile is not tilled soil.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
        if (currentHealth >= ModConstants.MaxSoilHealth)
        {
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostMaxHealth));
            _monitor.Log("Compost rejected: tile already at maximum health.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var heldItem = _playerProvider.CurrentItem;
        if (heldItem == null || !HoldsCompost(heldItem))
        {
            Game1.showRedMessage(TranslateRefusalMessage(ModConstants.TranslationKeyCompostInvalidTile));
            _monitor.Log("Compost rejected: player not holding compost.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var newHealth = Math.Min(currentHealth + ModConstants.RestorationAmount, ModConstants.MaxSoilHealth);
        _soilHealthService.UpdateHealth(locationName, tile, ModConstants.RestorationAmount);

        heldItem.Stack--;

        var worldPosition = new Vector2(tile.X * 64f + 32f, tile.Y * 64f);
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
        sprite.color = Color.Green;
        sprite.alphaFade = 0.02f;
        sprite.motion = new Vector2(0, -0.5f);
        location.TemporarySprites.Add(sprite);

        _monitor.Log($"Compost applied at ({tile.X}, {tile.Y}) in {locationName}: " +
                     $"{currentHealth:F1} -> {newHealth:F1} (+{ModConstants.RestorationAmount})",
                     LogLevel.Trace);
        return true;
    }
}

// FR-008a assertion: refusal messages must never expose raw translation keys
#if DEBUG
internal static class TranslationSafetyAssert
{
    public static void AssertNoRawKey(string message)
    {
        if (message != null && (message.Contains("TranslationKey") || message.Contains("key.") || message.StartsWith("com")))
            System.Diagnostics.Debug.Assert(false, "FR-008a: raw translation key leaked: " + message);
    }
}
#endif
