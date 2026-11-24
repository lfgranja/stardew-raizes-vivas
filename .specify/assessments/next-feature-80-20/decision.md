# Decision: What to Build Next

- **Slug**: next-feature-80-20
- **Decided**: 2026-10-03
- **Verdict**: **go**
- **Clarification received**: 2026-10-03 — Principle VI requires only that **no new feature is designed
  single-player-only**; multiplayer capability is *not* mandated as the immediate next work item.
  Answered by the maintainer. This resolves blocking question 1 and converts the verdict from
  `needs-clarification` to `go` without re-shaping, per the revisit path recorded below.
- **Artifacts reviewed**: intake.md yes | research.md yes | problem.md yes | concept.md yes

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | **strong** | Verified by execution, not inference: build is clean, 816/816 tests pass, and `ModEntry.cs:114-130` registers no `Data/Objects` entry while `Data/Machines` is empty. The project demonstrably reports success over a mod with no playable surface. |
| Evidence strength | **adequate** | Problem-side evidence is strong (executed build/test, `file:line` throughout). Solution-side is weak: appetite estimates are explicitly ASSUMPTION-grade and untrialed, the Content-Patcher claim rests on a host outside the safe list marked `[UNVERIFIED]`, and no third-party demand evidence was found at all. Adequate overall, not strong. |
| Value vs. inaction | **strong** | Inaction cost is severe and well-evidenced: the suite stays permanently misleading, Principle VI stays violated, and `specs/002` stands as proof that a spec can be marked 38/38 complete while the feature is unreachable. Caveat: direct *player* value is low — Option A serves the maintainer's decision-making, not his spouse's gameplay. |
| Feasibility / appetite | **adequate** | A credible smallest-option exists at small–medium appetite, plus a very cheap small option (C). But every supporting LOC figure is untrialed and the visualization layer has never executed in production, so the estimate is credible rather than reliable. |
| Strategic fit | **adequate** | Option A ships no single-player-only path and enables Principle VI's Gate 3, so it does not violate the constitution — but it builds none of the capability the constitution made non-negotiable. Real tension, defensible either way, not clean. |
| Risk posture | **adequate** | Risks are catalogued unusually well (Content Patcher uncertainty, unverified `Data/Objects` schema, visualization cascade, `WriteJsonFile` vs `WriteSaveData`, absent farmhand test doubles, N-times daily decay). But mitigation is mostly sequencing advice — "validate first", "treat as a spike" — rather than concrete. Risks are identified, not yet retired. |

**Two `unknown`/absent items acknowledged, not glossed:**
- `dotnet format --verify-no-changes` produced **no result either way** (5-minute timeout). Gate 1's
  formatting gate is unverified.
- **No external demand evidence exists.** The entire value case rests on first-party intent. Absence of
  evidence, not evidence of absence.

## Verdict & Rationale

**Initial verdict (recorded 2026-10-03, retained for audit): needs-clarification.** Every gate criterion
for a `go` was technically satisfied — problem validity `strong`, evidence strength `adequate` (not
`weak` or `unknown`), and a shaped, recommended concept option existed. It was downgraded anyway,
because `concept.md` had committed that open question 5 — *does Principle VI require multiplayer
capability as the immediate next work item, or only that nothing new be designed single-player-only?* —
"should be settled before `/speckit.assess.decide` commits." That commitment was binding. The answer
was outcome-determinative: had it come back "multiplayer now", the recommendation would have flipped
from Option A to Option B and everything downstream would have changed. No pipeline stage could resolve
it — it required a statement of intent from the maintainer, not more research.

**Final verdict: go.** The maintainer answered on 2026-10-03: Principle VI requires **only that nothing
new is designed single-player-only**. Multiplayer capability is required by the constitution but is not
the immediate next work item. Per the revisit path recorded below, Option A stands as written and no
re-shaping is required. `problem.md` and `research.md` are unchanged by the answer, as predicted.

The reasoning that produced `needs-clarification` remains valid and is not retracted: the blocker was
real, and guessing it would have meant silently resolving a question the maintainer had not answered —
most likely producing a specification for "register the compost item" immediately after he had declared
multiplayer non-negotiable. Cheap blocker, expensive to guess wrong.

Option A is now confirmed on the merits:

