using StardewModdingAPI;

namespace LivingRoots.Verification
{
    public sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
        }

        // Dispose stub (T003/T151): scaffold placeholder until T102
        // replaces it with the real lock + _disposed pattern.
#pragma warning disable S1185
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
#pragma warning restore S1185
    }
}
