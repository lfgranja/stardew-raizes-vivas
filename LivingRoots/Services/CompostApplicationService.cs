using LivingRoots.Domain;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Services;

public class CompostApplicationService(
    ISoilHealthService soilHealthService,
    IPlayerProvider playerProvider,
    IMonitor monitor) : ICompostApplicationService
{
    private readonly ISoilHealthService _soilHealthService = soilHealthService ?? throw new ArgumentNullException(nameof(soilHealthService));
    private readonly IPlayerProvider _playerProvider = playerProvider ?? throw new ArgumentNullException(nameof(playerProvider));
    private readonly IMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

    public bool TryApplyCompost(GameLocation location, Vector2 tile)
    {
        var locationName = location.Name ?? "Unknown";

        if (!location.IsFarm && locationName != "Greenhouse")
        {
            Game1.showRedMessage("Compost only on farm or Greenhouse.");
            _monitor.Log($"Compost rejected: {locationName} is not farm or Greenhouse.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        if (!location.terrainFeatures.TryGetValue(tile, out var terrainFeature)
            || terrainFeature is not HoeDirt)
        {
            Game1.showRedMessage("Tile not tilled.");
            _monitor.Log("Compost rejected: target tile is not tilled soil.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var currentHealth = _soilHealthService.GetSoilHealth(locationName, tile);
        if (currentHealth >= ModConstants.MaxSoilHealth)
        {
            Game1.showRedMessage("Soil at max health.");
            _monitor.Log("Compost rejected: tile already at maximum health.", LogLevel.Trace);
            Game1.playSound("cancel");
            return false;
        }

        var heldItem = _playerProvider.CurrentItem;
        if (heldItem == null || heldItem.QualifiedItemId != ModConstants.CompostItemId)
        {
            Game1.showRedMessage("Not holding compost.");
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
