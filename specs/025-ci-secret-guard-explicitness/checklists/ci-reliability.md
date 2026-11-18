# Requirements Quality Checklist: CI credential-guard explicitness

**Purpose**: Requirements-quality review (unit tests for English), not implementation verification
**Created**: 2026-11-18
**Feature**: [spec.md](../spec.md)

> `[x]` means a reviewer judged the *requirement* complete, clear and testable. It does **not** mean the implementation is done.

## Requirement Completeness

- [x] CHK001 - Are the credential-dependent jobs enumerated with their current guard status? [Completeness, Spec §FR-001]
- [x] CHK002 - Is the required behavior specified for both unset and empty credential values? [Completeness, Spec §FR-002]
- [x] CHK003 - Are requirements stated for the credential-present path so unchanged behavior is explicit? [Completeness, Spec §FR-003]
- [x] CHK004 - Are documentation requirements specified for the local-versus-hosted trust boundary? [Completeness, Spec §FR-007]

## Requirement Clarity

- [x] CHK005 - Is the required diagnostic message specified precisely enough to be compared literally? [Clarity, Spec §FR-004]
- [x] CHK006 - Is "best-effort" defined so the exemption is unambiguous? [Clarity, Spec §FR-006]
- [x] CHK007 - Is the ordering of the guard relative to first credential use stated? [Clarity, contracts/ci-check-contract.md]

## Requirement Consistency

- [x] CHK008 - Do FR-002 (fail closed) and the best-effort exemption in FR-006 avoid contradiction by scoping the exemption to a named job family? [Consistency]
- [x] CHK009 - Are the success metrics consistent with the stated baseline of two guarded consumers out of three? [Consistency, Spec §SC-001]

## Acceptance Criteria Quality

- [x] CHK010 - Can SC-001 be measured without reading implementation code? [Measurability]
- [x] CHK011 - Does SC-002 require two observable outcomes rather than an inferred one? [Acceptance Criteria]
- [x] CHK012 - Are [NV] claims explicitly excluded from being promoted by local evidence? [Measurability, Spec §FR-008]

## Scenario Coverage

- [x] CHK013 - Are primary, alternate and error flows represented across the three user stories? [Coverage, Spec §User Scenarios]
- [x] CHK014 - Is the future-job regression scenario covered as its own acceptance case? [Coverage, Spec §User Story 2]

## Edge Case Coverage

- [x] CHK015 - Are empty-string credential, best-effort job, concurrent workflow edits and post-download failure specified as distinct cases? [Edge Case, Spec §Edge Cases]

## Non-Functional Requirements

- [x] CHK016 - Are fail-closed and no-secret-disclosure constraints stated as requirements? [Security, Spec §FR-002 and §FR-008]

## Dependencies & Assumptions

- [x] CHK017 - Are the YAML test dependency and the owner-only boundary recorded as assumptions? [Assumption, Spec §Assumptions]

## Ambiguities & Conflicts

- [x] CHK018 - Is the unverified remote root cause of red PRs recorded as an open boundary rather than a guessed cause? [Ambiguity, Spec §Assumptions]

## Notes

- Generated 2026-11-18 by `/speckit-checklist`; focus areas: CI reliability, fail-closed semantics, traceability to §FR/§SC; depth: standard; actor/timing: reviewer during PR review.
- Reviewer pass 2026-11-18: 18/18 satisfied after one source correction — CHK001 prompted adding the enumerated credential-consumer inventory to `spec.md` §Key Entities. `[x]` is a requirements-quality judgement only; no implementation is claimed complete.
