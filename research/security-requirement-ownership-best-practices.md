# Research Report: Ownership of Security Requirements (RACI, Security Champions, OWASP, NIST)

**Date:** 2026-10-26
**Context:** Spec 010 (headless verification runner) defers security-requirement ownership (CHK052 / C-SEC-19) and periodic security review cadence (CHK053 / C-SEC-20) to an unnamed "project process doc" that does not exist in the repository. This report decides whether an owner should be assigned **in the spec**, and how, drawing on OWASP, NIST, RACI, and Security Champions practice.

## 1. Executive summary

**Recommendation: Yes — assign a named, accountable owner for each security-requirement domain inside the spec (Governance Annex), and do not defer to an unnamed process doc.**

- Deferral to an unnamed, nonexistent document is the single most common governance-gap finding in audits ("no periodic security process review cycle", "ownership not defined") and contradicts NIST RMF's requirement that the organization **define and document** its monitoring frequency and roles.
- The accountable owner for a solo-maintainer project is honestly the maintainer (`lfgranja`), with a documented fallback and succession rule. RACI collapses to a single Accountable owner, but the table must still exist so delegation is possible later.
- Periodic review cadence must be defined in the spec alongside ownership (see companion `research/security-review-cadence-best-practices.md`), because ownership without a review trigger is unenforceable.
- Infrastructure/ops actions (runner group creation, host hardening) remain deferred to the Governance Annex as before — those are **realization**, not ownership.

This mirrors the already-accepted pattern for CHK038 (FR-026 + Governance Annex A): normative FR in the spec + annex of manual/ops steps.

## 2. The decision question

> Should security-requirement ownership be assigned in the spec, or is deferring to a "project process doc" acceptable?

Two sub-questions:
1. Is deferral to an unnamed process doc acceptable?
2. If an owner is assigned, what shape (RACI? single owner? champion?) and where (spec vs annex vs external doc)?

## 3. Findings

### 3.1 Deferring to an unnamed process doc is not acceptable

- **NIST RMF Monitor step (SP 800-37 Rev 2) and SP 800-137 (ISCM):** the organization must **define and document** its continuous-monitoring strategy, including frequency and rigor, in an organization-defined document. "One size does not fit all" — but the frequency must be *defined*, not left to an unnamed artifact. Deferral to a doc that does not exist is the opposite of what NIST requires.
- **OWASP DevSecOps Verification Standard REL-008:** "A defined, periodic review schedule governs the security checklist itself… aligned with supply-chain integrity expectations such as those described by the SLSA framework." A checklist with no owner and no schedule cannot satisfy this.
- **Traceability (NIST SP 800-160 Vol 2; OWASP CICD-SEC-10):** security requirements must be traceable to threats and to an accountable party. An unnamed deferral breaks the traceability chain at the governance layer, leaving CHK052/CHK053 as permanent `[Gap]`.
- **ISO/IEC 27001:2022 Clause 9.3 / A.5.2 (Information security roles and responsibilities):** roles and responsibilities must be defined and allocated. "Whatever cadence you choose, document it and stick to it" (episki/ISO practice). An unnamed doc cannot be audited.
- **Audit reality:** the most common audit finding in small-org assessments is "no documented owner for security requirements / no periodic review cycle" (kelseyqms common gap). Deferral reproduces exactly that finding.

**Conclusion on Q1:** Deferral is acceptable *only* if the target document is **named, versioned, exists in the repo, and is linked** from the spec (Option C in the cadence report). Even then, the **accountable owner and cadence value should be stated in the spec** so the spec is self-auditable. Given no such doc exists today, the lower-risk choice is to assign ownership in the spec.

### 3.2 RACI for security requirements

RACI (Responsible / Accountable / Consulted / Informed) is the standard model for assigning requirement ownership. Key rules:

- **One Accountable owner per domain.** Multiple "Accountables" defeats the purpose; accountability must be singular. (PMI / PRINCE2 / ISO 27001 A.5.2.)
- **Responsible** may be many (who does the work); **Consulted** (input before decision); **Informed** (notified after).
- For a solo-maintainer open-source project, R, A, C, I collapse to the maintainer, but the **Accountable** column must still be recorded so future delegation is a one-line change, not a re-design.

**Recommended RACI for spec 010 security domains:**

| Domain | Accountable (A) | Responsible (R) | Consulted (C) | Informed (I) |
|---|---|---|---|---|
| Token handling (BUILDDEPS_TOKEN) | maintainer | maintainer (+ contributors) | security reviewers | repo owner |
| Artifact integrity (checksums, archives) | maintainer | maintainer | — | repo owner |
| Cache trust boundaries | maintainer | maintainer | — | repo owner |
| Path traversal / input sanitization | maintainer | maintainer | — | repo owner |
| Secret handling in logs | maintainer | maintainer | — | repo owner |
| Evidence-bundle integrity | maintainer | maintainer | verifier maintainers | repo owner |
| Least privilege / runner groups | maintainer | maintainer + infra owner | — | repo owner |
| Fail-closed behavior | maintainer | maintainer | — | repo owner |
| Audit logging | maintainer | maintainer | — | repo owner |
| Cross-cutting (threat mapping, ownership, cadence) | maintainer | maintainer | — | repo owner |

