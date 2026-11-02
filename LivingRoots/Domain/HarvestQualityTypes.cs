using System;

namespace LivingRoots.Domain
{
    public enum HarvestTier
    {
        Unknown = 0,
        Poor = 1,
        Moderate = 2,
        Healthy = 3,
        OutOfScope = 4
    }

    [Flags]
    public enum HarvestFeedback
    {
        None = 0,
        Poor = 1 << 0,
        LivingQuality = 1 << 1,
        ExtraHarvest = 1 << 2
    }

    public readonly record struct HarvestQualityInputs
    {
        public int FarmingLevel { get; init; }
        public int FertilizerTier { get; init; }
        public bool IsPaddyCrop { get; init; }
        public int RegrowDays { get; init; }
        public int HarvestMaxIncreasePerFarmingLevel { get; init; }
        public bool IsForageCrop { get; init; }
        public bool IsFibreSeed { get; init; }
        public bool IsQiFruit { get; init; }
        public int? PestQualityPenalty { get; init; }
        public bool IsInventoryFull { get; init; }
    }

    public readonly record struct HarvestQualityDecision
    {
        public HarvestTier Tier { get; init; }
        public bool IsInScope { get; init; }
        public bool ForceQualityZero { get; init; }
        public float QualityBoost { get; init; }
        public float ExtraHarvestBoost { get; init; }
        public int? PestQualityPenalty { get; init; }
        public bool MainItemBlocked { get; init; }
        public bool ExtraItemDroppedAsDebris { get; init; }
    }
}
