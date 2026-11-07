using LivingRoots.Verification.Queue;
using Xunit;

namespace LivingRoots.Tests.Verification
{
    public class QueueCommandParserTests
    {
        [Theory]
        [InlineData("lr_set_soil_health -10 5 10", "lr_set_soil_health")]
        [InlineData("lr_till 60 15", "lr_till")]
        [InlineData("lr_till 60 15 Farm", "lr_till")]
        [InlineData("lr_place_bin -10 5", "lr_place_bin")]
        [InlineData("lr_insert_waste -10 5 24 5", "lr_insert_waste")]
        [InlineData("lr_plant_crop -10 5 LivingRoots.Verification.LongPhaseCrop", "lr_plant_crop")]
        [InlineData("lr_give_mulch 1", "lr_give_mulch")]
        [InlineData("lr_apply_mulch 60 15", "lr_apply_mulch")]
        [InlineData("lr_apply_mulch 60 15 Farm", "lr_apply_mulch")]
        [InlineData("lr_acquire_host_lock 60 15 999999", "lr_acquire_host_lock")]
        [InlineData("lr_acquire_host_lock 60 15 999999 Farm", "lr_acquire_host_lock")]
        [InlineData("lr_release_host_lock 60 15 999999", "lr_release_host_lock")]
        [InlineData("lr_split_screen", "lr_split_screen")]
        [InlineData("lr_harvest_matrix -10 5", "lr_harvest_matrix")]
        [InlineData("lr_harvest_matrix -10 5 unknown", "lr_harvest_matrix")]
        [InlineData("lr_harvest_matrix -10 5 zero", "lr_harvest_matrix")]
        [InlineData("lr_advance_days 5", "lr_advance_days")]
        [InlineData("lr_dump_crop_phase before", "lr_dump_crop_phase")]
        [InlineData("lr_dump_soil_health after", "lr_dump_soil_health")]
        [InlineData("lr_quit", "lr_quit")]
        public void TryParse_AcceptsTheQueueContract(string line, string expectedName)
        {
            Assert.True(QueueCommand.TryParse(line, out var command, out var error), error);
            Assert.NotNull(command);
            Assert.Equal(expectedName, command!.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("# comment")]
        [InlineData("debug runmacro foo")]
        [InlineData("lr_set_soil_health 1 2")]
        [InlineData("lr_set_soil_health a 2 10")]
        [InlineData("lr_place_bin 1")]
        [InlineData("lr_harvest_matrix 1")]
        [InlineData("lr_harvest_matrix a 2")]
        [InlineData("lr_harvest_matrix 1 2 3 4")]
        [InlineData("lr_harvest_matrix 1 2 bad/probe")]
        [InlineData("lr_insert_waste 1 2 not an item")]
        [InlineData("lr_give_mulch 0")]
        [InlineData("lr_give_mulch")]
        [InlineData("lr_give_mulch nope")]
        [InlineData("lr_till 1")]
        [InlineData("lr_till a 2")]
        [InlineData("lr_till 1 2 bad/location")]
        [InlineData("lr_apply_mulch 1")]
        [InlineData("lr_apply_mulch a 2")]
        [InlineData("lr_apply_mulch 1 2 3 4")]
        [InlineData("lr_apply_mulch 1 2 bad/location")]
        [InlineData("lr_acquire_host_lock 60 15")]
        [InlineData("lr_acquire_host_lock a 15 1")]
        [InlineData("lr_release_host_lock 60 15 1 bad/location")]
        [InlineData("lr_split_screen now")]
        [InlineData("lr_advance_days -1")]
        [InlineData("lr_dump_crop_phase ../../etc/passwd")]
        [InlineData("lr_dump_soil_health a/b")]
        [InlineData("lr_quit now")]
        public void TryParse_RejectsUnknownBadArityAndPathTraversal(string line)
        {
            Assert.False(QueueCommand.TryParse(line, out var command, out _));
            Assert.Null(command);
        }

        [Fact]
        public void TryParse_RejectsOversizedLabels()
        {
            var line = "lr_dump_crop_phase " + new string('a', 65);
            Assert.False(QueueCommand.TryParse(line, out _, out _));
        }

        [Fact]
        public void TryParse_AllowsOptionalLocation()
        {
            Assert.True(QueueCommand.TryParse("lr_set_soil_health -10 5 10 Greenhouse", out var command, out _));
            Assert.Equal("Greenhouse", command!.GetString(3));
        }
    }
}
