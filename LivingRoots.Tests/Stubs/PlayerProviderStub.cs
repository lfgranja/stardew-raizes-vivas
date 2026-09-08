using StardewValley;

using LivingRoots.Domain;

namespace LivingRoots.Tests.Stubs
{
    public class PlayerProviderStub : IPlayerProvider
    {
        public Farmer CurrentPlayer { get; set; } = null!;
        public Item? CurrentItem { get; set; }
    }
}
