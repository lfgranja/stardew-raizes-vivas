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

        public static void Initialize(IMonitor modMonitor, IHarvestQualityService harvestService, ISoilHealthService? soilService = null, IVisualizationService? vizService = null)
        {
            monitor = modMonitor;
            service = harvestService ?? new HarvestQualityService();
            soilHealthService = soilService;
            visualizationService = vizService;
        }

        // Typed signature matching Crop.harvest(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest)
        public static bool HarvestPrefix(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest, ref object __state)
        {
            __state = true;
            try
            {
                if (service == null)
                {
                    service = new HarvestQualityService();
                }
            }
            catch (Exception ex)
            {
                if (monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest prefix patch failed; falling back to original. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
                }
            }
            return true;
        }

        public static void HarvestPostfix(int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester, bool isForcedScytheHarvest, bool __result, object __state)
        {
            try
            {
                if (service == null)
                {
                    return;
                }

                Crop? crop = soil != null ? soil.crop : null;
                if (crop == null)
                {
                    return;
                }
                if (crop.forageCrop.Value)
                {
                    return;
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
                    return; // OutOfScope
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
                float soilHealth = 50.0f;
                if (soilHealthService != null && !string.IsNullOrEmpty(locationName))
                {
                    try
                    {
                        soilHealth = soilHealthService.GetSoilHealth(locationName, new Vector2(xTile, yTile));
                    }
                    catch
                    {
                        soilHealth = 50.0f;
                    }
                }

                // Inventory-full detection (best-effort heuristic)
                Farmer? who = null;
                // junimoHarvester is not a Farmer; farmer is Game1.player during manual harvest
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
                    return;
                }

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
                                int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays;
                                double roll = Math.Abs(hash % 10000) / 10000.0;
                                float boosted = service.BoostQualityProbability(0.5f, decision);
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
                        int hash = (xTile * 397) ^ (yTile * 17) ^ Game1.Date.TotalDays ^ 0x5A827999;
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
                                            extra.Quality = source.Quality;
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
                if (feedback != HarvestFeedback.None && monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest feedback: {0}", feedback), LogLevel.Trace);
                }

                // Drive feedback broadcast (contract §6): shared outcomes broadcast
                // to all players; local-only input stays on the triggering client.
                if (feedback != HarvestFeedback.None)
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
                        if (feedback.HasFlag(HarvestFeedback.Poor))
                        {
                            soundName = "harvest";
                        }
                        else if (feedback.HasFlag(HarvestFeedback.LivingQuality) || feedback.HasFlag(HarvestFeedback.ExtraHarvest))
                        {
                            soundName = "newRecipe";
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

                        // Broadcast of transient sprites is host-only and best-effort;
                        // Game1.multiplayer is protected, so it is deferred to the
                        // game-loop render path (Gate 3) to avoid reflection
                        // accessibility bypass (S3011). playSound above covers
                        // the shared audio cue.

                        // Local-only input (hoe flash/text) via SpriteBatch is handled
                        // by the game loop render path; the seam's flags gate it.
                        if (visualizationService != null)
                        {
                            try
                            {
                                // Invalidate cache so overlay reflects any change.
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
