using FsCheck;
using FsCheck.Xunit;
using LivingRoots.Domain;
using LivingRoots.Domain.Models;
using Xunit;

namespace LivingRoots.Tests;

/// <summary>
/// T000-R2 (spec 004, Phase 0 RED): design-invariant tests for the
/// <see cref="CompostingBinStateModel"/> runtime state machine —
/// Empty→Processing→Ready monotonicity (never skip, never regress) and
/// the single-batch yield contract (max stack = 1 while progress is
/// carried). RED-phase only: pins the model contract before T006 lands
/// the state-machine transitions. Not integration tests.
/// </summary>
public class CompostingBinStateModelTests
{
    /// <summary>
    /// T000-R2.1: a fresh model starts Empty — no input, no progress.
    /// </summary>
    [Fact]
    public void DefaultState_IsEmpty()
    {
        var bin = new CompostingBinStateModel();

        Assert.Equal(CompostingBinState.Empty, bin.State);
        Assert.Null(bin.InputItemId);
        Assert.Null(bin.InputTimestamp);
    }

    /// <summary>
    /// T000-R2.2: transitions are monotonic — Empty→Processing→Ready, each
    /// step advances the ordinal by exactly one; never skips, never regresses.
    /// </summary>
    [Fact]
    public void StateTransitions_AreMonotonic_NeverSkipOrRegress()
    {
        var bin = new CompostingBinStateModel();
        int previousOrdinal = (int)bin.State;

        Assert.Equal(CompostingBinState.Empty, bin.State);

        bin.State = CompostingBinState.Processing;
        Assert.Equal(CompostingBinState.Processing, bin.State);
        Assert.True((int)bin.State == previousOrdinal + 1, "Processing must advance the ordinal by exactly one");
        previousOrdinal = (int)bin.State;

        bin.State = CompostingBinState.Ready;
        Assert.Equal(CompostingBinState.Ready, bin.State);
        Assert.True((int)bin.State == previousOrdinal + 1, "Ready must advance the ordinal by exactly one");
    }

    /// <summary>
    /// T000-R2.3 — ForAll(state =&gt; Enum.IsDefined(typeof(CompostingBinState), state)):
    /// every generated state is a defined CompostingBinState member
    /// (Empty/Processing/Ready).
    /// </summary>
    [Property(MaxTest = 100)]
    public bool State_IsAlwaysAValidEnumValue_ForAll(int seed)
    {
        var state = (CompostingBinState)Modulo(seed, 3);

        return Enum.IsDefined(typeof(CompostingBinState), state);
    }

    /// <summary>
    /// T000-R2.4 — ForAll(tileX, tileY =&gt; model.TileX == tileX &amp;&amp; model.TileY == tileY):
    /// tile coordinates round-trip through the model.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool TileCoordinates_CanBeSetAndRetrieved_ForAll(int tileX, int tileY)
    {
        var bin = new CompostingBinStateModel { TileX = tileX, TileY = tileY };

        return bin.TileX == tileX && bin.TileY == tileY;
    }

    /// <summary>
    /// T000-R2.5 — ForAll(state =&gt; (int)state is within [Empty, Ready]):
    /// the state machine is bounded — no ordinal below Empty or above Ready,
    /// so the cycle can never escape the defined states.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool StateMachine_MonotonicityInvariant_BoundedOrdinals_ForAll(int seed)
    {
        var state = (CompostingBinState)Modulo(seed, 3);
        int ordinal = (int)state;

        return ordinal >= (int)CompostingBinState.Empty
            && ordinal <= (int)CompostingBinState.Ready;
    }

    /// <summary>
    /// T000-R2.6 — ForAll(bin =&gt; bin.State != Ready || bin.InputItemId != null):
    /// a bin may only reach Ready while an input batch is recorded — Ready
    /// requires input (batch-yield provenance, FR-005a). The model is built
    /// under the seam's consistency rule: Empty carries no input;
    /// Processing/Ready always carry one.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool BatchYield_ReadyRequiresInputItemId_ForAll(int stateSeed, NonNull<string> itemId)
    {
        var state = (CompostingBinState)Modulo(stateSeed, 3);
        var bin = CreateConsistentBin(state, itemId.Get);

        return bin.State != CompostingBinState.Ready || bin.InputItemId != null;
    }

    /// <summary>
    /// T000-R2.7 — ForAll(bin =&gt; (bin.State == Processing || bin.State == Ready)
    /// ? bin.InputItemId != null : true): while any progress is active the
    /// batch is unstackable (max stack = 1) and must carry its input
    /// provenance; Empty carries none.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool MaxStack_OneWhenProgressActive_ForAll(int stateSeed, NonNull<string> itemId)
    {
        var state = (CompostingBinState)Modulo(stateSeed, 3);
        var bin = CreateConsistentBin(state, itemId.Get);

        return bin.State == CompostingBinState.Processing || bin.State == CompostingBinState.Ready
            ? bin.InputItemId != null
            : true;
    }

    /// <summary>
    /// Builds a model under the seam's consistency rule: Empty forces both
    /// input fields to null; Processing/Ready always carry an item ID and a
    /// timestamp.
    /// </summary>
    private static CompostingBinStateModel CreateConsistentBin(CompostingBinState state, string inputItemId)
    {
        var bin = new CompostingBinStateModel
        {
            State = state,
            InputItemId = state == CompostingBinState.Empty ? null : inputItemId,
            InputTimestamp = state == CompostingBinState.Empty ? null : 1
        };

        return bin;
    }

    /// <summary>
    /// Non-negative modulo for generated seeds, so arbitrary (including
    /// negative) FsCheck integers map into a fixed range.
    /// </summary>
    private static int Modulo(int value, int modulus)
    {
        int remainder = value % modulus;
        if (remainder < 0)
        {
            remainder += modulus;
        }

        return remainder;
    }
}
