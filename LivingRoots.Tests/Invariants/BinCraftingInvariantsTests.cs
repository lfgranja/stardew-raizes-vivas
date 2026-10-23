using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using LivingRoots;
using StardewValley;
using Xunit;

namespace LivingRoots.Tests.Invariants;

/// <summary>
/// Property-based invariant tests verifying ingredient consumption contracts
/// for crafting the Living Roots composting bin.
/// Validates Task T009 [P] [US1] under Feature 004 (Playable Compost Loop).
/// </summary>
public class BinCraftingInvariantsTests
{
    private const string WoodItemId = "388";
    private const string StoneItemId = "390";
    private const string FiberItemId = "771";
    public const string ExpectedRecipeString = "388 50 390 25 771 15/Home/LivingRoots.CompostingBin 1/false/null/Composting Bin";

    /// <summary>
    /// Represents the parsed or defined crafting recipe for the composting bin.
    /// </summary>
    public sealed class BinCraftingRecipe
    {
        public int WoodCost { get; }
        public int StoneCost { get; }
        public int FiberCost { get; }
        public string OutputItemId { get; }
        public int OutputYieldPerCraft { get; }

        public BinCraftingRecipe(
            int woodCost = ModConstants.CompostingBinWoodCost,
            int stoneCost = ModConstants.CompostingBinStoneCost,
            int fiberCost = ModConstants.CompostingBinFiberCost,
            string outputItemId = ModConstants.CompostingBinItemId,
            int outputYieldPerCraft = 1)
        {
            WoodCost = woodCost;
            StoneCost = stoneCost;
            FiberCost = fiberCost;
            OutputItemId = outputItemId;
            OutputYieldPerCraft = outputYieldPerCraft;
        }

