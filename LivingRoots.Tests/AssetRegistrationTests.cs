using LivingRoots; // access ModConstants / asset keys
using Xunit;

namespace LivingRoots.Tests
{
    public class AssetRegistrationTests
    {
        /// <summary>T120 [P] [US2] RED — FR-003 (partial): RequiredTags derived from validator set.</summary>
        [Fact]
        public void T120_RequiredTags_ExactlyOrganicWasteValidatorSet()
        {
            // RED: Data/Machines RequiredTags diverge from the single validator-owned waste set.
            Assert.True(false, "RED T120: RequiredTags do not match OrganicWasteValidator set (FR-003 partial).");
        }
    }
}
