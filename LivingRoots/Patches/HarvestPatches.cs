using System;
using System.Collections.Generic;
using HarmonyLib;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using LivingRoots.Services.Visualization;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Patches
{
    public static class HarvestPatches
    {
        private static IMonitor? monitor;
        private static IHarvestQualityService? service;
        private static ISoilHealthService? soilHealthService;
        // Spec 020: crioulo harvest-time rules. Optional — when null the seam is
        // inert (e.g., test doubles). Initialized via InitializeHeirloomRules.
        private static LivingRoots.Domain.Interfaces.IHostAtomicLockService? heirloomHostLock;
        private static Func<Farmer?, Domain.IPlayerInventory?>? heirloomInventoryFactory;
        private static Func<string>? heirloomDayKeyProvider;
        private static Action? heirloomBroadcastSync;
        private static IModHelper? heirloomHelper;
        private static IManifest? heirloomManifest;

        private sealed class HarvestState
        {
            public bool Skip;
            public HarvestQualityDecision Decision;
            public HarvestQualityInputs Inputs;
            public Farmer? Who;
            public HoeDirt? Soil;
            public int XTile;
            public int YTile;
            public IList<Item?>? TargetInventory;
            public IReadOnlyList<InventorySlotSnapshot>? BeforeSnapshot;
        }

        public static void Initialize(IMonitor modMonitor, IHarvestQualityService harvestService, ISoilHealthService? soilService = null)
        {
            monitor = modMonitor ?? throw new ArgumentNullException(nameof(modMonitor));
            service = harvestService ?? throw new ArgumentNullException(nameof(harvestService));
            soilHealthService = soilService;
        }

        /// <summary>Spec 020: wires the crioulo harvest-time seam (seed-save + Healthy-band bonus).</summary>
        public static void InitializeHeirloomRules(
            LivingRoots.Domain.Interfaces.IHostAtomicLockService? hostLock,
            Func<Farmer?, Domain.IPlayerInventory?>? inventoryFactory,
            Func<string>? dayKeyProvider,
            Action? broadcastSync,
            IModHelper? modHelper,
            IManifest? modManifest)
        {
            heirloomHostLock = hostLock;
            heirloomInventoryFactory = inventoryFactory;
            heirloomDayKeyProvider = dayKeyProvider;
            heirloomBroadcastSync = broadcastSync;
            heirloomHelper = modHelper;
            heirloomManifest = modManifest;
        }

        public static bool Harvest_Prefix(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest, ref object __state)
        {
            var state = new HarvestState { Soil = soil, XTile = xTile, YTile = yTile };
            __state = state;
            try
            {
                if (service == null)
                {
                    state.Skip = true;
                    return true;
                }

                Crop? crop = soil != null ? soil.crop : null;
                if (crop == null)
                {
                    state.Skip = true;
                    return true;
                }

                if (crop.forageCrop.Value)
                {
                    state.Skip = true;
                    return true;
                }

                bool isFibreSeed = false;
                bool isQiFruit = false;
                try
                {
                    string harvestId = crop.indexOfHarvest?.Value ?? string.Empty;
                    isFibreSeed = string.Equals(harvestId, "771", StringComparison.Ordinal);
                    isQiFruit = string.Equals(harvestId, "890", StringComparison.Ordinal);
                }
                catch
                {
                    // Best-effort.
                }

                if (isFibreSeed || isQiFruit)
                {
                    state.Skip = true;
                    return true;
                }

                string locationName = string.Empty;
                if (soil != null && soil.Location != null)
                {
                    locationName = soil.Location.Name;
                }
                else if (Game1.currentLocation != null)
                {
                    locationName = Game1.currentLocation.Name;
                }

                float soilHealth = float.NaN;
                if (soilHealthService != null && !string.IsNullOrEmpty(locationName))
                {
                    try
                    {
                        soilHealth = soilHealthService.GetSoilHealth(locationName, new Vector2(xTile, yTile));
                    }
                    catch
                    {
                        soilHealth = float.NaN;
                    }
                }

                Farmer? who = junimoHarvester == null ? Game1.player : null;
                IList<Item?>? targetInventory = GetTargetInventory(who, junimoHarvester);
                state.TargetInventory = targetInventory;
                state.BeforeSnapshot = HarvestInventoryDelta.Snapshot(ToReadOnly(targetInventory));

                int maxItems = GetMaxItems(who);
                bool isInventoryFull = targetInventory != null && HarvestInventoryDelta.IsFull(ToReadOnly(targetInventory), maxItems);

                int farmingLevel = who != null ? who.FarmingLevel : 0;
                bool isPaddyCrop = false;
                try
                {
                    isPaddyCrop = crop.isPaddyCrop();
                }
                catch
                {
                    isPaddyCrop = false;
                }

                int regrowDays = 0;
                try
                {
                    if (crop.RegrowsAfterHarvest())
                    {
                        regrowDays = crop.phaseDays.Count > 0 ? crop.phaseDays[crop.phaseDays.Count - 1] : 0;
                    }
                }
                catch
                {
                    regrowDays = 0;
                }

                int harvestMaxIncreasePerFarmingLevel = 0;
                try
                {
                    var data = crop.GetData();
                    if (data != null)
                    {
                        harvestMaxIncreasePerFarmingLevel = (int)data.HarvestMaxIncreasePerFarmingLevel;
                    }
                }
                catch
                {
                    // Best-effort.
                }

                int fertilizerTier = 0;
                if (soil != null && soil.fertilizer.Value != null)
                {
                    string fert = soil.fertilizer.Value.ToLowerInvariant();
                    if (fert.Contains("deluxe"))
                    {
                        fertilizerTier = 3;
                    }
                    else if (fert.Contains("quality"))
                    {
                        fertilizerTier = 2;
                    }
                    else if (fert.Contains("fertilizer") || fert.Contains("speed") || fert.Contains("retain"))
                    {
                        fertilizerTier = 1;
                    }
                }

                var inputs = new HarvestQualityInputs
                {
                    FarmingLevel = farmingLevel,
                    FertilizerTier = fertilizerTier,
                    IsPaddyCrop = isPaddyCrop,
                    RegrowDays = regrowDays,
                    HarvestMaxIncreasePerFarmingLevel = harvestMaxIncreasePerFarmingLevel,
                    IsForageCrop = crop.forageCrop.Value,
                    IsFibreSeed = isFibreSeed,
                    IsQiFruit = isQiFruit,
                    PestQualityPenalty = null,
                    IsInventoryFull = isInventoryFull
                };

                var decision = service.Decide(soilHealth, inputs);
                if (!decision.IsInScope)
                {
                    state.Skip = true;
                    return true;
                }

                state.Skip = false;
                state.Decision = decision;
                state.Inputs = inputs;
                state.Who = who;
            }
            catch (Exception ex)
            {
                if (monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest prefix patch failed; falling back to original. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
                }

                state.Skip = true;
            }

            return true;
        }

        public static void Harvest_Postfix(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest, bool __result, object __state)
        {
            try
            {
                if (service == null)
                {
                    return;
                }

                var state = __state as HarvestState;
                if (state == null || state.Skip)
                {
                    return;
                }

                var decision = state.Decision;
                var inputs = state.Inputs;
                var targetInventory = state.TargetInventory;
                var afterSnapshot = HarvestInventoryDelta.Snapshot(ToReadOnly(targetInventory));
                var changes = state.BeforeSnapshot != null
                    ? HarvestInventoryDelta.Diff(state.BeforeSnapshot, afterSnapshot)
                    : new List<HarvestInventoryChange>();
                int primarySlot = HarvestInventoryDelta.FindPrimaryAddedSlot(changes);

                bool enhancedQualityOccurred = false;
                bool extraHarvestOccurred = false;

                if (!__result && primarySlot < 0)
                {
                    // Harvest was blocked and nothing was added.
                    DoFeedbackBroadcast(soil, xTile, yTile, service.BuildFeedbackPlan(decision, false, false, IsMainPlayerSafe()));
                    return;
                }

                if (decision.Tier == HarvestTier.Poor && decision.ForceQualityZero && primarySlot >= 0 && targetInventory != null)
                {
                    try
                    {
                        if (targetInventory[primarySlot] is StardewValley.Object obj)
                        {
                            obj.Quality = 0;
                        }
                    }
                    catch
                    {
                        // Best-effort.
                    }
                }

                if (decision.Tier == HarvestTier.Healthy && primarySlot >= 0 && targetInventory != null)
                {
                    if (targetInventory[primarySlot] is StardewValley.Object item && item.Quality < 4)
                    {
                        try
                        {
                            float vanillaP = HarvestQualityMath.ComputeBaselineProbability(inputs.FarmingLevel, inputs.FertilizerTier, inputs.IsPaddyCrop, inputs.RegrowDays);
                            int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays ^ (int)(Game1.uniqueIDForThisGame & 0xFFFFFFFF) ^ (inputs.FertilizerTier * 31);
                            double roll = Math.Abs(hash % 10000) / 10000.0;
                            float boosted = service.BoostQualityProbability(vanillaP, decision);
                            if (roll < boosted)
                            {
                                item.Quality = Math.Min(4, item.Quality + 1);
                                enhancedQualityOccurred = true;
                            }
                        }
                        catch
                        {
                            // Best-effort.
                        }
                    }

                    try
                    {
                        float baseExtra = GetBaseExtraHarvestChance(soil?.crop);
                        float boostedChance = service.BoostExtraHarvestChance(baseExtra, decision);
                        if (boostedChance > 0f)
                        {
                            int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays ^ (int)(Game1.uniqueIDForThisGame & 0xFFFFFFFF) ^ 0x5A827999;
                            double roll = Math.Abs(hash % 10000) / 10000.0;
                            if (roll < boostedChance && primarySlot >= 0 && targetInventory != null)
                            {
                                extraHarvestOccurred = true;
                                var source = targetInventory[primarySlot];
                                if (source != null)
                                {
                                    var extra = source.getOne();
                                    if (extra != null)
                                    {
                                        extra.Quality = 0;
                                        bool added = TryAddToTarget(extra, state.Who, junimoHarvester);
                                        if (!added)
                                        {
                                            DropAsDebris(extra, soil, xTile, yTile);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Best-effort.
                    }
                }

                var plan = service.BuildFeedbackPlan(decision, enhancedQualityOccurred, extraHarvestOccurred, IsMainPlayerSafe());
                DoFeedbackBroadcast(soil, xTile, yTile, plan);

                // Spec 020: crioulo rules ride the same seam (014 outcome untouched).
                ApplyHeirloomRules(soil, xTile, yTile, junimoHarvester, __result);
            }
            catch (Exception ex)
            {
                if (monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest postfix patch failed; skipping modification. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
                }
            }
        }

        /// <summary>Spec 020: heirloom harvest arm — deterministic seed save (exactly one) + Flat Healthy-band soil bonus (host-authoritative). Never throws.</summary>
        private static void ApplyHeirloomRules(HoeDirt? soil, int xTile, int yTile, JunimoHarvester junimoHarvester, bool harvestSucceeded)
        {
            try
            {
                if (soil == null || !harvestSucceeded)
                {
                    return;
                }

                var crop = soil.crop;
                var seedId = crop?.netSeedIndex?.Value;
                if (!HeirloomIdentity.IsHeirloomSeedId(seedId))
                {
                    return;
                }

                var locationName = soil.Location?.Name;
                var isJunimo = junimoHarvester != null;

                // (a) Deterministic seed save — FE-009. Junimos never grant (no player inventory).
                if (!isJunimo)
                {
                    try
                    {
                        Farmer? who = Game1.player;
                        var inventory = heirloomInventoryFactory?.Invoke(who);
                        var seedItem = ItemRegistry.Create(ModConstants.QualifiedHeirloomItemId, ModConstants.HeirloomSavedSeedYield);
                        var granted = LivingRoots.Services.HeirloomSeedSaveService.TryGrantSavedSeed(seedItem, inventory, null);
                        if (granted)
                        {
                            ShowLocalizedHeirloomMessage(ModConstants.TranslationKeyHeirloomSeedSaved);
                        }
                        else if (who != null)
                        {
                            ShowLocalizedHeirloomMessage(ModConstants.TranslationKeyHeirloomSeedSavedInventoryFull);
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog("heirloom seed save failed", ex);
                    }
                }

                if (string.IsNullOrWhiteSpace(locationName))
                {
                    return;
                }

                var tile = new Vector2(xTile, yTile);

                // (b) Flat +HeirloomHarvestBonus, Healthy band only — HE-2.
                if (IsMainPlayerSafe())
                {
                    ApplyHostHarvestBonus(locationName, tile, Game1.player?.UniqueMultiplayerID ?? 0L);
                }
                else
                {
                    SendHarvestBonusRequest(locationName, tile, seedId!);
                }
            }
            catch (Exception ex)
            {
                TraceLog("heirloom harvest rules failed", ex);
            }
        }

        private static void ApplyHostHarvestBonus(string locationName, Vector2 tile, long farmerId)
        {
            if (soilHealthService == null || heirloomHostLock == null)
            {
                TraceLog("heirloom harvest bonus skipped: soil service or host lock missing", null);
                return;
            }

            if (!heirloomHostLock.TryAcquireLock(locationName, tile, farmerId))
            {
                return; // first-wins; a concurrent actor already serviced the tile
            }

            try
            {
                var before = soilHealthService.GetSoilHealth(locationName, tile);
                if (!float.IsNaN(before) && before >= ModConstants.ModerateHealthyBandEdge)
                {
                    var applied = HeirloomSoilMath.Harvest(before, out float after);
                    if (!applied)
                    {
                        return;
                    }

                    soilHealthService.TrySetSoilHealth(locationName, new Point((int)tile.X, (int)tile.Y), after);
                }
            }
            finally
            {
                heirloomHostLock.ReleaseLock(locationName, tile, farmerId);
            }

            try
            {
                heirloomBroadcastSync?.Invoke();
            }
            catch (Exception ex)
            {
                TraceLog("heirloom harvest bonus: sync broadcast failed", ex);
            }
        }

        private static void SendHarvestBonusRequest(string locationName, Vector2 tile, string itemId)
        {
            var multiplayer = heirloomHelper?.Multiplayer;
            if (multiplayer == null || heirloomManifest == null)
            {
                return; // farmhand messaging unavailable: bonus not applied, plant happened as usual
            }

            var dayKey = heirloomDayKeyProvider?.Invoke() ?? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}:{2}", Game1.currentSeason, Game1.dayOfMonth, Game1.year);
            var request = new HeirloomHarvestBonusRequest(locationName, (int)tile.X, (int)tile.Y, dayKey, itemId);
            try
            {
                multiplayer.SendMessage(request, ModConstants.HeirloomHarvestBonusRequestMessageType, modIDs: new[] { heirloomManifest.UniqueID });
            }
            catch (Exception ex)
            {
                TraceLog("heirloom harvest bonus request send failed", ex);
            }
        }

        private static void ShowLocalizedHeirloomMessage(string key)
        {
            try
            {
                var translation = heirloomHelper?.Translation?.Get(key);
                var text = translation != null ? translation.ToString() : key;
                Game1.showRedMessage(text);
            }
            catch
            {
                // Never throw from feedback.
            }
        }

        private static void TraceLog(string message, Exception? ex)
        {
            try
            {
                if (monitor != null)
                {
                    if (ex != null)
                    {
                        monitor.Log($"{message}: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                    }
                    else
                    {
                        monitor.Log(message, LogLevel.Trace);
                    }
                }
            }
            catch
            {
                // never throw from logging
            }
        }

        private static IReadOnlyList<Item?>? ToReadOnly(IList<Item?>? items)
        {
            if (items == null)
            {
                return null;
            }

            var list = new List<Item?>(items.Count);
            foreach (var item in items)
            {
                list.Add(item);
            }

            return list;
        }

        private static int GetMaxItems(Farmer? who)
        {
            if (who == null)
            {
                return ModConstants.DefaultBackpackSlots;
            }

            try
            {
                return who.MaxItems;
            }
            catch
            {
                return ModConstants.DefaultBackpackSlots;
            }
        }

        private static IList<Item?>? GetTargetInventory(Farmer? who, JunimoHarvester? junimoHarvester)
        {
            if (junimoHarvester != null)
            {
                try
                {
                    var hut = junimoHarvester.home;
                    if (hut?.GetOutputChest() is StardewValley.Objects.Chest chest)
                    {
                        return chest.Items;
                    }
                }
                catch
                {
                    // Best-effort.
                }
            }

            return who?.Items;
        }

        private static float GetBaseExtraHarvestChance(Crop? crop)
        {
            if (crop == null)
            {
                return 0.0f;
            }

            try
            {
                var data = crop.GetData();
                if (data != null)
                {
                    return Math.Clamp((float)data.ExtraHarvestChance, 0.0f, 1.0f);
                }
            }
            catch
            {
                // Best-effort.
            }

            return 0.0f;
        }

        private static bool TryAddToTarget(Item extra, Farmer? who, JunimoHarvester? junimoHarvester)
        {
            try
            {
                if (junimoHarvester != null)
                {
                    var hut = junimoHarvester.home;
                    if (hut?.GetOutputChest() is StardewValley.Objects.Chest chest)
                    {
                        var leftover = chest.addItem(extra);
                        return leftover == null;
                    }
                }

                if (who != null)
                {
                    return who.addItemToInventoryBool(extra, false);
                }
            }
            catch
            {
                // Best-effort.
            }

            return false;
        }

        private static void DropAsDebris(Item extra, HoeDirt? soil, int xTile, int yTile)
        {
            try
            {
                var location = soil != null ? soil.Location : null;
                if (location == null)
                {
                    location = Game1.currentLocation;
                }

                if (location != null)
                {
                    Game1.createItemDebris(extra, new Vector2(xTile + 0.5f, yTile + 0.5f), -1, location);
                }
            }
            catch
            {
                // Best-effort.
            }
        }

        private static void DoFeedbackBroadcast(HoeDirt? soil, int xTile, int yTile, HarvestFeedbackPlan plan)
        {
            try
            {
                if (plan == null || plan.IsEmpty)
                {
                    return;
                }

                var location = soil?.Location ?? Game1.currentLocation;
                if (location != null && !string.IsNullOrEmpty(plan.SoundName))
                {
                    try
                    {
                        location.playSound(plan.SoundName);
                    }
                    catch
                    {
                        // Best-effort.
                    }
                }

                if (location == null)
                {
                    return;
                }

                var sprites = new List<TemporaryAnimatedSprite>();
                var worldPosition = new Vector2(xTile * (float)ModConstants.TileSize + ModConstants.HarvestFeedbackTextOffsetX, yTile * (float)ModConstants.TileSize);
                foreach (var message in plan.Messages)
                {
                    var texture = HarvestFeedbackTextures.GetTexture(message.SpriteKey);
                    var sourceRect = texture != null ? new Rectangle(0, 0, 16, 16) : Rectangle.Empty;
                    var spritePos = texture != null ? worldPosition - new Vector2(0, 18f) : worldPosition;
                    var sprite = new TemporaryAnimatedSprite(texture != null ? texture.Name ?? string.Empty : string.Empty, sourceRect, plan.DurationMs, 1, 1, spritePos, false, false)
                    {
                        texture = texture,
                        sourceRect = sourceRect,
                        text = message.Text,
                        color = GetFeedbackColor(message),
                        alphaFade = 0.02f,
                        motion = new Vector2(0, -0.5f),
                        scale = texture != null ? 2f : 1f
                    };
                    sprites.Add(sprite);
                    try
                    {
                        location.TemporarySprites.Add(sprite);
                    }
                    catch
                    {
                        // Best-effort.
                    }
                }

                if (plan.BroadcastToAllPlayers && sprites.Count > 0)
                {
                    try
                    {
                        object? mp = typeof(Game1).GetField("multiplayer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
                        var method = mp?.GetType().GetMethod("broadcastSprites", new[] { typeof(GameLocation), typeof(List<TemporaryAnimatedSprite>) });
                        method?.Invoke(mp, new object[] { location, sprites });
                    }
                    catch
                    {
                        // Best-effort.
                    }
                }
            }
            catch
            {
                // Best-effort.
            }
        }

        private static bool IsMainPlayerSafe()
        {
            try
            {
                return Context.IsMainPlayer;
            }
            catch
            {
                return true;
            }
        }

        private static Color GetFeedbackColor(HarvestFeedbackMessage message)
        {
            if (string.Equals(message.SpriteKey, "marker-poor", StringComparison.Ordinal))
            {
                return Color.Red;
            }

            if (string.Equals(message.Text, "Extra harvest!", StringComparison.Ordinal))
            {
                return Color.Gold;
            }

            return Color.LimeGreen;
        }
    }
}
