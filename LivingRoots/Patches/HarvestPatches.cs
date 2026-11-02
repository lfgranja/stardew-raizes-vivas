using System;
using HarmonyLib;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace LivingRoots.Patches
{
    public static class HarvestPatches
    {
        private static IMonitor? _monitor;
        private static IHarvestQualityService? _service;

        public static void Initialize(IMonitor monitor, IHarvestQualityService service)
        {
            _monitor = monitor;
            _service = service ?? new HarvestQualityService();
        }

        public static bool HarvestPrefix(Crop __instance, int xTile, int yTile, HoeDirt soil, object? junimoHarvester, bool isForcedScytheHarvest, ref bool __state)
        {
            __state = true;
            try
            {
                if (__instance == null || soil == null)
                {
                    return true;
                }
                if (_service == null)
                {
                    _service = new HarvestQualityService();
                }
                // Basic crop scope guard
                if (__instance.forageCrop.Value)
                {
                    return true;
                }
                if (__instance.indexOfHarvest.Value == "771") // tea leaves not typical crop scope issues, but keep guard
                {
                    // continue
                }
            }
            catch (Exception ex)
            {
                if (_monitor != null)
                {
                    _monitor.Log($"Living Roots harvest prefix patch failed; falling back to original. Type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                }
                return true;
            }
            return true;
        }

        public static void HarvestPostfix(Crop __instance, int xTile, int yTile, HoeDirt soil, object? junimoHarvester, bool isForcedScytheHarvest, bool __result)
        {
            try
            {
                if (__instance == null || soil == null || !_serviceInitialized())
                {
                    return;
                }
                if (_service == null)
                {
                    _service = new HarvestQualityService();
                }
                // Apply scope guards
                if (__instance.forageCrop.Value)
                {
                    return;
                }
                // Get soil health from game state would require service; use defaults for patch structure
                float soilHealth = 50.0f;
                var inputs = new HarvestQualityInputs
                {
                    FarmingLevel = Game1.player?.farmingLevel.Value ?? 0,
                    FertilizerTier = 0,
                    IsPaddyCrop = false,
                    RegrowDays = __instance.regrowAfterHarvest.Value,
                    HarvestMaxIncreasePerFarmingLevel = 0,
                    IsForageCrop = __instance.forageCrop.Value,
                    IsFibreSeed = false,
                    IsQiFruit = false,
                    PestQualityPenalty = null,
                    IsInventoryFull = false
                };
                var decision = _service.Decide(soilHealth, inputs);
                if (!decision.IsInScope || decision.Tier == HarvestTier.Moderate || decision.Tier == HarvestTier.Unknown)
                {
                    return;
                }
                if (decision.Tier == HarvestTier.Poor && decision.ForceQualityZero)
                {
                    // Try to force quality to 0 on harvested items - best effort
                    try
                    {
                        if (Game1.player != null && Game1.player.currentLocation != null)
                        {
                            // Postfix can't easily access created items; this is structural
                        }
                    }
                    catch { }
                    return;
                }
                if (decision.Tier == HarvestTier.Healthy)
                {
                    // Apply boosts conceptually - structural implementation
                    return;
                }
            }
            catch (Exception ex)
            {
                if (_monitor != null)
                {
                    _monitor.Log($"Living Roots harvest postfix patch failed; skipping modification. Type: {ex.GetType().FullName} (HResult: 0x{ex.HResult:X8})", LogLevel.Trace);
                }
            }
        }

        private static bool _serviceInitialized()
        {
            return _service != null || _monitor != null;
        }
    }
}
