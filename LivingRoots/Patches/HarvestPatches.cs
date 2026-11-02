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

        public static void Initialize(IMonitor modMonitor, IHarvestQualityService harvestService, ISoilHealthService? soilService = null)
        {
            monitor = modMonitor;
            service = harvestService ?? new HarvestQualityService();
            soilHealthService = soilService;
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

                // Resolve the harvested tile and location. Crop.harvest(int x, int y, HoeDirt soil, Farmer who)
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

                var inputs = new HarvestQualityInputs
                {
                    FarmingLevel = who != null ? who.farmingLevel.Value : 0,
                    FertilizerTier = 0,
                    IsPaddyCrop = false,
                    RegrowDays = 0,
                    HarvestMaxIncreasePerFarmingLevel = 0,
                    IsForageCrop = crop.forageCrop.Value,
                    IsFibreSeed = false,
                    IsQiFruit = false,
                    PestQualityPenalty = null,
                    IsInventoryFull = false
                };

                var decision = service.Decide(soilHealth, inputs);
                if (!decision.IsInScope)
                {
                    return;
                }

                if (decision.Tier == HarvestTier.Poor && decision.ForceQualityZero)
                {
                    // Force the harvested item(s) to normal quality (0 stars) for Poor soil.
                    if (who != null)
                    {
                        try
                        {
                            // The last item added to the farmer's backpack during harvest
                            // is the primary harvested product; downgrade its quality.
                            if (who.Items != null && who.Items.Count > 0)
                            {
                                var last = who.Items[who.Items.Count - 1];
                                if (last != null)
                                {
                                    last.Quality = 0;
                                }
                            }
                        }
                        catch
                        {
                            // Best-effort; do not break harvest.
                        }
                    }
                    if (monitor != null)
                    {
                        monitor.Log("Living Roots: Poor soil — harvest quality forced to normal.", LogLevel.Trace);
                    }
                }

                if (decision.Tier == HarvestTier.Healthy)
                {
                    // Conditional upgrade: apply the boosted quality probability as a
                    // chance to bump the harvested item one quality tier. The actual
                    // roll is performed here (in-game only); the seam keeps the math.
                    if (who != null && who.Items != null && who.Items.Count > 0)
                    {
                        try
                        {
                            var last = who.Items[who.Items.Count - 1];
                            if (last != null && last.Quality < 4)
                            {
                                float vanillaProb = 0.5f;
                                float boosted = service.BoostQualityProbability(vanillaProb, decision);
                                if (Random.Shared.NextDouble() < boosted)
                                {
                                    last.Quality = Math.Min(4, last.Quality + 1);
                                }
                            }
                        }
                        catch
                        {
                            // Best-effort.
                        }
                    }
                    if (monitor != null)
                    {
                        monitor.Log("Living Roots: Living soil — quality boost applied.", LogLevel.Trace);
                    }
                }

                // Feedback resolution is driven by the seam; transient sprites/SFX
                // are broadcast by the game loop (out of scope for this adapter).
                var feedback = service.ResolveFeedback(decision, decision.Tier == HarvestTier.Healthy, decision.Tier == HarvestTier.Healthy);
                if (feedback != HarvestFeedback.None && monitor != null)
                {
                    monitor.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Living Roots harvest feedback: {0}", feedback), LogLevel.Trace);
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