For this project, "maintainer" = `lfgranja` (repository owner). Fallback = repository owner. Succession = recorded in the review record.

### 3.3 OWASP Security Champions

The OWASP Security Champions Guide (incubator project) provides the organizational model for *who* owns security in a team:

- **Principle 4 — "Nominate a dedicated captain":** security ownership needs a **dedicated, named captain** with a clear mandate; doing it "on the side" loses momentum. Leading a program is effectively a full-time responsibility; the captain sets strategy, obtains management support, builds community, drives success, and stays current on practice.
- **Principle 5 — "Trust your champions":** champions are the eyes and ears; trust requires **clear expectations and a mandate to make security decisions within the risk appetite**. Champions co-own the program and influence direction; they are more effective because they are "one of their own."
- **OWASP SAMM (Governance → Policy & Compliance; Design → Security Requirements):** maturity progresses from "identify governance drivers" → "application-specific security baseline" → "measure adherence." Ownership is a governance concern that matures; at Maturity 1 it is enough to *name* an owner and document the baseline.
- **OWASP CICD-SEC-2 (Inadequate IAM):** accountability for identity/access is a first-class control; "who is accountable" is itself a security requirement, not an afterthought.

**Implication for spec 010:** even a solo maintainer benefits from naming a "security owner" (the captain) in the spec, because (a) it makes delegation a documented transfer, (b) it gives reviewers a single point of contact for security questions, and (c) it satisfies the "trust your champions" mandate concept — the owner has explicit decision rights within risk appetite.

### 3.4 NIST governance

- **NIST CSF 2.0 — Govern function, category GV.RR (Roles, Responsibilities, and Authorities):**
  - GV.RR-01: Organizational leadership is **responsible and accountable** for cybersecurity risk and fosters a risk-aware, ethical, continually improving culture.
  - GV.RR-02: Roles, responsibilities, and authorities related to cybersecurity risk are **established, communicated, understood, and enforced**.
  - GV.RR-03: Adequate resources allocated commensurate with risk, roles, and policies.
  - GV.RR-04: Cybersecurity included in human-resources practices.
- **NIST SSDF (SP 800-218):** provides a common vocabulary for secure software development so producers and consumers can communicate; governance (who does what) is part of integrating SSDF into the SDLC.
- **NIST SP 800-137 (ISCM):** monitoring frequency is risk-based and adjustable; the org defines and documents it.

**Implication:** GV.RR-02 is almost a direct specification of what CHK052 asks ("Is an owner assigned for each security requirement domain?"). NIST's answer is unambiguous: yes, roles/responsibilities/authorities must be established, communicated, understood, and enforced — not left to an unnamed document.

### 3.5 Standards consensus on traceability

- **OWASP CICD-SEC-10 (Logging & Visibility):** centralized aggregation, alerting on anomalies — implies someone owns the alerting and the review.
- **SLSA v1.2 threats overview:** supply-chain integrity is "an ongoing discipline that compounds with every release cycle," not a one-time project — implies a named owner for ongoing reassessment.
- **ISO 27001 A.5.2 / A.5.3:** roles and responsibilities defined and allocated; segregation of duties where needed.
- **OWASP ASVS / SAMM:** ownership matures from "named owner + baseline" (M1) to "measured adherence" (M3).

**Consensus:** a named accountable owner per domain, recorded in the spec or a linked, versioned, existing document, plus a documented review cadence, is the minimum bar.

## 4. Decision

### 4.1 Should an owner be assigned in the spec? **Yes.**

Rationale (scientific/standards-backed):
1. NIST CSF 2.0 GV.RR-02 requires roles/responsibilities/authorities to be established, communicated, understood, and enforced — an unnamed deferral cannot be "communicated" or "enforced."
2. NIST RMF/ISCM requires monitoring frequency to be defined and documented by the organization — not deferred to a nonexistent artifact.
3. OWASP REL-008 and CICD-SEC-2 treat ownership/accountability as a first-class control.
4. ISO 27001 A.5.2 requires roles and responsibilities to be defined and allocated.
5. Traceability (threat → requirement → owner → review record) is broken otherwise; CHK052/CHK053 remain permanent `[Gap]`.
6. Audit reality: unnamed deferral reproduces the most common small-org audit finding.

### 4.2 How — concrete recommendation

Adopt the **Governance Annex pattern** already used for CHK038 (FR-026 + Governance Annex A):

1. **In the spec (normative):** amend `C-SEC-19 (CHK052)` to pin ownership:
   > "Security owner = repository maintainer (`lfgranja`) as the single accountable owner for all security-requirement domains listed in the Governance Annex Ownership Table, until a designated security owner is named; ownership transfers are recorded in the review record. R/A/C/I per domain as tabulated. Fallback = repository owner."
