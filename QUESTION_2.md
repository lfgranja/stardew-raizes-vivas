**Question:** Should the performance contingency mechanism (`FR-001` line 74; `Dependencies` 139; `Clarifications` Q7 line 34) — combined chunk phase (~1,000 tiles/chunk, ~2 ms budget, prime-modulus stride 61, spread across ticks, `ExecuteWithConcurrencyGuard` at `OnDayStarted`) — be implemented as the design decision for the `DayStarted` growth scan, accepting that no batch mechanism exists today and the contingency activates only when the linear scan exceeds the `<5 ms` target at 40,000 tiles? (Performance / Deployment / FR-001)

Why it matters: This defines whether the `DayStarted` linear scan (`SoilDecayService.cs` 86; `FR-001`) has a bounded fallback. Without the contingency, host stalls at extreme tile counts (`≤40k`). It also defines whether `ExecuteWithConcurrencyGuard` is added at `OnDayStarted` (matching the `OnSaveLoaded` atomic-flag state-machine `Interlocked.CompareExchange` pattern at `ModController.cs` 2100 / 978 / 1019). The spec explicitly notes: "no batch mechanism exists today — this is a specification-level contingency decision" (`FR-001` 74; `Clarifications` Q7 34).

**Recommended:** Option A — Confirm the defined contingency (Option A from Q7): combined chunk phase, ~1,000 tiles/chunk, ~2 ms budget, prime-modulus stride 61, spread across ticks when linear exceeds `<5 ms`; add `ExecuteWithConcurrencyGuard` at `OnDayStarted`. Matches spec exactly (`FR-001` 74; `Clarifications` Q7 34; `Dependencies` 139). Implementation deferred to plan/tasks; design locked.

| Option | Description |
|--------|-------------|
| A (Recommended) | Confirm the defined contingency: combined chunk phase, ~1,000 tiles/chunk, ~2 ms budget, prime-modulus stride 61, spread across ticks when linear exceeds `<5 ms`; add `ExecuteWithConcurrencyGuard` at `OnDayStarted`. Matches spec exactly (`FR-001` 74; `Clarifications` Q7 34; `Dependencies` 139). Implementation deferred to plan/tasks; design locked. |
| B | Simplify to linear-only: remove chunk contingency from spec; rely solely on `<5 ms` linear contract with no fallback. Reduces complexity but eliminates bounded behavior at 40k tiles — contradicts `FR-001` contingency clause. |
| C | Replace combined chunk with separate decay/growth chunk phases: separate `SoilDecayService` chunks from `Crop.newDay` chunks. Increases complexity (two chunk mechanisms); contradicts Q7's "combined chunk phase" confirmation. |

You can reply with the option letter (e.g., "A"), accept the recommendation by saying "yes" or "recommended", or provide your own short answer (<=5 words).
