using System;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Patches;

/// <summary>
/// Spec 020 — Sementes Crioulas plant-time seam. Harmony postfix on
/// <c>HoeDirt.plant</c> fires only for the crioulo seed, applies the bounded
/// +HeirloomPlantBonus write host-authoritatively (Constitution VI), or routes
/// a typed farmhand→host request. State mutation is bounded, atomic (first-wins
/// per tile via the shared host lock), and farm-scoped. Feedback failures are
/// swallowed; the write itself fails closed (no bonus, no false success).
/// </summary>
public static class HeirloomPatches
{
    private static IMonitor? monitor;
    private static ISoilHealthService? soilHealthService;
    private static LivingRoots.Domain.Interfaces.IHostAtomicLockService? hostLockService;
    private static IModHelper? helper;
    private static IManifest? manifest;
    private static Func<string, string>? localizer; // key → localized string (EN/PT parity managed by i18n files)
    private static Action? broadcastSync;
    private static Func<string>? dayKeyProvider;

    /// <summary>Wires the seam. Called from <c>ModEntry.RegisterHarmonyPatch</c>-adjacent setup after the harvest seam.</summary>
    public static void Initialize(
        IMonitor modMonitor,
        ISoilHealthService soil,
        LivingRoots.Domain.Interfaces.IHostAtomicLockService? hostLock,
        IModHelper? modHelper,
        IManifest? modManifest,
        Func<string, string>? localize,
        Action? broadcastSoilSync,
        Func<string>? dayKey)
    {
        monitor = modMonitor ?? throw new ArgumentNullException(nameof(modMonitor));
        soilHealthService = soil ?? throw new ArgumentNullException(nameof(soil));
        hostLockService = hostLock;
        helper = modHelper;
        manifest = modManifest;
        localizer = localize;
        broadcastSync = broadcastSoilSync;
        dayKeyProvider = dayKey;
    }

    /// <summary>Postfix for HoeDirt.plant — heirloom seed only, never a reason to break the game.</summary>
    public static void HeirloomPlant_Postfix(HoeDirt __instance, string itemId, Farmer who, bool isFertilizer, bool __result)
    {
        try
        {
            if (!__result || isFertilizer || __instance == null || who == null)
            {
                return;
            }

            if (!HeirloomIdentity.IsHeirloomSeedId(itemId))
            {
                return;
            }

            var location = __instance.Location;
            if (location == null)
            {
                return;
            }

            var tile = __instance.Tile;
            var locationName = location.Name;
            var farmerId = who.UniqueMultiplayerID;

            if (IsMainPlayerSafe())
            {
                ApplyHostPlantBonus(locationName, tile, farmerId);
                return;
            }

            // Farmhand: never mutate the shared soil directly (Constitution VI).
            if (SendPlantBonusRequest(locationName, tile, itemId))
            {
                return;
            }

            // Messaging unavailable — honest, non-misleading local copy; the plant
            // itself happened (game-side), the soil bonus did not.
            ShowAuthorityUnsupported();
        }
        catch (Exception ex)
        {
            // Postfix must not crash the game; trace only.
            PatchTraceLog.Log(monitor, "heirloom plant postfix failed", ex);
        }
    }

    private static void ApplyHostPlantBonus(string locationName, Vector2 tile, long farmerId)
    {
        if (soilHealthService == null || hostLockService == null)
        {
            // Fail closed: no host authority ⇒ no shared write (Constitution VI / FR-012).
            PatchTraceLog.Log(monitor, "heirloom plant bonus skipped: host lock or soil service missing", null);
            return;
        }

        if (!hostLockService.TryAcquireLock(locationName, tile, farmerId))
        {
            // First-wins; a concurrent plant on this tile was already serviced.
            // No false success is shown; the tile converges through the normal sync.
            return;
        }

        try
        {
            var before = soilHealthService.GetSoilHealth(locationName, tile);
            var after = HeirloomSoilMath.Plant(before);
            var point = new Point((int)tile.X, (int)tile.Y);
            soilHealthService.TrySetSoilHealth(locationName, point, after);
        }
        finally
        {
            hostLockService.ReleaseLock(locationName, tile, farmerId);
        }

        try
        {
            broadcastSync?.Invoke();
        }
        catch (Exception ex)
        {
            PatchTraceLog.Log(monitor, "heirloom plant: sync broadcast failed", ex);
        }
    }

    private static bool SendPlantBonusRequest(string locationName, Vector2 tile, string itemId)
    {
        var multiplayer = helper?.Multiplayer;
        if (multiplayer == null || manifest == null)
        {
            return false;
        }

        var dayKey = dayKeyProvider?.Invoke() ?? GetDayKeyFallback();
        var request = new HeirloomPlantBonusRequest(
            locationName,
            (int)tile.X,
            (int)tile.Y,
            dayKey,
            itemId);

        try
        {
            multiplayer.SendMessage(request, ModConstants.HeirloomPlantBonusRequestMessageType, modIDs: new[] { manifest.UniqueID });
            return true;
        }
        catch (Exception ex)
        {
            PatchTraceLog.Log(monitor, "heirloom plant bonus request send failed", ex);
            return false;
        }
    }

    private static void ShowAuthorityUnsupported()
    {
        var text = localizer?.Invoke(ModConstants.TranslationKeyHeirloomPlantBonusAuthorityUnsupported);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Only the host can renew the soil for you in multiplayer, so the soil bonus was not applied. The seed was still planted — ask the host to care for the shared soil.";
        }

        Game1.showRedMessage(text);
    }

    private static bool IsMainPlayerSafe()
    {
        try
        {
            return Context.IsMainPlayer;
        }
        catch
        {
            return false;
        }
    }

    private static string GetDayKeyFallback()
    {
        try
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", Game1.currentSeason, Game1.dayOfMonth, Game1.year);
        }
        catch
        {
            return "unknown";
        }
    }
}