        /// <summary>
        /// Parses a Stardew Valley Data/CraftingRecipes entry string.
        /// Format: "ingredientId count ingredientId count ... / tab / outputId count / isBigCraftable / conditions / displayName"
        /// </summary>
        public static BinCraftingRecipe Parse(string recipeString)
        {
            if (string.IsNullOrWhiteSpace(recipeString))
            {
                throw new ArgumentException("Recipe string cannot be null or empty.", nameof(recipeString));
            }

            var parts = recipeString.Split('/');
            if (parts.Length < 3)
            {
                throw new FormatException($"Invalid crafting recipe format: {recipeString}");
            }

            var ingredientTokens = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int wood = 0;
            int stone = 0;
            int fiber = 0;

            for (int i = 0; i < ingredientTokens.Length - 1; i += 2)
            {
                string id = ingredientTokens[i];
                int count = int.Parse(ingredientTokens[i + 1]);

                if (id == WoodItemId || id == "(O)" + WoodItemId)
                {
                    wood = count;
                }
                else if (id == StoneItemId || id == "(O)" + StoneItemId)
                {
                    stone = count;
                }
                else if (id == FiberItemId || id == "(O)" + FiberItemId)
                {
                    fiber = count;
                }
            }

            var outputTokens = parts[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string outputId = outputTokens.Length > 0 ? outputTokens[0] : ModConstants.CompostingBinItemId;
            int yield = outputTokens.Length > 1 ? int.Parse(outputTokens[1]) : 1;

            return new BinCraftingRecipe(wood, stone, fiber, outputId, yield);
        }

        /// <summary>
        /// Checks whether the inventory has sufficient ingredients for the specified craft count.
        /// </summary>
        public bool CanCraft(IList<Item?> inventory, int craftCount)
        {
            if (craftCount <= 0 || inventory == null)
            {
                return false;
            }

            checked
            {
                int requiredWood = WoodCost * craftCount;
                int requiredStone = StoneCost * craftCount;
                int requiredFiber = FiberCost * craftCount;

                int availableWood = GetTotalItemCount(inventory, WoodItemId);
                int availableStone = GetTotalItemCount(inventory, StoneItemId);
                int availableFiber = GetTotalItemCount(inventory, FiberItemId);

                return availableWood >= requiredWood &&
                       availableStone >= requiredStone &&
                       availableFiber >= requiredFiber;
            }
        }

        /// <summary>
        /// Attempts to craft the given count of composting bins.
        /// If ingredients are sufficient, deducts exactly the required ingredients and adds crafted items.
        /// If ingredients are insufficient, refuses crafting and leaves inventory completely unchanged.
        /// </summary>
        public bool TryCraft(IList<Item?> inventory, int craftCount, out List<Item> craftedItems)
        {
            craftedItems = new List<Item>();

            if (!CanCraft(inventory, craftCount))
            {
                return false;
            }

            checked
            {
                int woodToDeduct = WoodCost * craftCount;
                int stoneToDeduct = StoneCost * craftCount;
                int fiberToDeduct = FiberCost * craftCount;

                DeductItems(inventory, WoodItemId, woodToDeduct);
                DeductItems(inventory, StoneItemId, stoneToDeduct);
                DeductItems(inventory, FiberItemId, fiberToDeduct);

                int totalYield = OutputYieldPerCraft * craftCount;
                for (int i = 0; i < totalYield; i++)
                {
                    craftedItems.Add(new StardewValley.Object
                    {
                        ItemId = OutputItemId,
                        Stack = 1
                    });
                }

                return true;
            }
        }

        private static int GetTotalItemCount(IList<Item?> inventory, string itemId)
        {
            int total = 0;
            foreach (var item in inventory)
            {
                if (item != null && (item.ItemId == itemId || item.QualifiedItemId == "(O)" + itemId))
                {
                    total += item.Stack;
                }
            }

            return total;
        }

        private static void DeductItems(IList<Item?> inventory, string itemId, int countToDeduct)
        {
            int remaining = countToDeduct;

            for (int i = 0; i < inventory.Count && remaining > 0; i++)
            {
                var item = inventory[i];
                if (item == null)
                {
                    continue;
                }

                if (item.ItemId == itemId || item.QualifiedItemId == "(O)" + itemId)
                {
                    if (item.Stack <= remaining)
                    {
                        remaining -= item.Stack;
                        inventory[i] = null;
                    }
                    else
                    {
                        item.Stack -= remaining;
                        remaining = 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper to take an immutable snapshot of inventory item stacks and IDs.
    /// </summary>
    private static List<(string? ItemId, int Stack)> CaptureInventorySnapshot(IList<Item?> inventory)
    {
        var snapshot = new List<(string?, int)>(inventory.Count);
        foreach (var item in inventory)
        {
            if (item == null)
            {
                snapshot.Add((null, 0));
            }
            else
            {
                snapshot.Add((item.ItemId, item.Stack));
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Asserts that two inventory snapshots are strictly identical in item IDs, quantities, and order.
    /// </summary>
    private static bool AssertSnapshotsEqual(
        IReadOnlyList<(string? ItemId, int Stack)> expected,
        IReadOnlyList<(string? ItemId, int Stack)> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            if (expected[i].ItemId != actual[i].ItemId || expected[i].Stack != actual[i].Stack)
            {
                return false;
            }
        }

        return true;
    }

    private static int GetTotalCount(IList<Item?> inventory, string itemId)
    {
        int total = 0;
        foreach (var item in inventory)
        {
            if (item != null && (item.ItemId == itemId || item.QualifiedItemId == "(O)" + itemId))
            {
                total += item.Stack;
            }
        }

        return total;
    }

    [Property(MaxTest = 100)]
    public bool ArbitraryPositiveCraftCount_DeductsExactIngredients(
        PositiveInt arbitraryN,
        NonNegativeInt extraWood,
        NonNegativeInt extraStone,
        NonNegativeInt extraFiber)
    {
        int n = (arbitraryN.Get % 100) + 1; // 1 <= n <= 100
        int surplusWood = extraWood.Get % 500;
        int surplusStone = extraStone.Get % 500;
        int surplusFiber = extraFiber.Get % 500;

        int initialWood = (ModConstants.CompostingBinWoodCost * n) + surplusWood;
        int initialStone = (ModConstants.CompostingBinStoneCost * n) + surplusStone;
        int initialFiber = (ModConstants.CompostingBinFiberCost * n) + surplusFiber;

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = initialWood },
            new StardewValley.Object { ItemId = StoneItemId, Stack = initialStone },
            new StardewValley.Object { ItemId = FiberItemId, Stack = initialFiber },
            new StardewValley.Object { ItemId = "382", Stack = 42 } // Coal (non-consumed item)
        };

        var recipe = new BinCraftingRecipe();
        bool success = recipe.TryCraft(inventory, n, out var crafted);

        if (!success)
        {
            return false;
        }

        int expectedWoodDeducted = ModConstants.CompostingBinWoodCost * n;
        int expectedStoneDeducted = ModConstants.CompostingBinStoneCost * n;
        int expectedFiberDeducted = ModConstants.CompostingBinFiberCost * n;

        int finalWood = GetTotalCount(inventory, WoodItemId);
        int finalStone = GetTotalCount(inventory, StoneItemId);
        int finalFiber = GetTotalCount(inventory, FiberItemId);
        int finalCoal = GetTotalCount(inventory, "382");

        bool woodExact = (finalWood == initialWood - expectedWoodDeducted);
        bool stoneExact = (finalStone == initialStone - expectedStoneDeducted);
        bool fiberExact = (finalFiber == initialFiber - expectedFiberDeducted);
        bool coalIntact = (finalCoal == 42);
        bool outputCountMatches = (crafted.Count == n);
        bool allOutputsAreBins = crafted.All(c => c.ItemId == ModConstants.CompostingBinItemId);

        return woodExact && stoneExact && fiberExact && coalIntact && outputCountMatches && allOutputsAreBins;
    }

    [Property(MaxTest = 100)]
    public bool InsufficientWood_RefusesCraftingAndLeavesInventoryUnchanged(
        PositiveInt arbitraryN,
        PositiveInt deficitGen)
    {
        int n = (arbitraryN.Get % 100) + 1;
        int requiredWood = ModConstants.CompostingBinWoodCost * n;
        int deficit = (deficitGen.Get % requiredWood) + 1; // 1 <= deficit <= requiredWood
        int insufficientWood = requiredWood - deficit;

        int initialStone = ModConstants.CompostingBinStoneCost * n + 50;
        int initialFiber = ModConstants.CompostingBinFiberCost * n + 50;

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = insufficientWood },
            new StardewValley.Object { ItemId = StoneItemId, Stack = initialStone },
            new StardewValley.Object { ItemId = FiberItemId, Stack = initialFiber },
            new StardewValley.Object { ItemId = "Sap", Stack = 99 }
        };

        var snapshotBefore = CaptureInventorySnapshot(inventory);
        var recipe = new BinCraftingRecipe();

        bool success = recipe.TryCraft(inventory, n, out var crafted);
        var snapshotAfter = CaptureInventorySnapshot(inventory);

        return !success &&
               crafted.Count == 0 &&
               AssertSnapshotsEqual(snapshotBefore, snapshotAfter);
    }

    [Property(MaxTest = 100)]
    public bool InsufficientStone_RefusesCraftingAndLeavesInventoryUnchanged(
        PositiveInt arbitraryN,
        PositiveInt deficitGen)
    {
        int n = (arbitraryN.Get % 100) + 1;
        int requiredStone = ModConstants.CompostingBinStoneCost * n;
        int deficit = (deficitGen.Get % requiredStone) + 1;
        int insufficientStone = requiredStone - deficit;

        int initialWood = ModConstants.CompostingBinWoodCost * n + 50;
        int initialFiber = ModConstants.CompostingBinFiberCost * n + 50;

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = initialWood },
            new StardewValley.Object { ItemId = StoneItemId, Stack = insufficientStone },
            new StardewValley.Object { ItemId = FiberItemId, Stack = initialFiber },
            new StardewValley.Object { ItemId = "Sap", Stack = 99 }
        };

        var snapshotBefore = CaptureInventorySnapshot(inventory);
        var recipe = new BinCraftingRecipe();

        bool success = recipe.TryCraft(inventory, n, out var crafted);
        var snapshotAfter = CaptureInventorySnapshot(inventory);

        return !success &&
               crafted.Count == 0 &&
               AssertSnapshotsEqual(snapshotBefore, snapshotAfter);
    }

    [Property(MaxTest = 100)]
    public bool InsufficientFiber_RefusesCraftingAndLeavesInventoryUnchanged(
        PositiveInt arbitraryN,
        PositiveInt deficitGen)
    {
        int n = (arbitraryN.Get % 100) + 1;
        int requiredFiber = ModConstants.CompostingBinFiberCost * n;
        int deficit = (deficitGen.Get % requiredFiber) + 1;
        int insufficientFiber = requiredFiber - deficit;

        int initialWood = ModConstants.CompostingBinWoodCost * n + 50;
        int initialStone = ModConstants.CompostingBinStoneCost * n + 50;

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = initialWood },
            new StardewValley.Object { ItemId = StoneItemId, Stack = initialStone },
            new StardewValley.Object { ItemId = FiberItemId, Stack = insufficientFiber },
            new StardewValley.Object { ItemId = "Sap", Stack = 99 }
        };

