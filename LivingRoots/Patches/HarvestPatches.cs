using System;
using HarmonyLib;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
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
        private static IVisualizationService? visualizationService;

        // State captured by the Prefix and consumed by the Postfix (contract §3).
        private sealed class HarvestState
        {
            public bool Skip;
            public HarvestQualityDecision Decision;
            public HarvestQualityInputs Inputs;
            public Farmer? Who;
            public bool IsInventoryFull;
            public HoeDirt? Soil;
            public int XTile;
            public int YTile;
        }

        public static void Initialize(IMonitor modMonitor, IHarvestQualityService harvestService, ISoilHealthService? soilService = null, IVisualizationService? vizService = null)
        {
            monitor = modMonitor;
            service = harvestService ?? new HarvestQualityService();
            soilHealthService = soilService;
            visualizationService = vizService;
        }

        // Typed signature matching Crop.harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest)
        public static bool Harvest_Prefix(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest, ref object __state)
        {
            var state = new HarvestState { Soil = soil, XTile = xTile, YTile = yTile };
            __state = state;
            try
            {
                if (service == null)
                {
                    service = new HarvestQualityService();
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

                // Crop scope: fibre seeds and Qi Fruit (A6)
                bool isFibreSeed = false;
                try
                {
                    if (crop.indexOfHarvest.Value == "771")
                    {
                        isFibreSeed = true;
                    }
                }
                catch
                {
                    // Best-effort.
                }
                bool isQiFruit = false; // Qi fruit is a forage crop, already excluded above

                if (isFibreSeed || isQiFruit)
                {
                    state.Skip = true;
                    return true; // OutOfScope
                }

                // Soil health from tile
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

                // Inventory-full detection (best-effort heuristic)
                Farmer? who = null;
                if (!isForcedScytheHarvest)
                {
                    who = Game1.player;
                }
                bool isInventoryFull = false;
                if (who != null && who.Items != null)
                {
                    try
                    {
                        isInventoryFull = who.Items.Count >= 36;
                    }
                    catch
                    {
                        isInventoryFull = false;
                    }
                }

                // Read inputs from crop/farmer/soil
                int farmingLevel = who != null ? who.farmingLevel.Value : 0;
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

                // Fertilizer tier from soil
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
                    HarvestMaxIncreasePerFarmingLevel = 0,
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
                state.IsInventoryFull = isInventoryFull;
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

                // NOTE (T056, deferred): Junimo/forced-scythe harvests have no Farmer
                // inventory (who == null), so Poor/Healthy Quality application below is
                // skipped on that path; Quality stays vanilla. Full Junimo support
                // requires intercepting the harvested item from the Junimo hut and is
                // deferred until a game-runtime verification pass (Gate 3).

                var decision = state.Decision;
                var who = state.Who;
                var isInventoryFull = state.IsInventoryFull;
                var inputs = state.Inputs;

                bool enhancedQualityOccurred = false;
                bool extraHarvestOccurred = false;

                if (decision.Tier == HarvestTier.Poor && decision.ForceQualityZero)
                {
                    if (who != null && who.Items != null && who.Items.Count > 0)
                    {
                        try
                        {
                            var last = who.Items[who.Items.Count - 1];
                            if (last != null)
                            {
                                last.Quality = 0;
                            }
                        }
                        catch
                        {
                            // Best-effort.
                        }
                    }
                    if (monitor != null)
                    {
                        monitor.Log("Living Roots: Poor soil — harvest quality forced to normal.", LogLevel.Trace);
                    }
                }

                if (decision.Tier == HarvestTier.Healthy)
                {
                    if (who != null && who.Items != null && who.Items.Count > 0)
                    {
                        try
                        {
                            var last = who.Items[who.Items.Count - 1];
                            if (last != null && last.Quality < 4)
                            {
                                float vanillaP = 0.2f * (inputs.FarmingLevel / 10.0f) + 0.2f * inputs.FertilizerTier * ((inputs.FarmingLevel + 2) / 12.0f) + 0.01f;
                                int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays ^ (int)(Game1.uniqueIDForThisGame & 0xFFFFFFFF) ^ (inputs.FertilizerTier * 31);
                                double roll = Math.Abs(hash % 10000) / 10000.0;
                                float boosted = service.BoostQualityProbability(vanillaP, decision);
                                if (roll < boosted)
                                {
                                    last.Quality = Math.Min(4, last.Quality + 1);
                                    enhancedQualityOccurred = true;
                                }
                            }
                        }
                        catch
                        {
                            // Best-effort.
                        }
                    }

                    // Extra harvest: apply boosted chance deterministically
                    try
                    {
                        int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays ^ (int)(Game1.uniqueIDForThisGame & 0xFFFFFFFF) ^ 0x5A827999;
                        double roll = Math.Abs(hash % 10000) / 10000.0;
                        float boostedChance = service.BoostExtraHarvestChance(0.0f, decision);
                        if (roll < boostedChance && who != null && who.Items != null)
                        {
                            extraHarvestOccurred = true;
                            if (monitor != null)
                            {
                                monitor.Log("Living Roots: Extra harvest triggered.", LogLevel.Trace);
                            }

                            // Spawn the extra harvest item (host-authoritative, single-harvest guarantee).
                            try
                            {
                                if (who.Items.Count > 0)
                                {
                                    var source = who.Items[who.Items.Count - 1];
                                    if (source != null)
                                    {
                                        var extra = source.getOne();
                                        if (extra != null)
                                        {
                                            extra.Quality = 0; // A2: extra-harvest items are normal quality (0)
                                            bool added = false;
                                            try
                                            {
                                                added = who.addItemToInventoryBool(extra, false);
                                            }
                                            catch
                                            {
                                                added = false;
                                            }
                                            if (!added)
                                            {
                                                // Inventory full: drop as debris (vanilla semantics).
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
                                        }
                                    }
                                }
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

                    if (monitor != null)
                    {
                        monitor.Log("Living Roots: Living soil — quality boost applied.", LogLevel.Trace);
                    }
                }

                // Inventory-full handling (FR-008)
                if (isInventoryFull)
                {
                    if (decision.MainItemBlocked && monitor != null)
                    {
                        monitor.Log("Living Roots: Inventory full — main item blocked (vanilla).", LogLevel.Trace);
                    }
                    if (decision.ExtraItemDroppedAsDebris && monitor != null)
                    {
                        monitor.Log("Living Roots: Inventory full — extra item dropped as debris.", LogLevel.Trace);
                    }
                }

                // Feedback resolution (B1/B2 conditionality)
                var feedback = service.ResolveFeedback(decision, enhancedQualityOccurred, extraHarvestOccurred);
                if (feedback != HarvestFeedbackFlags.None && monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest feedback: {0}", feedback), LogLevel.Trace);
                }

                // Drive feedback broadcast (contract §6): shared outcomes broadcast
                // to all players; local-only input stays on the triggering client.
                if (feedback != HarvestFeedbackFlags.None)
                {
                    try
                    {
                        var location = soil != null ? soil.Location : null;
                        if (location == null)
                        {
                            location = Game1.currentLocation;
                        }

                        // Shared (broadcast) outcome: play sound + broadcast sprites (host-only).
                        string? soundName = null;
                        if (feedback.HasFlag(HarvestFeedbackFlags.Poor))
                        {
                            soundName = "cancel";
                        }
                        else if (feedback.HasFlag(HarvestFeedbackFlags.LivingQuality) || feedback.HasFlag(HarvestFeedbackFlags.ExtraHarvest))
                        {
                            soundName = "coin";
                        }

                        if (location != null && !string.IsNullOrEmpty(soundName))
                        {
                            try
                            {
                                location.playSound(soundName);
                            }
                            catch
                            {
                                // Best-effort.
                            }
                        }

                        if (location != null)
                        {
                            try
                            {
                                object? mp;
                                var mpField = typeof(Game1).GetField("multiplayer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                                var mpProp = mpField == null ? typeof(Game1).GetProperty("multiplayer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) : null;
                                if (mpField != null)
                                {
                                    mp = mpField.GetValue(null);
                                }
                                else if (mpProp != null)
                                {
                                    mp = mpProp.GetValue(null);
                                }
                                else
                                {
                                    mp = null;
                                }
                                if (mp != null)
                                {
                                    var method = mp.GetType().GetMethod("broadcastSprites", new[] { typeof(GameLocation), typeof(List<TemporaryAnimatedSprite>) });
                                    if (method != null)
                                    {
                                        var sprites = new List<TemporaryAnimatedSprite>();
                                        var worldPosition = new Vector2(xTile * (float)ModConstants.TileSize + 32f, yTile * (float)ModConstants.TileSize);
                                        // FR-007 (non-chromatic): tier info is carried by text + SFX, not color alone.
                                        if (feedback.HasFlag(HarvestFeedbackFlags.Poor))
                                        {
                                            var poorSprite = new TemporaryAnimatedSprite(null, Rectangle.Empty, 1000f, 1, 1, worldPosition, false, false);
                                            poorSprite.text = "Soil too poor for quality crops";
                                            poorSprite.color = Color.Red;
                                            poorSprite.alphaFade = 0.02f;
                                            poorSprite.motion = new Vector2(0, -0.5f);
                                            sprites.Add(poorSprite);
                                        }
                                        if (feedback.HasFlag(HarvestFeedbackFlags.LivingQuality))
                                        {
                                            var livingSprite = new TemporaryAnimatedSprite(null, Rectangle.Empty, 1000f, 1, 1, worldPosition, false, false);
                                            livingSprite.text = "Living Soil";
                                            livingSprite.color = Color.LimeGreen;
                                            livingSprite.alphaFade = 0.02f;
                                            livingSprite.motion = new Vector2(0, -0.5f);
                                            sprites.Add(livingSprite);
                                        }
                                        if (feedback.HasFlag(HarvestFeedbackFlags.ExtraHarvest))
                                        {
                                            var extraSprite = new TemporaryAnimatedSprite(null, Rectangle.Empty, 1000f, 1, 1, worldPosition, false, false);
                                            extraSprite.text = "Extra harvest!";
                                            extraSprite.color = Color.Gold;
                                            extraSprite.alphaFade = 0.02f;
                                            extraSprite.motion = new Vector2(0, -0.5f);
                                            sprites.Add(extraSprite);
                                        }
                                        foreach (var s in sprites)
                                        {
                                            try { location.TemporarySprites.Add(s); } catch { /* best-effort */ }
                                        }
                                        method.Invoke(mp, new object[] { location, sprites });
                                    }
                                }
                            }
                            catch
                            {
                                // Best-effort.
                            }
                        }

                        if (visualizationService != null)
                        {
                            try
                            {
                                visualizationService.InvalidateCache();
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
            }
            catch (Exception ex)
            {
                if (monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest postfix patch failed; skipping modification. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
                }
            }
        }
    }
}
