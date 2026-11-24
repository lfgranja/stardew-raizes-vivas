using System;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Services;
using Xunit;

namespace LivingRoots.Tests.HarvestQuality
{
    public class HarvestFeedbackPlanTests
    {
        private readonly HarvestQualityService _service = new HarvestQualityService();

        [Fact]
        public void PoorTier_HostPlan_ContainsPoorFeedback()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5 };
            var decision = _service.Decide(20.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, false, true);

            Assert.Equal(HarvestFeedbackFlags.Poor, plan.Flags);
            Assert.Single(plan.Messages);
            Assert.Equal("marker-poor", plan.Messages[0].SpriteKey);
            Assert.Equal("Soil too poor for quality crops", plan.Messages[0].Text);
            Assert.Equal("cancel", plan.SoundName);
            Assert.True(plan.DurationMs > 0 && plan.DurationMs <= 1000);
            Assert.True(plan.BroadcastToAllPlayers);
        }

        [Fact]
        public void PoorTier_ClientPlan_IsNotBroadcast()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 5 };
            var decision = _service.Decide(20.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, false, false);

            Assert.Equal(HarvestFeedbackFlags.Poor, plan.Flags);
            Assert.False(plan.BroadcastToAllPlayers);
        }

        [Fact]
        public void HealthyEnhancedOnly_HasLivingQualityFeedback()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, true, false, true);

            Assert.True(plan.Flags.HasFlag(HarvestFeedbackFlags.LivingQuality));
            Assert.False(plan.Flags.HasFlag(HarvestFeedbackFlags.ExtraHarvest));
            Assert.Single(plan.Messages);
            Assert.Equal("sparkle-living", plan.Messages[0].SpriteKey);
            Assert.Equal("Living Soil", plan.Messages[0].Text);
            Assert.NotNull(plan.SoundName);
            Assert.True(plan.DurationMs > 0 && plan.DurationMs <= 1000);
        }

        [Fact]
        public void HealthyExtraOnly_DoesNotSetLivingQuality()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, true, true);

            Assert.True(plan.Flags.HasFlag(HarvestFeedbackFlags.ExtraHarvest));
            Assert.False(plan.Flags.HasFlag(HarvestFeedbackFlags.LivingQuality));
            Assert.Single(plan.Messages);
            Assert.Equal("sparkle-living", plan.Messages[0].SpriteKey);
            Assert.Equal("Extra harvest!", plan.Messages[0].Text);
            Assert.NotNull(plan.SoundName);
            Assert.True(plan.DurationMs > 0 && plan.DurationMs <= 1000);
        }

        [Fact]
        public void HealthyBoth_HasTwoMessages()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, true, true, true);

            Assert.True(plan.Flags.HasFlag(HarvestFeedbackFlags.LivingQuality));
            Assert.True(plan.Flags.HasFlag(HarvestFeedbackFlags.ExtraHarvest));
            Assert.Equal(2, plan.Messages.Count);
            Assert.Contains(plan.Messages, message => message.Text == "Living Soil");
            Assert.Contains(plan.Messages, message => message.Text == "Extra harvest!");
            Assert.True(plan.DurationMs > 0 && plan.DurationMs <= 1000);
        }

        [Fact]
        public void HealthyNeither_IsEmpty()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, false, true);

            Assert.True(plan.IsEmpty);
            Assert.Empty(plan.Messages);
            Assert.Null(plan.SoundName);
        }

        [Theory]
        [InlineData(35.0f)]
        [InlineData(50.0f)]
        [InlineData(70.999f)]
        public void ModerateTier_IsEmpty(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(health, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, false, true);

            Assert.True(plan.IsEmpty);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void UnknownTier_IsEmpty(float health)
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(health, inputs);

            var plan = _service.BuildFeedbackPlan(decision, false, false, true);

            Assert.True(plan.IsEmpty);
        }

        [Fact]
        public void OutOfScopeForageCrop_IsEmpty()
        {
            var inputs = new HarvestQualityInputs { IsForageCrop = true, FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var plan = _service.BuildFeedbackPlan(decision, true, true, true);

            Assert.True(plan.IsEmpty);
        }

        [Property]
        public Property NonEmptyPlan_DurationAndBroadcastAreValid(float health, bool enhanced, bool extra, bool isMainPlayer)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health < 0.0f || health > 100.0f)
            {
                return true.ToProperty();
            }

            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(health, inputs);
            var plan = _service.BuildFeedbackPlan(decision, enhanced, extra, isMainPlayer);

            if (plan.IsEmpty)
            {
                return true.ToProperty();
            }

            return (plan.DurationMs > 0
                    && plan.DurationMs <= 1000
                    && plan.BroadcastToAllPlayers == isMainPlayer).ToProperty();
        }

        [Fact]
        public void BuildFeedbackPlan_IsDeterministic()
        {
            var inputs = new HarvestQualityInputs { FarmingLevel = 10 };
            var decision = _service.Decide(80.0f, inputs);

            var first = _service.BuildFeedbackPlan(decision, true, true, true);
            var second = _service.BuildFeedbackPlan(decision, true, true, true);

            Assert.Equal(first.Flags, second.Flags);
            Assert.Equal(first.SoundName, second.SoundName);
            Assert.Equal(first.DurationMs, second.DurationMs);
            Assert.Equal(first.BroadcastToAllPlayers, second.BroadcastToAllPlayers);
            Assert.Equal(first.Messages.Count, second.Messages.Count);
        }
    }
}