- It is the only option that moves the **playable surface** metric off **0**, which is the problem's
  central condition; every other success metric is unmeasurable while that stays zero.
- It is a **precondition for satisfying Principle VI**, not an alternative to it. Gate 3 requires a
  second player observing consistent values, which is impossible against a mod that will not launch.
- It ships **no single-player-only path**, so it satisfies the clarification directly.
- It is the cheapest route to a trustworthy signal, which is the actual problem.

Residual concern, recorded not resolved: Option A delivers **no multiplayer capability**, so the
constitution's substantive mandate stays unserved by this work item. D1 remains tracked in `ROADMAP.md`
as required, and its Gate 3 verification is what Option A unblocks. This ordering is now the
maintainer's explicit choice, not this assessment's recommendation.

## Clarification Received (was: If needs-clarification)

**Blocking question 1 — ANSWERED 2026-10-03 by the maintainer:**

> [NEEDS CLARIFICATION: Does Principle VI require **multiplayer capability as the next work item**, or
> only that **no new feature is designed single-player-only** while multiplayer arrives later? This
> selects Option A versus Option B and is outcome-determinative for everything downstream.]

**Answer**: only that no new feature is designed single-player-only. → **Option A confirmed. Verdict
converts to `go`. Revisit stage `shape` was NOT required.**

**Blocking question 2 — DOWNGRADED, no longer blocking:**

> [NEEDS CLARIFICATION: Is a small Content Patcher JSON an acceptable way to register the compost and
> bin items, or must all content registration stay in C# via `AssetRequested`?]

This is a technical question, not a maintainer preference, and it was never answerable by the
assessment pipeline — only by verification. It is therefore **carried into `/speckit.specify` as the
first task, framed as a spike rather than a commitment.** The supporting evidence for the C# route
(`AssetRequested` + `AsDictionary<string,string>()`) currently rests on a host outside the URL Trust
Policy safe list and is marked `[UNVERIFIED]` in `research.md`. If Content Patcher proves materially
cheaper, Option A's route and appetite change — but its *scope* (make the loop playable) does not, so
the decision survives either outcome.

## If go — Handoff to `/speckit.specify`

- **Problem**: the project cannot identify its highest-value next step because its decision signals are
  untrustworthy — 816 green tests over a mod with no playable surface.
- **Chosen approach**: **Option A — Make It Playable First**, plus the Option C documentation rider
  (correct the stale specs and README claims). Appetite small–medium, *confidence low — treat the first
  task as a spike.*
- **What the user experiences**: craft the composting bin, add organic waste, wait for maturation,
  collect compost, apply it to a tilled tile, and see the soil health change. A complete, if plain,
  gameplay loop instead of nothing happening.
- **In scope**: content registration for the compost item and composting bin (including a real machine
  behaviour in place of the empty `Data/Machines` branch); connecting soil health data to the
  visualization layer so the overlay renders; correcting the stale specs and README claims.
- **Out of scope**: multiplayer synchronization itself (D1, tracked in `ROADMAP.md`, required by
  Principle VI but not this work item); custom sprites, audio, i18n wiring; the `HoeFeedbackRenderer`
  refactor; overlay visual polish; `ModController.cs` refactoring beyond what the change strictly
  requires; everything in `ROADMAP.md`; CI gate enablement.
- **Success metrics** (from `problem.md`): playable surface moves off **0**; **0** specs marked
  complete describe an unreachable capability; **3/3** README claims true or explicitly labelled; the
  green suite no longer overstates working software; governance, specs, and code agree on multiplayer
  scope.
- **Carried-forward open questions**: farmhand read/write model; live-vs-save-time sync; farmhand
  overlay visibility; Gate 3 verification hardware availability; `WriteJsonFile` vs `WriteSaveData`
  host-authority semantics on the pinned SMAPI version; whether the empty `Data/Machines` branch and the
  dead `HoeFeedbackRenderer` are defects to correct or intentional deferrals to record.
- **First task is a spike, not a commitment**: settle Content Patcher vs. C# `AssetRequested` for item
  registration. The research supporting the C# route is `[UNVERIFIED]`. Scope is unaffected by the
  outcome; route and appetite are.
- **Constraint inherited from the clarification**: nothing shipped may be designed single-player-only.
  The persistence transport remains host-local and must not be quietly "fixed" as part of this work —
  it is D1's concern.
