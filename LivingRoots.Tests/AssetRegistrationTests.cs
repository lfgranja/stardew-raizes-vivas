using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using LivingRoots.Domain.Services;
using LivingRoots.Tests.Fixtures;
using Microsoft.Xna.Framework;
using Moq;
using StardewModdingAPI;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests
{
    /// <summary>
    /// T120 [P] [US2] — specs/004-playable-compost-loop
    /// Parity test: Data/Machines RequiredTags must accept exactly the
    /// OrganicWasteValidator-owned waste set with zero restatement drift.
    /// Per FR-003 (partial): the qualifying set is inherited, not restated.
    /// Property-based over arbitrary (tag, category) inputs — a drift between
    /// the native machine filter and the validator fails for that input.
    /// </summary>
    public class AssetRegistrationTests
    {
        /// <summary>T120 [P] [US2] — FR-003 (partial): RequiredTags derived from validator set.</summary>
        [Property(MaxTest = 200)]
        public bool T120_RequiredTags_ExactlyOrganicWasteValidatorSet(NonEmptyString tag, int category)
        {
            // Context-tag queries resolve through ItemRegistry, which is only
            // populated once the game has booted; the fixture installs the
            // minimum registry state the query needs.
            Fixtures.GameStateFixture.Install();

            var item = ItemFactory.CreateItem("Object.Sap");
            item.Category = category;
            item.modData["ContextTags"] = tag.Get;

            // Property under test: the machine tag rule and the validator
            // agree for every item, so the Data/Machines triggers (derived from
            // the validator's own constants — OrganicWasteValidator.CompostableItemTag
            // / NotCompostableTag, T133) can never restate or drift.
            return OrganicWasteValidator.MatchesMachineTagRule(item) ==
                   new OrganicWasteValidator(new Moq.Mock<StardewModdingAPI.IMonitor>().Object).IsValidOrganicWaste(item);
        }

        /// <summary>T120 [P] [US2] — FR-003: the machine tags are the validator's constants, not literals.</summary>
        [Fact]
        public void T120_MachineWasteTags_AreValidatorOwnedConstants()
        {
            // Zero restatement drift: the native trigger tags come from the
            // validator's own constant set, so there is nothing to drift from.
            Assert.Contains(OrganicWasteValidator.CompostableItemTag, OrganicWasteValidator.MachineWasteTags);
            Assert.Contains("!" + OrganicWasteValidator.NotCompostableTag, OrganicWasteValidator.MachineWasteTags);
            Assert.Equal(2, OrganicWasteValidator.MachineWasteTags.Count);
        }
    }
}
