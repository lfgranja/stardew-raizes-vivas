# Decision: CI / Release Gate (spec 019)

**Verdict:** go (local-only).

**Rationale (one line):** production without human approval requires an authoritative local gate and an honest, host-side-enablement-ready CI topology; without this, protected-branch SDD work cannot be auto-merged honestly.

**In scope:**
- `scripts/release-gate.sh` (deterministic, fail-closed, secret-free).
- Spec artifacts (this directory), honest verification record.
- `release.yml` (local-only release workflow) authored but marked unverified remote.

**Out of scope:**
- Host-side configuration, auto-merge, tag push, publishing, enabling protected settings (explicit authorization or owner).
- Claims of hosted CI greenness (cannot be attested locally).
- Any change to the MDMV constitution/hub or this repo's `018` coverage ledger.

**Preconditions honored (R1-like):** constitution principles cited in `spec.md`; research is file-bound and fresh; hub-first 022 status `local-only`.
