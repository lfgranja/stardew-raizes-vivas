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
        private static IMonitor monitor;
        private static IHarvestQualityService service;

        public static void Initialize(IMonitor modMonitor, IHarvestQualityService harvestService)
        {
            monitor = modMonitor;
            service = harvestService ?? new HarvestQualityService();
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
                    monitor.Log(string.Format("Living Roots harvest prefix patch failed; falling back to original. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
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
                float soilHealth = 50.0f;
                var inputs = new HarvestQualityInputs
                {
                    FarmingLevel = Game1.player != null ? Game1.player.farmingLevel.Value : 0,
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
                if (!decision.IsInScope || decision.Tier == HarvestTier.Moderate || decision.Tier == HarvestTier.Unknown)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                if (monitor != null)
                {
                    monitor.Log(string.Format("Living Roots harvest postfix patch failed; skipping modification. Type: {0} (HResult: 0x{1:X8})", ex.GetType().FullName, ex.HResult), LogLevel.Trace);
                }
            }
        }
    }
}
