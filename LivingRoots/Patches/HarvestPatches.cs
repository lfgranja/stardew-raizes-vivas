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

                // Drive VisualizationService feedback if available
                if (visualizationService != null && feedback != HarvestFeedback.None)
                {
                    try
                    {
                        // Transient feedback broadcast (shared outcomes) and local-only
                        // input (hoe flash/text) are driven by the game loop; the seam's
                        // flags determine which feedback to show. Game1.multiplayer.
                        // broadcastSprites + location.playSound (host-only) for shared;
                        // SpriteBatch for local-only.
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