2. **In the spec (normative):** amend `C-SEC-20 (CHK053)` with the tiered cadence from `research/security-review-cadence-best-practices.md` §4 (trigger-driven / quarterly lightweight / annual full / per-PR automated).
3. **In the Governance Annex (new "Ownership Table" + "Security Review Record"):**
   - Domain → Accountable owner → Responsible → Consulted → Informed → last-reviewed date → next due date.
   - Succession rule: on maintainer change, the new maintainer assumes accountability; transfer recorded with date.
   - Review record template (dated, reviewer, scope, outcome, deviations) stored alongside `checklists/security.md`.
4. **Update `C-SEC-24`:** remove the deferral clause; state ownership and cadence are now defined in C-SEC-19/C-SEC-20 and recorded in the Governance Annex.
5. **Mark CHK052 and CHK053 as `[x]`** in `checklists/security.md` with references to C-SEC-19/C-SEC-20 and the Governance Annex.
6. **Optional (Option C, later):** extract the cadence + ownership into a top-level `SECURITY_REVIEW.md` once stable; the spec remains source of truth until then. If an external doc is introduced, it must be **named, versioned, present in the repo, and linked** — never an unnamed reference.

### 4.3 What should remain deferred (not owned in spec)

Keep the precedent: **infrastructure realization** (runner group creation, host OS hardening, firewall rules) is governance/ops, documented in the Governance Annex as a checklist the maintainer signs off on — not enforced by code (FR-026). This is *realization*, not *ownership*, and the distinction (already established for CHK038) should be preserved.

### 4.4 Solo-maintainer honesty

The project is a solo-maintainer open-source mod. RACI must not pretend there is a team. The honest formulation is: **single accountable owner = maintainer**, with a documented fallback and a succession rule, plus an explicit invitation for contributors to be *Consulted* (PR review) and *Informed* (security advisories). This keeps the door open for future delegation without re-designing the governance model, and matches OWASP SAMM Maturity 1 ("name an owner and document the baseline").

## 5. Recommended spec edits (summary table)

| Item | Change |
|---|---|
| C-SEC-19 (CHK052) | Pin ownership: maintainer = single accountable owner; RACI table in Governance Annex; fallback + succession rule |
| C-SEC-20 (CHK053) | Tiered cadence (trigger / quarterly / annual / per-PR) |
| C-SEC-24 | Remove deferral clause; point to C-SEC-19/C-SEC-20 + Governance Annex |
| Governance Annex | Add "Ownership Table" + "Security Review Record" template |
| checklists/security.md | Mark CHK052, CHK053 as `[x]` with references |
| (optional) | Later extract to `SECURITY_REVIEW.md` (named, versioned, linked) |

## 6. Sources

### Standards & frameworks
- NIST CSF 2.0 — Govern function, category GV.RR (Roles, Responsibilities, and Authorities): GV.RR-01..GV.RR-04. https://www.nist.gov/cyberframework
- NIST SP 800-218 (SSDF) v1.1 — common vocabulary for secure software development. https://csrc.nist.gov/pubs/sp/800/218/final
- NIST SP 800-37 Rev 2 — RMF Monitor step (ongoing situational awareness, organization-defined monitoring). https://csrc.nist.gov/projects/risk-management
- NIST SP 800-137 — ISCM (organization defines and documents monitoring frequency). https://www.govinfo.gov/content/pkg/GOVPUB-C13-PURL-gpo28843/pdf/GOVPUB-C13-PURL-gpo28843.pdf
- ISO/IEC 27001:2022 — Clause 9.3 (management review at planned intervals); Annex A 5.2 (information security roles and responsibilities); A.5.3 (segregation of duties).
- OWASP Top 10 CI/CD Security Risks — CICD-SEC-2 (IAM accountability), CICD-SEC-10 (logging & visibility). https://owasp.org/www-project-top-10-ci-cd-security-risks/
- OWASP SAMM — Governance (Policy & Compliance), Design (Security Requirements). https://owaspsamm.org/model/
- OWASP Security Champions Guide — Principle 4 (nominate a dedicated captain), Principle 5 (trust your champions). https://securitychampions.owasp.org/
- OWASP DevSecOps Verification Standard REL-008 (defined periodic review schedule). https://owasp.org/www-project-devsecops-verification-standard/
- SLSA v1.2 — threats overview; ongoing discipline per release cycle. https://slsa.dev/spec/v1.2/threats-overview
- OWASP Secure Product Design Cheat Sheet — threat modeling producing security stories; abuse cases. https://cheatsheetseries.owasp.org/cheatsheets/Secure_Product_Design_Cheat_Sheet.html

### Project-internal
- `specs/010-headless-verification-runner/spec.md` (C-SEC-19, C-SEC-20, C-SEC-24, FR-026)
- `specs/010-headless-verification-runner/checklists/security.md` (CHK052, CHK053)
- `research/security-review-cadence-best-practices.md` (tiered cadence, Option A/B/C analysis)
- `research/security-gaps-security-and-supply-chain-best-practices.md` (§7.3 CHK052, §7.4 CHK053)
- `research/runner-group-segregation-least-privilege-ci-runners.md` (Governance Annex precedent)
- `VERIFICATION_PROTOCOL.md`, `CONTRIBUTING.md` (Gate 3 verification contract, process artifacts)

---

*End of research deliverable. No code edited; no commit made. Companion to `research/security-review-cadence-best-practices.md`.*