        var snapshotBefore = CaptureInventorySnapshot(inventory);
        var recipe = new BinCraftingRecipe();

        bool success = recipe.TryCraft(inventory, n, out var crafted);
        var snapshotAfter = CaptureInventorySnapshot(inventory);

        return !success &&
               crafted.Count == 0 &&
               AssertSnapshotsEqual(snapshotBefore, snapshotAfter);
    }

    [Property(MaxTest = 100)]
    public bool NonConsumedItemsRemainIntact_AcrossArbitraryInventories(
        PositiveInt arbitraryN,
        NonNegativeInt extraWood,
        NonNegativeInt extraStone,
        NonNegativeInt extraFiber,
        NonNegativeInt otherStack1,
        NonNegativeInt otherStack2)
    {
        int n = (arbitraryN.Get % 50) + 1;
        int stack1 = (otherStack1.Get % 999) + 1;
        int stack2 = (otherStack2.Get % 999) + 1;

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = "GoldOre", Stack = stack1 },
            new StardewValley.Object { ItemId = WoodItemId, Stack = ModConstants.CompostingBinWoodCost * n + (extraWood.Get % 100) },
            new StardewValley.Object { ItemId = "IronBar", Stack = stack2 },
            new StardewValley.Object { ItemId = StoneItemId, Stack = ModConstants.CompostingBinStoneCost * n + (extraStone.Get % 100) },
            new StardewValley.Object { ItemId = FiberItemId, Stack = ModConstants.CompostingBinFiberCost * n + (extraFiber.Get % 100) },
            new StardewValley.Object { ItemId = "Diamond", Stack = 5 }
        };

        var recipe = new BinCraftingRecipe();
        bool success = recipe.TryCraft(inventory, n, out _);

        if (!success)
        {
            return false;
        }

        bool goldIntact = (GetTotalCount(inventory, "GoldOre") == stack1);
        bool ironIntact = (GetTotalCount(inventory, "IronBar") == stack2);
        bool diamondIntact = (GetTotalCount(inventory, "Diamond") == 5);

        return goldIntact && ironIntact && diamondIntact;
    }

    [Property(MaxTest = 100)]
    public bool MultiStackIngredients_DeductedCorrectlyAcrossSlots(
        PositiveInt arbitraryN)
    {
        int n = (arbitraryN.Get % 20) + 1;
        int requiredWood = ModConstants.CompostingBinWoodCost * n;
        int requiredStone = ModConstants.CompostingBinStoneCost * n;
        int requiredFiber = ModConstants.CompostingBinFiberCost * n;

        // Split wood across 3 slots: part1, part2, surplus
        int woodPart1 = requiredWood / 3;
        int woodPart2 = requiredWood / 3;
        int woodPart3 = requiredWood - (woodPart1 + woodPart2) + 25; // includes 25 surplus

        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = woodPart1 },
            new StardewValley.Object { ItemId = "Coal", Stack = 10 },
            new StardewValley.Object { ItemId = WoodItemId, Stack = woodPart2 },
            new StardewValley.Object { ItemId = StoneItemId, Stack = requiredStone + 10 },
            new StardewValley.Object { ItemId = WoodItemId, Stack = woodPart3 },
            new StardewValley.Object { ItemId = FiberItemId, Stack = requiredFiber + 5 }
        };

        int initialTotalWood = woodPart1 + woodPart2 + woodPart3;

        var recipe = new BinCraftingRecipe();
        bool success = recipe.TryCraft(inventory, n, out var crafted);

        if (!success)
        {
            return false;
        }

        int finalTotalWood = GetTotalCount(inventory, WoodItemId);
        int finalTotalStone = GetTotalCount(inventory, StoneItemId);
        int finalTotalFiber = GetTotalCount(inventory, FiberItemId);
        int finalCoal = GetTotalCount(inventory, "Coal");

        return (finalTotalWood == initialTotalWood - requiredWood) &&
               (finalTotalStone == 10) &&
               (finalTotalFiber == 5) &&
               (finalCoal == 10) &&
               (crafted.Count == n);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void NonPositiveCraftCount_RefusesCraftingAndLeavesInventoryUnchanged(int count)
    {
        var inventory = new List<Item?>
        {
            new StardewValley.Object { ItemId = WoodItemId, Stack = 500 },
            new StardewValley.Object { ItemId = StoneItemId, Stack = 250 },
            new StardewValley.Object { ItemId = FiberItemId, Stack = 150 }
        };

        var snapshotBefore = CaptureInventorySnapshot(inventory);
        var recipe = new BinCraftingRecipe();

        bool success = recipe.TryCraft(inventory, count, out var crafted);
        var snapshotAfter = CaptureInventorySnapshot(inventory);

        Assert.False(success);
        Assert.Empty(crafted);
        Assert.True(AssertSnapshotsEqual(snapshotBefore, snapshotAfter));
    }

    [Fact]
    public void ParsedRecipeString_DeductsIngredientsConsistently()
    {
        var recipe = BinCraftingRecipe.Parse(ExpectedRecipeString);

        Assert.Equal(ModConstants.CompostingBinWoodCost, recipe.WoodCost);
        Assert.Equal(ModConstants.CompostingBinStoneCost, recipe.StoneCost);
        Assert.Equal(ModConstants.CompostingBinFiberCost, recipe.FiberCost);
        Assert.Equal(ModConstants.CompostingBinItemId, recipe.OutputItemId);
        Assert.Equal(1, recipe.OutputYieldPerCraft);
    }
}
