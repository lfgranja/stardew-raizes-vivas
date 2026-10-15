# Machine Lifecycle & State Retention Requirements Quality Checklist: Playable Compost Loop

**Purpose**: Requirements-quality verification checklist ("Unit Tests for English") evaluating Completeness, Clarity, Consistency, Measurability, Coverage, and Edge Cases for Machine Lifecycle, Inventory & State Retention in the Playable Compost Loop feature specification.
**Created**: 2026-10-04
**Feature**: [Playable Compost Loop Specification](../spec.md)

**Note**: This custom checklist is generated based on feature context and requirements.
**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.
**Marker Semantics**: `[x]` means the criterion has been reviewed and satisfied for requirements quality. It does not mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Does the specification define what game data structures or fields store mid-processing progress and held inputs when a bin is picked up into the player's inventory? [Completeness, Spec §FR-005c, §FR-012]
- [x] CHK002 Does the specification define the fate of partially processed inputs if a placed bin is removed using different tools (e.g., pickaxe, axe, hoe) or destroyed by environment/bombs? [Completeness, Spec §FR-005c, §Edge Cases]
- [x] CHK003 Does the specification define whether multiple bins carrying distinct progress levels can stack in inventory, or if carrying progress forces them to be unstackable? [Completeness, Spec §FR-005, §FR-005c]
- [x] CHK004 Does the specification define the cleanup mechanism that guarantees zero orphaned state in save data upon bin removal across all farm locations? [Completeness, Spec §FR-002, §FR-005c]
- [x] CHK005 Does the specification define whether placed bins are restricted to specific farm maps/locations or allowed indoors (e.g., greenhouse, sheds, farmhouse)? [Completeness, Spec §FR-001, §FR-002, §Edge Cases]

## Requirement Clarity & Precision

- [x] CHK006 Is "native game machine" unambiguously specified with respect to vanilla Stardew machine contracts (e.g., StardewValley.Object machine rules, `heldObject`, `readyForHarvest`, minutes until ready) versus custom SMAPI ticking? [Clarity, Spec §FR-001a, §FR-001b]
- [x] CHK007 Is the "one in, one out" batch rule explicitly quantified to prevent ambiguity regarding multi-item input stacks (e.g., consuming exactly 1 unit of organic waste vs whole stack)? [Precision, Spec §FR-005a, §Clarifications]
- [x] CHK008 Does the specification clearly define how the bin signals to the player that an input was accepted versus rejected without introducing custom UI? [Clarity, Spec §FR-003, §FR-008a]
- [x] CHK009 Is the exact wording, display duration, and vanilla HUD channel specified for the refusal reason when compost collection fails due to a full inventory? [Clarity, Spec §FR-005b, §FR-008a]
- [x] CHK010 Are the bounds and exact units of "processing progress" (e.g., days, minutes, percentages) explicitly defined to avoid conflicting interpretations across single-player and multiplayer sleep cycles? [Precision, Spec §FR-001b, §FR-004, §FR-012]

## State Retention & Lifecycle Coverage

- [x] CHK011 Does the specification cover state retention and progress consistency across an immediate save-and-reload cycle when a bin has just accepted an input but not yet advanced a day? [Coverage, Spec §FR-012, §SC-004]
- [x] CHK012 Does the specification define whether a bin holding ready, uncollected compost retains its output without rotting, resetting, or despawning across season transitions and festival days? [Coverage, Spec §FR-005b, §FR-012]
- [x] CHK013 Does the specification define the behavior when placing a bin that previously held ready compost (i.e., whether it immediately displays the ready-for-harvest bubble)? [Coverage, Spec §FR-001a, §FR-005c]
- [x] CHK014 Does the specification define what happens to bin processing if the machine is picked up and held in inventory across a day boundary or overnight sleep cycle? [Coverage, Spec §FR-004, §FR-005c]
- [x] CHK015 Does the specification specify whether moving a bin resets any internal timestamps or daily tick flags? [Coverage, Spec §FR-005c]

## Edge Case & Failure Path Specification

- [x] CHK016 Does the specification explicitly define how item duplication and item deletion are prevented if a player's inventory becomes full during bin pickup while the bin contains contents? [Edge Cases, Spec §FR-005b, §FR-005c, §SC-009]
- [x] CHK017 Does the specification specify how the machine handles rapid consecutive interactions (e.g., spam-clicking harvest or spam-clicking input load) to guarantee atomic batch transitions? [Edge Cases, Spec §FR-005a, §FR-019]
- [x] CHK018 Does the specification define the exact resolution path when two players concurrently attempt to pick up or collect from the same bin at the exact same tick? [Edge Cases, Spec §FR-019, §SC-010]
- [x] CHK019 Does the specification specify the behavior if a player attempts to load a second item of qualifying organic waste into a bin that is already actively processing a batch? [Edge Cases, Spec §Key Entities, §FR-003]
- [x] CHK020 Does the specification define how corrupted, obsolete, or disconnected bin save entries from previous mod versions are quarantined without crashing save loads or duplicating machines? [Failure Paths, Spec §FR-012, §FR-014]

## Acceptance Criteria & Measurability

- [x] CHK021 Is the criterion for "zero orphaned state" accompanied by an objective, measurable inspection procedure (e.g., save file XML/JSON inspection criteria)? [Measurability, Spec §FR-002, §SC-004]
- [x] CHK022 Is there a concrete, verifiable definition for what constitutes "identical progress restored" when placing a moved bin back down? [Measurability, Spec §FR-005c, §SC-004]
- [x] CHK023 Does SC-009 define specific pass/fail threshold metrics for repeated full-inventory collection attempts across different inventory slot configurations? [Measurability, Spec §SC-009]
- [x] CHK024 Is the verification requirement for "first action wins" between two concurrent players objectively testable with specified observable outcomes for both players? [Measurability, Spec §FR-019, §SC-010]

## Notes

- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied
- Leave items unchecked when they still require clarification, correction, or reviewer evaluation
- `/speckit-implement` reads checklist checkbox state as a gate and must not modify markers
- `checklists/requirements.md` has a separate built-in lifecycle maintained by `/speckit-specify` and `/speckit-clarify`
- Add comments or findings inline
- Link to relevant resources or documentation
- Items are numbered sequentially for easy reference
