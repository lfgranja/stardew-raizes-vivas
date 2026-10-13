using StardewValley;

namespace LivingRoots.Services
{
    /// <summary>
    /// T194 (FR-005c): the composting bin as it exists while carrying a batch.
    /// A bin holding organic material or ready compost MUST be unstackable
    /// (maximum stack size 1), because two stacked progress-carrying bins would
    /// merge their batches — the data-loss and processing-shortcut hazard
    /// FR-005c exists to prevent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enforcement lives here because this is the only place the rule can live:
    /// <c>Item.canStackWith</c> consults <c>maximumStackSize()</c>, and for a
    /// modded item with no registered definition that method resolves to the
    /// 999 default — so two progress-carrying bins would stack even though both
    /// carry the big-craftable flag. <c>BigCraftableData</c> exposes no
    /// stack-size member to configure, so the definition route cannot carry the
    /// rule; the virtual seam can.
    /// </para>
    /// <para>
    /// Scoped by construction: only a bin the mod reconstructs with progress
    /// (see <c>CompostingBinService.CreateDislodgedBinItem</c>) is ever created
    /// as this type, so a progress-free bin keeps vanilla stack behaviour and
    /// the base item is unchanged for anything else.
    /// </para>
    /// </remarks>
    public sealed class ProgressCarryingBinItem : StardewValley.Object
    {
        /// <summary>
        /// T194 (FR-005c): one batch per bin — the stack can never grow past one.
        /// </summary>
        /// <returns>Always 1.</returns>
        public override int maximumStackSize()
        {
            return 1;
        }
    }
}
