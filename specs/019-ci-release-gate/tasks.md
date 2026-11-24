# Tasks: CI / Release Gate (spec 019)

- [x] T001 Author `scripts/release-gate.sh` (deterministic, fail-closed, secret-free; exit codes 3/4/11–17). **Evidência:** `bash -n` + `chmod 0755`.
- [x] T002 Re-certify launch-free smoke on quiet host (`SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh`, TMPDIR outside repo). **Evidência:** run 2026-11-08 on this host, `/tmp/opencode/smoke-run-all-pass14.txt`: **exit 0 — 50 files, 49 passed, 0 failed, 1 skipped** (`test-integration.sh`, intentional). Resolves the pass 12 pre-push smoke finding.
- [x] T003 019 scaffold: `spec.md`, `research.md`, `decision.md`(go, local-only), `plan.md`, `quickstart.md`, `tasks.md`.
- [x] T004 Author `.github/workflows/release.yml` (local-only release: tag `v*.*.*` → ReleaseZip artifact). **Evidência pendente (no hosted run):** workflow authored, NOT executed remotely; activation requires host enablement.
- [x] T005 Gate executed for real on this host; output recorded (`verification/verification.md`). **Evidência (2026-11-08, hostquieto):** `gate_v4_exit=0` — steps 1–7 OK, smoke 50 files/49 pass/1 skip, tests 1937/4/0, `ALL STEPS PASSED`, commit `4860cf2`. History of failures (MSB4166 under memory pressure; dirty-tree stop) recorded in verification.md.
- [ ] T006 (owner action outside repo): secrets/vars, required checks, branch protection/rulesets, auto-merge policy, tag/release publish per `quickstart.md`. **Never done autonomously.**
- [ ] T007 Hub-first honesty marker honored: nothing in this feature is an MDMV default; `local-only` stays until hub evaluation.
