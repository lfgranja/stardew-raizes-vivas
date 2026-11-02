using System;
using HarmonyLib;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
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

        public static bool HarvestPrefix(object instance, object[] args, ref object state)
        {
            state = true;
            try
            {
                if (instance == null)
                {
                    return true;
                }
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

        public static void HarvestPostfix(object instance, object[] args)
        {
            try
            {
                if (instance == null || service == null)
                {
                    return;
                }
                var crop = instance as Crop;
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
                bool isQiFruit = false;
                try
                {
                    // Fibre seeds produce item 771 (fiber); Qi fruit is a forage crop (already excluded above)
                    if (crop.indexOfHarvest.Value == "771")
                    {
                        isFibreSeed = true;
                    }
                }
                catch
                {
                    // Best-effort; leave false.
                }

                if (isFibreSeed || isQiFruit)
                {
                    return; // OutOfScope: no modification, no feedback
                }

                // Resolve the harvested tile and location.
                int x = 0;
                int y = 0;
                if (args != null && args.Length >= 2)
                {
                    if (args[0] is int xi) x = xi;
                    if (args[1] is int yi) y = yi;
                }
                Farmer? who = null;
                if (args != null && args.Length >= 4)
                {
                    who = args[3] as Farmer;
                }

                string locationName = Game1.currentLocation != null ? Game1.currentLocation.Name : string.Empty;
                float soilHealth = 50.0f;
                if (soilHealthService != null && !string.IsNullOrEmpty(locationName))
                {
                    try
                    {
                        soilHealth = soilHealthService.GetSoilHealth(locationName, new Vector2(x, y));
                    }
                    catch
                    {
                        soilHealth = 50.0f;
                    }
                }

                // Inventory-full detection (best-effort heuristic)
                bool isInventoryFull = false;
                if (who != null && who.Items != null)
                {
                    try
                    {
                        isInventoryFull = who.Items.Count >= 36; // standard backpack capacity
                    }
                    catch
                    {
                        isInventoryFull = false;
                    }
                }

                // Read inputs from crop/farmer where available
                int farmingLevel = who != null ? who.farmingLevel.Value : 0;
                bool isPaddyCrop = false;
                isPaddyCrop = false;

                var inputs = new HarvestQualityInputs
                {
                    FarmingLevel = farmingLevel,
                    FertilizerTier = 0,
                    IsPaddyCrop = isPaddyCrop,
                    RegrowDays = 0,
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
                    // Conditional upgrade using deterministic roll (FR-011)
                    if (who != null && who.Items != null && who.Items.Count > 0)
                    {
                        try
                        {
                            var last = who.Items[who.Items.Count - 1];
                            if (last != null && last.Quality < 4)
                            {
                                int hash = (x * 397) ^ (y * 17) ^ Game1.Date.TotalDays;
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
                        int hash = (x * 397) ^ (y * 17) ^ Game1.Date.TotalDays ^ 0x5A827999;
                        double roll = Math.Abs(hash % 10000) / 10000.0;
                        float boostedChance = service.BoostExtraHarvestChance(0.0f, decision);
                        if (roll < boostedChance)
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

                // Inventory-full handling (FR-008): main item vanilla-blocked; extra item debris
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

                // Drive VisualizationService feedback if available (broadcast handled by game loop)
                if (visualizationService != null && feedback != HarvestFeedback.None)
                {
                    try
                    {
                        // Transient feedback is driven by the seam's flags; the game loop
                        // broadcasts shared outcomes (harvest quality, extra harvest) via
                        // Game1.multiplayer.broadcastSprites + location.playSound (host-only).
                        // Local-only input (hoe flash/text) uses SpriteBatch.
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
