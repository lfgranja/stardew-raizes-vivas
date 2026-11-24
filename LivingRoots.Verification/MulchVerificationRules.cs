using System;

namespace LivingRoots.Verification
{
    /// <summary>
    /// Pure decisions behind the dev/CI <c>lr_give_mulch</c> command (018 T006/T007).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>GrantMulch</c> used to combine two decisions — host/count gating and
    /// empty-slot selection — with reads of <c>Game1.player</c>. That coupling
    /// made the decisions unreachable from a unit test: constructing the command
    /// handler requires a booted game. The decisions themselves are pure, so they
    /// live here and the handler keeps only the game-state reads.
    /// </para>
    /// <para>
    /// These rules are internal to the verification assembly and visible to the
    /// test assembly through <c>InternalsVisibleTo</c>. They are not part of the
    /// production mod's contract.
    /// </para>
    /// </remarks>
    internal static class MulchVerificationRules
    {
        /// <summary>
        /// Determines whether a grant command may proceed at all: the requested
        /// count must be positive and the command must run on the main player's
        /// machine. A farmhand must never mint items into another player's
        /// inventory.
        /// </summary>
        /// <param name="count">Requested item count.</param>
        /// <param name="isMainPlayer">Whether this process owns the main player.</param>
        /// <returns><c>true</c> only for a positive count on the main player.</returns>
        public static bool CanGrant(int count, bool isMainPlayer)
        {
            return count > 0 && isMainPlayer;
        }

        /// <summary>
        /// Finds the first empty inventory slot within the player's effective
        /// capacity, which is the smaller of the slot count and the player's
        /// <c>MaxItems</c>. Occupancy is supplied by the caller so this rule
        /// stays independent of the game's inventory type.
        /// </summary>
        /// <param name="slotCount">Number of slots in the backing inventory.</param>
        /// <param name="maxItems">Player capacity, which may exceed the slot count.</param>
        /// <param name="isOccupied">Predicate reporting whether a slot is occupied.</param>
        /// <returns>The zero-based slot index, or <c>-1</c> when no slot is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="isOccupied"/> is null.</exception>
        public static int FindEmptySlot(int slotCount, int maxItems, Func<int, bool> isOccupied)
        {
            if (isOccupied == null)
            {
                throw new ArgumentNullException(nameof(isOccupied));
            }

            var limit = Math.Max(0, Math.Min(slotCount, maxItems));
            for (var slot = 0; slot < limit; slot++)
            {
                if (!isOccupied(slot))
                {
                    return slot;
                }
            }

            return -1;
        }
    }
}
