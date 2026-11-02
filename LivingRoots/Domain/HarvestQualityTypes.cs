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
    public enum HarvestFeedbackFlags
    {
        None = 0,
        Poor = 1 << 0,
        LivingQuality = 1 << 1,
        ExtraHarvest = 1 << 2
    }

    public sealed class HarvestQualityInputs
    {
        public int FarmingLevel { get; set; }
        public int FertilizerTier { get; set; }
        public bool IsPaddyCrop { get; set; }
        public int RegrowDays { get; set; }
        public int HarvestMaxIncreasePerFarmingLevel { get; set; }
        public bool IsForageCrop { get; set; }
        public bool IsFibreSeed { get; set; }
        public bool IsQiFruit { get; set; }
        public int? PestQualityPenalty { get; set; }
        public bool IsInventoryFull { get; set; }
    }

    public sealed class HarvestQualityDecision
    {
        public HarvestTier Tier { get; set; }
        public bool IsInScope { get; set; }
        public bool ForceQualityZero { get; set; }
        public float QualityBoost { get; set; }
        public float ExtraHarvestBoost { get; set; }
        public int? PestQualityPenalty { get; set; }
        public bool MainItemBlocked { get; set; }
        public bool ExtraItemDroppedAsDebris { get; set; }
    }
}
