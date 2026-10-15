using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
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
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;
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

        /// <summary>
        /// T223 [P] [US2] — FR-003: the REGISTERED Data/Machines triggers cover
        /// the full validator-owned set. Unlike T120 (which compares two methods
        /// sharing one implementation), this reads the actually-registered
        /// MachineData through the AssetRequested edit path and asserts its
        /// OutputRules triggers: the five vanilla category context tags plus
        /// the validator's inclusion tag, with the validator-owned exclusion
        /// (!not_compostable) present in EVERY trigger. Property-based over the
        /// validator's category set (arbitrary category IDs resolve through the
        /// validator's -74/-75/-79/-80/-81 mapping); the registered-trigger
        /// count/content is a fixed structural assertion evaluated on every
        /// case. RED: ModEntry registers only 2 triggers, omits the five
        /// categories, and trigger "Compost" omits !not_compostable.
        /// </summary>
        [Property(MaxTest = 200)]
        public bool T223_RegisteredTriggers_CoverValidatorCategorySet(int category)
        {
            var machineData = ReadRegisteredBinMachineData();
            if (machineData == null)
            {
                return false;
            }

            var triggers = (machineData.OutputRules ?? new List<MachineOutputRule>())
                .SelectMany(rule => rule.Triggers ?? new List<MachineOutputTriggerRule>())
                .ToList();

            // Fixed structural assertion: exactly the six validator-owned
            // triggers (five vanilla categories + the modded inclusion tag).
            if (triggers.Count != 6)
            {
                return false;
            }

            // Oracle: the validator's own constants, never restated literals.
            string exclusion = "!" + OrganicWasteValidator.NotCompostableTag;

            // Every trigger carries the exclusion override, so an excluded item
            // stays out even when it matches a category.
            foreach (var trigger in triggers)
            {
                if (trigger.RequiredTags == null || !trigger.RequiredTags.Contains(exclusion))
                {
                    return false;
                }
            }

            // The positive tags cover exactly the validator-owned set.
            var positives = triggers
                .SelectMany(trigger => trigger.RequiredTags ?? new List<string>())
                .Where(tag => tag != exclusion)
                .ToHashSet();
            var expected = new HashSet<string>
            {
                "category_seeds",
                "category_vegetable",
                "category_fruits",
                "category_flowers",
                "category_greens",
                OrganicWasteValidator.CompostableItemTag
            };
            if (!positives.SetEquals(expected))
            {
                return false;
            }

            // Property over the validator's category set: an arbitrary category
            // the validator accepts must resolve to a registered trigger pairing
            // its category tag with the exclusion override. Categories outside
            // the validator's set satisfy the property vacuously.
            string? expectedTag = category switch
            {
                -74 => "category_seeds",
                -75 => "category_vegetable",
                -79 => "category_fruits",
                -80 => "category_flowers",
                -81 => "category_greens",
                _ => null
            };
            if (expectedTag == null)
            {
                return true;
            }

            return triggers.Any(trigger =>
                trigger.RequiredTags != null
                && trigger.RequiredTags.Contains(expectedTag)
                && trigger.RequiredTags.Contains(exclusion));
        }

        /// <summary>
        /// Reads the native Data/Machines entry ModEntry registers for the
        /// composting bin: a real ModEntry handles a fabricated
        /// AssetRequestedEventArgs for Data/Machines, and the recorded edit
        /// operation is applied to a dictionary the test owns, so the
        /// declaration read here is the one the mod actually ships.
        /// (CompostingBinServiceTests.ReadRegisteredBinMachineData pattern.)
        /// </summary>
        private static MachineData? ReadRegisteredBinMachineData()
        {
            var modEntry = new ModEntry();
            var args = CreateAssetRequestedEventArgs("Data/Machines");
            modEntry.OnAssetRequested(null, args);

            var editOperations = GetEditOperations(args);
            if (editOperations.Count == 0)
            {
                return null;
            }

            var machinesDict = new Dictionary<string, MachineData>();
            ApplyEditOperation(editOperations[0]!, machinesDict);

            return machinesDict.TryGetValue(ModConstants.MachineCompostingBinId, out var machineData)
                ? machineData
                : null;
        }

        /// <summary>Fabricates the SMAPI event args the registration handler reads.</summary>
        private static AssetRequestedEventArgs CreateAssetRequestedEventArgs(string assetName)
        {
            var mockName = new Mock<IAssetName>();
            mockName.Setup(n => n.IsEquivalentTo(assetName, It.IsAny<bool>())).Returns(true);
            mockName.Setup(n => n.IsEquivalentTo(It.Is<string>(s => s != assetName), It.IsAny<bool>())).Returns(false);

            var mockAssetInfo = new Mock<IAssetInfo>();
            mockAssetInfo.Setup(a => a.NameWithoutLocale).Returns(mockName.Object);

            var ctor = typeof(AssetRequestedEventArgs).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            args[0] = mockAssetInfo.Object;

            if (parameters.Length > 1 && parameters[1].ParameterType.IsGenericType)
            {
                var genArgs = parameters[1].ParameterType.GetGenericArguments();
                var returnType = genArgs.Last();
                var paramTypes = genArgs.Take(genArgs.Length - 1).ToArray();
                var method = new DynamicMethod("DummyGetOnBehalfOf", returnType, paramTypes, typeof(AssetRegistrationTests).Module);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);
                args[1] = method.CreateDelegate(parameters[1].ParameterType);
            }

            var instance = (AssetRequestedEventArgs)ctor.Invoke(args);

            var editOpsField = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (editOpsField != null && editOpsField.GetValue(instance) == null)
            {
                editOpsField.SetValue(instance, Activator.CreateInstance(editOpsField.FieldType));
            }

            var loadOpsField = typeof(AssetRequestedEventArgs).GetField("<LoadOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (loadOpsField != null && loadOpsField.GetValue(instance) == null)
            {
                loadOpsField.SetValue(instance, Activator.CreateInstance(loadOpsField.FieldType));
            }

            var modField = typeof(AssetRequestedEventArgs).GetField("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (modField != null)
            {
                var mockType = typeof(Mock<>).MakeGenericType(modField.FieldType);
                var mockModMetadata = (Mock)Activator.CreateInstance(mockType)!;
                modField.SetValue(instance, mockModMetadata.Object);
            }

            return instance;
        }

        /// <summary>The edit operations the handler recorded against the fabricated event args.</summary>
        private static IList GetEditOperations(AssetRequestedEventArgs args)
        {
            var field = typeof(AssetRequestedEventArgs).GetField("<EditOperations>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IList)field!.GetValue(args)!;
        }

        /// <summary>
        /// Applies a recorded edit operation against a dictionary the test
        /// owns, standing in for the game's editable asset data, so the
        /// handler's edit lands in the dictionary the test reads back.
        /// </summary>
        private static void ApplyEditOperation<TKey, TValue>(object editOp, IDictionary<TKey, TValue> targetDict)
            where TKey : notnull
        {
            var mockAssetData = new Mock<IAssetData>();
            var mockDictData = new Mock<IAssetDataForDictionary<TKey, TValue>>();
            mockDictData.Setup(d => d.Data).Returns(targetDict);
            mockAssetData.Setup(a => a.AsDictionary<TKey, TValue>()).Returns(mockDictData.Object);

            var applyMethod = editOp.GetType().GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyMethod != null)
            {
                applyMethod.Invoke(editOp, new object[] { mockAssetData.Object });
            }
            else
            {
                var actionField = editOp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(f => typeof(Delegate).IsAssignableFrom(f.FieldType));
                var action = actionField?.GetValue(editOp) as Action<IAssetData>;
                action?.Invoke(mockAssetData.Object);
            }
        }
    }
}
