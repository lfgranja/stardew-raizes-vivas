# Static Checks — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06  
**Evidence class:** `automated` (static/format gate)

## Formatting gate

**Exact command:**

```bash
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

**Result:** exit code `0` — no formatting changes required.  
**Output note:** the only message was the informational workspace-load warning:

```text
Foram encontrados avisos ao carregar o workspace. Defina a opção de detalhamento para o nível de 'diagnóstico' para registrar avisos.
```

That warning is about workspace diagnostic verbosity, not a formatting violation.

**Source log:** `specs/017-joja-agrochemical-tradeoff/verification/static-checks.log`

## Build

The full `Release` test runs build the solution (including `LivingRoots`, `LivingRoots.Tests`, and
`LivingRoots.Verification`) before executing tests, and exited `0`:

- Targeted 017 suite: `verification/automated-tests-targeted.log`
- Full suite: `verification/automated-tests-full.log`

No separate build command was needed to establish compilation for this verification pass.

## Scope

This record covers formatting and compilation only. It is not behavioral, real-game, Gate 3, D1,
human, or SC-004 evidence.

## Phase 8 reruns (T061)

**Date:** 2026-11-06  
**Evidence class:** `automated` (static/format gate) after 017 verification-runner support wiring (T056–T060).

### Formatting gate

```bash
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

**Result:** exit code `0` (workspace-load warning only; no formatting changes required).

### 017 runner shell suite

```bash
bash scripts/tests/test-017-agrochemical.sh
```

**Result:** 23 passed, 0 failed after the `runMacro` steps-phase mapping and the 017 scenario/`test.md` marker validation were in place.
