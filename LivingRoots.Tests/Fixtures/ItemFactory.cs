using StardewValley;

namespace LivingRoots.Tests.Fixtures
{
    public static class ItemFactory
    {
        public static Item CreateValidWasteItem()
        {
            var item = new StardewValley.Object("Seeds", 1);
            item.Category = -74;
            return item;
        }

        public static Item CreateInvalidWasteItem()
        {
            var item = new StardewValley.Object("Stone", 1);
            item.Category = -12;
            return item;
        }

        public static Item CreateItem(string qualifiedItemId, int category)
        {
            var item = new StardewValley.Object(qualifiedItemId, 1);
            item.Category = category;
            return item;
        }
    }
}
