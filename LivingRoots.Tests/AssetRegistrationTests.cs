using LivingRoots; // access ModConstants / asset keys
using LivingRoots.Domain.Services; // OrganicWasteValidator-owned set reference
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T120 [P] [US2] — specs/004-playable-compost-loop
    /// RED parity test: Data/Machines RequiredTags must accept exactly the
    /// OrganicWasteValidator-owned waste set with zero restatement drift.
    /// Per FR-003 (partial): qualifying set is inherited, not restated.
    /// Must fail red until production registration lands.
    /// </summary>
    public class AssetRegistrationTests
    {
        /// <summary>T120 [P] [US2] RED — FR-003 (partial): RequiredTags derived from validator set.</summary>
        [Fact]
        public void T120_RequiredTags_ExactlyOrganicWasteValidatorSet()
        {
            // RED: Data/Machines RequiredTags diverge from the single validator-owned waste set.
            // Per specs/004-playable-compost-loop FR-003 (partial): the qualifying set stays
            // owned by the soil decay and compost specification; this feature MUST NOT restate,
            // extend, narrow, or contradict that set.
            Assert.True(false,
                "RED T120: RequiredTags do not match OrganicWasteValidator-owned set (FR-003 partial, specs/004-playable-compost-loop). " +
                "No restatement drift permitted — parity not yet attested.");
        }
    }
}
