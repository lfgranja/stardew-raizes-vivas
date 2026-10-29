# Runner-Group Segregation & Least Privilege for CI Runners — Best Practices Research

**Date:** 2026-10-26
**Context:** Stardew Valley mod "Living Roots" — headless verification runner (spec 010), dormant `Build and Test` workflow, self-hosted GitHub Actions runner executing a game verification runner that handles proprietary game files (`BUILDDEPS_TOKEN`) and secrets. Closes checklist gap **CHK038** in `specs/010-headless-verification-runner/checklists/security.md` (also informs CHK037 deny-by-default and CHK039 default GITHUB_TOKEN read-only).
**Scope:** Runner-group segregation (high-privilege vs. low-privilege runners), non-root execution, permission minimization, secret isolation, container/VM isolation, ephemeral runners, network egress control, and autonomous-resolvability determination.
**Sources:** GitHub secure-use reference; OWASP GitHub Actions Security Cheat Sheet; OWASP CI/CD Security Cheat Sheet; OWASP Top 10 CI/CD Security Risks (CICD-SEC-2, CICD-SEC-5, CICD-SEC-6, CICD-SEC-7); NIST SP 800-204D; CISA/NSA "Defending CI/CD Environments" CSI; SLSA v1.0; CIS Controls v8; GitHub Actions Runner Controller; GitLab Runner security docs; industry practitioner guidance (AWS DevOps blog, Equinor AppSec, Medium regulated-env writeup).

---

## 1. Executive Summary

- **CHK038 (runner-group segregation)** is a **Gap** that can be partially closed by a normative FR in the spec, but the *operational realization* (creating the GitHub runner group, registering the self-hosted runner, configuring repo access) is an **infrastructure/governance action** that cannot be performed by code changes in this repo alone. **Autonomous-resolvability: Partial** — the spec can mandate the requirement; the execution is a governance/ops task.
- **CHK037 (deny-by-default / non-root)** and **CHK039 (default GITHUB_TOKEN read-only)** are **autonomously resolvable** via workflow YAML and code-level changes; CHK038 is the only one of the three that requires out-of-band infrastructure action.
- **Recommendation:** Add a single normative FR (FR-027 or a dedicated FR-031) capturing runner-group segregation + non-root + ephemeral + network egress as a *contract* the repo's CI and runner must satisfy, with an explicit **governance annex** listing the manual/infra steps the maintainer must perform. Mark CHK038 as "Resolved by FR-027 §Runner-Group Segregation + Governance Annex A" once the annex is acknowledged.
- **In-scope determination:** The *requirement wording* is in-scope for spec 010 (it is a security requirement of the verification runner's CI surface). The *realization* (GitHub runner group creation, runner registration, host hardening) is **governance/ops**, not spec. This matches the project's established pattern: the spec defines contracts (FR-002 token scoping, FR-023 local/CI parity); infrastructure actions (runner registration, host OS hardening) are documented in AGENTS.md / VERIFICATION_PROTOCOL.md, not coded.

---

## 2. Research Findings by Source

### 2.1 GitHub Secure-Use Reference — Self-Hosted Runner Hardening

**Source:** [GitHub Docs — Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use) (§"Hardening for self-hosted runners").

Key findings:

1. **Runner groups are the primary segregation mechanism.** "When a self-hosted runner is defined at the organization or enterprise level, GitHub can schedule workflows from multiple repositories onto the same runner. Consequently, a security compromise of these environments can result in a wide impact. To help reduce the scope of a compromise, you can create boundaries by organizing your self-hosted runners into separate groups. You can restrict what organizations and repositories can access runner groups."
2. **Self-hosted runners are NOT ephemeral by default.** Unlike GitHub-hosted runners (ephemeral VMs), self-hosted runners persist state between jobs and can be persistently compromised by untrusted code.
3. **Secret cross-job leakage is a known risk.** "Some customers might attempt to partially mitigate these risks by implementing systems that automatically destroy the self-hosted runner after each job execution. However, this approach might not be as effective as intended, as there is no way to guarantee that a self-hosted runner only runs one job. Some jobs will use secrets as command-line arguments which can be seen by another job running on the same runner, such as `ps x -w`. This can lead to secret leaks."
4. **Machine-level sensitive data inventory.** The reference mandates assessing: what sensitive information resides on the runner machine (SSH keys, API tokens), and whether the machine has network access to sensitive services (cloud metadata endpoints). "The amount of sensitive information in this environment should be kept to a minimum."
5. **Public repo prohibition.** Self-hosted runners should "almost never" be used for public repositories; forks can execute dangerous code.

**Relevance to spec 010:** The verification runner handles `BUILDDEPS_TOKEN` (a fine-grained PAT with `Contents: read` + `Metadata: read`) and proprietary game files. If a low-privilege job (e.g., linting, static analysis) runs on the same runner as the verification job, it can read the token via `ps` or filesystem inspection. Runner-group segregation prevents this.

### 2.2 OWASP GitHub Actions Security Cheat Sheet — Segregation of Runners

**Source:** [OWASP Cheat Sheet Series — GitHub Actions Security](https://cheatsheetseries.owasp.org/cheatsheets/GitHub_Actions_Security_Cheat_Sheet.html) (§"Segregate runners using runner groups and labels").

Key findings:

1. **Explicit high-privilege vs. low-privilege separation.** "Use runner groups and labels to separate high-privilege runners from low-privilege runners. High-privilege runners may have access to sensitive resources, while low-privilege runners should not."
2. **Concrete example categories:**
   - A runner group for container image build runners, limited to only the repositories that require those privileges.
   - A runner group for runners with access to restricted networks.
   - A separate runner group for low-privilege tasks such as linting and static analysis, used by repositories where secrets are absent or isolated in separate environments.
3. **Benefit:** "This separation gives more granular control over which repositories can access a runner group and which workflows can target specific runners, reducing the risk that a compromised or misconfigured workflow gains access to sensitive resources."

**Relevance to spec 010:** The verification runner is a **high-privilege workload** (accesses `BUILDDEPS_TOKEN`, downloads proprietary game files, writes evidence bundles with secrets-adjacent data). It must NOT share a runner pool with low-privilege tasks (linting, unit tests without secrets, static analysis). The spec must mandate a dedicated runner group.

### 2.3 OWASP CI/CD Security Cheat Sheet — Least Privilege & Isolation

**Source:** [OWASP Cheat Sheet Series — CI/CD Security](https://cheatsheetseries.owasp.org/cheatsheets/CI_CD_Security_Cheat_Sheet.html).

Key findings:

1. **Least privilege applies to OS accounts.** "The OS accounts responsible for running the pipeline should not have root or comparable privileges; this will help mitigate impact in case of compromise."
2. **Pipeline node isolation.** "Perform builds in appropriately isolated nodes."
3. **No privileged containers.** "If pipeline steps are executed in Docker image, avoid using the `--privileged` flag."
4. **MFA enforcement** where possible.

**Relevance to spec 010:** The runner process (game + verification mod) must execute as a non-root user. This is CHK037 (deny-by-default) but directly supports CHK038 (segregation) because a non-root runner process reduces the blast radius even within a segregated group.

### 2.4 NIST SP 800-204D — CI/CD Pipeline Security

**Source:** [NIST SP 800-204D](https://csrc.nist.gov/pubs/sp/800/204/d/final) (Strategies for the Integration of Software Supply Chain Security in DevSecOps CI/CD Pipelines).

Key findings:

1. **Harden the execution environment.** "Harden the CI/CD execution environment (e.g., VM or pod) to reduce its attack surface."
2. **Define roles for actors.** "Define roles for the actors who operate the various CI/CD pipelines (e.g., application updaters, package managers, deployment specialists)."
3. **Granular authorizations.** "Identify the granular authorizations to perform various tasks, such as generating and committing code to SCMs, generating builds and packages, and checking various artifacts into and out of the repositories."
4. **Driver tools at higher trust.** "The driver tools or build control plane execute at a higher level of trust than the individual functional steps, such as build." This supports separating the verification runner (high-trust driver) from individual build steps (lower trust).

**Relevance to spec 010:** The verification runner is a high-trust driver tool (it handles secrets, proprietary files, and produces evidence bundles). It must be isolated from lower-trust build steps.

### 2.5 CISA/NSA CSI — Defending CI/CD Environments

**Source:** [CISA/NSA Cybersecurity Information Sheet](https://media.defense.gov/2023/Jun/28/2003249466/-1/-1/0/CSI_DEFENDING_CI_CD_ENVIRONMENTS.PDF) (June 2023).

Key findings:

1. **Implement least-privilege policies for CI/CD access.**
2. **Network segmentation and traffic filtering** (CPG 2.F).
3. **Secure user accounts** — regularly audit administrative user accounts, configure access controls under least privilege and separation of duties.
4. **Isolated nodes** for builds.

**Relevance to spec 010:** Network egress restriction is a complementary control to runner-group segregation. The verification runner should only reach GitHub API (for `BUILDDEPS_TOKEN`-authenticated downloads) and the private builddeps repo; all other egress should be blocked.

### 2.6 OWASP Top 10 CI/CD Security Risks — CICD-SEC-5 (PBAC)

**Source:** [OWASP Top 10 CI/CD Security Risks](https://owasp.org/www-project-top-10-ci-cd-security-risks/).

Key findings:

1. **CICD-SEC-5 (Inadequate PBAC — Pipeline-Based Access Controls):** "The CI/CD pipeline itself is a powerful entity with access to sensitive resources. If the pipeline's permissions are not properly scoped, a compromised pipeline can access resources beyond what it needs."
2. **CICD-SEC-2 (IAM):** Inadequate identity and access management in CI/CD systems.
3. **CICD-SEC-6 (Credential Hygiene):** Improper handling of secrets in CI/CD.
4. **CICD-SEC-7 (Insecure System Configuration):** Misconfigured CI/CD systems.

**Relevance to spec 010:** The verification runner's `BUILDDEPS_TOKEN` and proprietary game files represent sensitive resources. PBAC ensures only the verification runner's designated runner group can access them.

### 2.7 GitHub Actions Runner Controller (ARC) — Ephemeral Runners

**Source:** [GitHub Docs — Actions Runner Controller](https://docs.github.com/en/actions/concepts/runners/actions-runner-controller).

Key findings:

1. **Ephemeral runners via JIT config.** ARC creates runner pods that are ephemeral — destroyed after one job. "Because controlled runners can be ephemeral and based on containers, new runner instances can scale up or down rapidly and cleanly."
2. **JIT (Just-in-Time) registration tokens.** "You can now create single-use self-hosted runners without time-limited registration tokens using the REST API. When a runner registers using this API it will only be allowed to run a single job before being automatically removed."
3. **Runner groups in ARC.** The AutoScalingRunnerSet controller fetches the runner group ID that the runner scale set belongs to, enabling group-based segregation at scale.

**Relevance to spec 010:** For production CI, ephemeral/JIT runners are the strongest mitigation against cross-job secret leakage. The spec should recommend (not mandate) ephemeral runners as the production upgrade path, consistent with CHK006 (OIDC/short-lived token as production upgrade path).

### 2.8 GitLab Runner Security — Non-Root & Capability Dropping

**Source:** [GitLab Docs — Security for self-managed runners](https://docs.gitlab.com/runner/security/).

Key findings (cross-platform CI runner security):

1. **Shell executor = high risk.** "High-security risks exist to your runner host and network when running builds with the shell executor. The jobs are run with the permissions of the GitLab Runner's user and can steal code from other projects that are run on this server. Use it only for running trusted builds."
2. **Docker executor non-root.** "Docker can be considered safe when running in non-privileged mode. To make such a configuration more secure, run jobs as a non-root user in Docker containers with disabled sudo or dropped SETUID and SETGID capabilities."
3. **Capability dropping.** "More granular permissions can be configured in non-privileged mode via the `cap_add`/`cap_drop` settings."
4. **Privileged mode = full root.** "Privileged containers in Docker have all the root capabilities of the host VM... By enabling privileged mode, you are effectively disabling all the container's security mechanisms."
5. **Network segmentation.** "Configuring runner virtual machines in their own network segment; Blocking SSH access from the Internet; Restricting traffic between runner virtual machines; Filtering access to cloud provider metadata endpoints."

**Relevance to spec 010:** The verification runner's game process (Stardew Valley under SMAPI) must run as non-root. If containerized in the future, capabilities must be dropped and `--privileged` forbidden. Network egress must be restricted to prevent metadata endpoint access.

### 2.9 Equinor AppSec — Self-Hosted Runner Threat Model

**Source:** [Equinor AppSec — GitHub Actions and Self-Hosted Runners](https://equinor.github.io/appsec/toolbox/version-control/gh-actions-runners/).

Key findings:

1. **Threat model requirement.** "Involve the team, create, and keep up to date a threat model of your self-hosted runner setup. The threat model should at least include: a data flow/architecture diagram; a list of identified threats/risks; information on how identified threats/risks are managed; acceptance by product owners/risk owners."
2. **Ephemeral preferred.** "We prefer ephemeral environments, non-persistent environments. Just-in-time runners may be an option."
3. **One job at a time.** "Only do one job at a time on the runner (don't do auto-scaling with persistent runners, use the `--ephemeral` flag)."
4. **Least privilege for runner code.** "The runner code should execute with the least possible privilege level."
5. **Regular refresh.** "Environments should be refreshed regularly (update, patch) (weekly)."
6. **Log monitoring.** "Have routines and practices for monitoring/examining run/runner logs."

**Relevance to spec 010:** The spec should mandate a threat model for the runner infrastructure (aligns with CHK051 threat mapping and CHK052 ownership). The runner should be ephemeral (one job) where possible.

### 2.10 AWS DevOps Blog — Runner Groups at Scale

**Source:** [AWS DevOps & Developer Productivity Blog](https://aws.amazon.com/blogs/devops/best-practices-working-with-self-hosted-github-action-runners-at-scale-on-aws/) (June 2024).

Key findings:

1. **Runner groups for security isolation.** "By using ephemeral runners in a single GitHub runner group, you are creating a pool of resources in the same AWS account that are used by all repositories sharing this runner group. Your organizational security requirements may dictate that your execution environments must be isolated further, such as by repository or by environment (such as dev, test, prod)."
2. **Dedicated AWS accounts per group.** "I recommend that your ephemeral runner groups are contained within their own AWS account and that this AWS account has minimal access to other organizational resources."
3. **IAM role assumption via OIDC.** "When access to organizational resources is required, this can be given on a repository-by-repository basis through IAM role assumption with OIDC, and these roles can be given least-privilege access to the resources they require."

**Relevance to spec 010:** The verification runner's runner group should be isolated at the infrastructure level (dedicated VM/account), not just at the GitHub runner-group level. This is a governance/ops action.

### 2.11 DEV Community — Self-Hosted Runners as Shared Secrets

**Source:** [DEV Community — Self-Hosted CI Runners Are Shared Secrets](https://dev.to/kozhevniko/self-hosted-ci-runners-are-shared-secrets-threat-modelling-your-build-infrastructure-2ji3) (Sept 2026).

Key findings:

1. **Runner labels are guessable.** "Runner labels are often guessable" — attackers can target specific runner groups by label.
2. **Persistent runners retain state.** "Because the runner is persistent, the job can write to paths outside the workspace: shell profiles, the runner's own service configuration, or a directory used by the next job."
3. **Cross-job cache poisoning.** "A cache written by one job can be read by another that trusts the cache key."
4. **Secrets via process inspection.** "An attacker who can submit a workflow can request a specific runner group... The result is that the runner is a shared resource with a shared secret store."

**Relevance to spec 010:** Runner labels must not be predictable/guessable. The verification runner's runner group should use non-obvious labels or rely on group-based access control (not label-based routing alone). Cache isolation (CHK020) is a complementary control.

### 2.12 GitHub Bug Bounty — Self-Hosted Runner Scope

**Source:** [GitHub Bug Bounty — GitHub Actions](https://bounty.github.com/targets/github-actions.html).

Key findings:

1. **Job = single trust domain.** "A workflow job is a single trust domain. Any step that can run code in the same job are ineligible [for bounty]."
2. **Cross-job boundary is the security boundary.** "To be eligible, show the artifact crossing a job or repository boundary, such as an artifact from an untrusted job overwriting files used by a more privileged job."
3. **Steps within a job are not security boundaries.**

**Relevance to spec 010:** This confirms that the security boundary is at the job/runner level, not the step level. Runner-group segregation operates at this boundary.

---

## 3. Autonomous-Resolvability Determination

| CHK | Item | Autonomous? | Reason |
|-----|------|-------------|--------|
| CHK037 | Deny-by-default (non-root, non-admin) | **Yes** | Code-level: fail-fast if root, non-root user documented, read-only mounts. Can be implemented in runner script + mod. |
| CHK038 | Runner-group segregation (high vs. low privilege) | **Partial** | The *requirement* can be normatively specified in the spec (FR). The *realization* (creating the GitHub runner group, registering the self-hosted runner, configuring repo access, host OS hardening) requires manual infrastructure/ops actions outside the repo. |
| CHK039 | Default GITHUB_TOKEN read-only | **Yes** | Workflow YAML change: `permissions: contents: read` at top level. |

**CHK038 autonomous-resolvability: PARTIAL.**

- **What the spec can autonomously resolve:** The normative requirement text (FR) defining what the runner group must look like, who can access it, and what audit/review obligations attach. This is a documentation/contract change — fully autonomous.
- **What requires human/ops action:** Creating the runner group in GitHub (org settings), registering the self-hosted runner with `--ephemeral` flag, configuring repo access policy, hardening the host OS (non-root user, network egress rules, no SSH from internet), and ongoing review cadence. These cannot be performed by code commits.
- **Precedent in project:** The spec already follows this pattern — FR-002 mandates token scoping (`Contents: read` + `Metadata: read`) but the actual PAT creation is a manual step documented in the invocation guide. Similarly, CHK038's FR mandates the runner-group contract; the actual group creation is a governance/ops step.

---

## 4. Options & Recommendation

### Option A: Spec-only (normative FR, no governance annex)

Add a single FR requiring runner-group segregation. Mark CHK038 resolved. No governance documentation.

- **Pros:** Simple, closes the gap quickly.
- **Cons:** The FR would be unverifiable — there's no way to confirm from the repo whether the runner group actually exists or is correctly configured. A reviewer cannot validate the requirement. This creates a false sense of security (the "false green" problem the project already identified in CI dormancy).

### Option B: Spec FR + Governance Annex (recommended)

Add the normative FR in the spec AND a governance annex (in `VERIFICATION_PROTOCOL.md` or a new `RUNNER-GROUP-SETUP.md`) documenting the manual/ops steps, with a checklist the maintainer signs off on. Mark CHK038 as "Resolved by FR-027 §Runner-Group Segregation + Governance Annex A (maintainer-verified)."

- **Pros:** Honest — acknowledges that infrastructure config is not code. Provides a verifiable checklist. Aligns with the project's Gate 3 verification discipline (manual verification with multi-signal evidence).
- **Cons:** Slightly more documentation. Requires maintainer follow-through.

### Option C: Defer to governance entirely (no spec change)

Remove CHK038 from the spec checklist and move it to a governance/ops document. The spec would not mention runner-group segregation.

- **Pros:** Clean separation of concerns.
- **Cons:** Loses traceability. The security checklist would have an unaddressed gap. Future contributors wouldn't know the requirement exists. Violates the project's pattern of documenting all security requirements in the spec.

### Recommendation: Option B

Add the normative FR + governance annex. This is consistent with:
- The project's existing pattern (FR-002 mandates token scoping; actual PAT creation is a manual step).
- The Gate 3 verification discipline (manual verification with evidence, not "verified by build").
- The spec's own claim discipline (safe wording, bounded claims, refusal-site liveness).

---

## 5. Concrete Normative Requirement Wording

### Recommended FR (add to spec 010 as FR-027 or a dedicated section)

> **FR-027 (Least Privilege — Runner-Group Segregation & Non-Root Execution):** The verification runner's CI execution environment MUST satisfy the following least-privilege and segregation contract:
>
> **(a) Runner-group segregation (CHK038):** Self-hosted runners executing the verification runner MUST be placed in a dedicated runner group whose repository-access policy is restricted to this repository and the private builddeps repository only. High-privilege runners (those with access to `BUILDDEPS_TOKEN` or proprietary game files) MUST be segregated from general build/test runners (linting, static analysis, unit tests without secrets). Runner-group membership MUST be audited as a `runner.group.change` audit event (CHK045) and reviewed at least quarterly (CHK053). Runner labels used for routing MUST NOT be predictable/guessable by untrusted parties; group-based access control (not label-based routing alone) MUST be the primary segregation mechanism.
>
> **(b) Non-root execution (CHK037):** The runner MUST refuse to start if the effective user is root/admin (fail fast, exit code 14 — usage error). The game process, SMAPI, and verification mod MUST execute under an unprivileged account with no sudo capability. Where the OS supports it, the game install directory MUST be mounted read-only. Unnecessary capabilities MUST be dropped (Linux: `cap_drop: ALL` plus explicit `cap_add` only where required; no `--privileged` mode in any containerized execution).
>
> **(c) Ephemeral runner preference (CHK037/CHK038):** Production CI SHOULD use ephemeral (just-in-time) self-hosted runners (`--ephemeral` flag or ARC JIT config) that are destroyed after a single job. Persistent runners are permitted for local development but MUST NOT be used for CI jobs that access `BUILDDEPS_TOKEN`. This is a SHOULD (not MUST) to accommodate hosts without autoscaling infrastructure; the upgrade path is documented in the governance annex.
>
> **(d) Default GITHUB_TOKEN read-only (CHK039):** All GitHub Actions workflows in this repository (including the dormant `Build and Test`) MUST declare `permissions: contents: read` at the top level. Elevated permissions MUST be granted only at the job level where required and documented. The default `GITHUB_TOKEN` MUST never have write access unless a specific step requires it and is recorded in the workflow's permission justification comment.
>
> **(e) Network egress restriction (CHK038 complementary):** The runner host SHOULD restrict outbound network access to: GitHub API (`api.github.com`, `github.com`), the private builddeps repository, and package registries required by the build. Cloud provider metadata endpoints (e.g., `169.254.169.254`) MUST be blocked. This is a SHOULD for local development and a MUST for production CI.
>
> **(f) Secret isolation (CHK038 complementary):** `BUILDDEPS_TOKEN` MUST be stored as a GitHub Actions repository secret (not an organization secret unless the org-level secret is scoped to this repo only). The token MUST NOT be passed as a command-line argument (visible via `ps`). It MUST be injected via environment variable only. The runner MUST NOT log the token or any derived value (FR-024). Cross-job secret leakage MUST be mitigated by ephemeral runners (clause c) or, for persistent runners, by ensuring no two jobs with different privilege levels share the same runner.

### Recommended NFR (add to spec 010)

> **NFR-005 (Runner-Group Segregation Auditability):** The verification runner MUST produce evidence (in the evidence bundle, CHK031) recording: (i) the runner group name and ID used for the run, (ii) the effective user (UID) of the game process, (iii) whether the runner was ephemeral or persistent, and (iv) the GITHUB_TOKEN permission set used. This evidence is part of the MIRROR bundle's `environment` block (FR-014) and is verified by the offline verifier (FR-015).

### Recommended Governance Annex (add to VERIFICATION_PROTOCOL.md or new file)

> **Governance Annex A — Runner-Group Setup Checklist (CHK038)**
>
> The following steps MUST be performed by the maintainer (not automatable from this repo) and recorded in the evidence bundle:
>
> 1. [ ] Create a dedicated runner group in the organization settings (GitHub → Settings → Actions → Runner groups → New runner group). Name: `living-roots-verification` (or equivalent).
> 2. [ ] Set repository access policy: allow only `lfgranja/stardew-raizes-vivas` and `lfgranja/stardew-raizes-vivas-builddeps`.
> 3. [ ] Register the self-hosted runner with `--ephemeral` flag (or configure ARC with JIT). Move the runner into the dedicated group.
> 4. [ ] Create an unprivileged OS user (`runner` or `livingroots`) with no sudo capability. Configure the runner service to execute as this user.
> 5. [ ] Mount the game install directory read-only where the OS supports it (Linux: `mount -o remount,ro` or bind-mount with `ro`).
> 6. [ ] Configure host firewall: block outbound to all except GitHub API, builddeps repo, and required package registries. Block cloud metadata endpoints.
> 7. [ ] Verify the runner does not run as root: `ps -o user= -p $(pgrep -f "Runner.Listener")` must show the unprivileged user.
> 8. [ ] Document the runner group configuration in `VERIFICATION_PROTOCOL.md` with date and maintainer sign-off.
> 9. [ ] Review quarterly: re-verify steps 1–7 and record the review date.

---

## 6. Threat Mapping (CHK051 alignment)

| CHK | Threat (OWASP CICD-SEC) | SLSA Threat | Mitigation (FR-027 clause) |
|-----|------------------------|-------------|---------------------------|
| CHK037 | CICD-SEC-7 (insecure system config) | (E) Build platform compromise | (b) non-root execution |
| CHK038 | CICD-SEC-5 (PBAC), CICD-SEC-2 (IAM) | (F) Build platform impersonation | (a) runner-group segregation, (e) network egress, (f) secret isolation |
| CHK039 | CICD-SEC-2 (IAM) | (A) Source code modification | (d) default GITHUB_TOKEN read-only |

---

## 7. Ownership & Review Cadence (CHK052/CHK053 alignment)

- **Owner:** `lfgranja` (repo owner / sole maintainer).
- **Review cadence:** Quarterly (aligns with CHK053). Review trigger: any OWASP/SLSA/GitHub security advisory affecting the stack, or any change to the runner infrastructure.
- **Escalation:** If the maintainer cannot perform the governance annex steps (e.g., no org-level runner group access), the FR clauses that depend on infrastructure (a, c, e) are downgraded from MUST to SHOULD and recorded as a deviation in the evidence bundle.

---

## 8. In-Scope Determination

| Aspect | In-scope for spec 010? | Rationale |
|--------|----------------------|-----------|
| Normative requirement wording (FR-027) | **Yes** | Security requirement of the verification runner's CI surface; consistent with FR-002 (token scoping), FR-023 (local/CI parity), FR-024 (secret logging). |
| Workflow YAML change (CHK039) | **Yes** | Code-level change to `.github/workflows/`. |
| Code-level non-root check (CHK037) | **Yes** | Runner script + mod code change. |
| GitHub runner group creation | **No — Governance** | Infrastructure action outside the repo. Documented in Governance Annex A. |
| Host OS hardening (non-root user, firewall) | **No — Governance** | OS-level configuration. Documented in Governance Annex A. |
| Ephemeral runner setup | **No — Governance** | Infrastructure action. Documented as SHOULD with upgrade path. |
| Quarterly review execution | **No — Governance** | Process discipline. Documented in §7. |

**Conclusion:** The *requirement* is in-scope for spec 010. The *realization* is governance. This is the correct split: the spec defines the contract; the governance annex documents the manual steps; the evidence bundle records compliance.

---

## 9. References

### Standards & frameworks
- GitHub Actions secure-use reference (self-hosted runner hardening, runner groups, secret leakage): https://docs.github.com/en/actions/reference/security/secure-use
- OWASP GitHub Actions Security Cheat Sheet (runner segregation): https://cheatsheetseries.owasp.org/cheatsheets/GitHub_Actions_Security_Cheat_Sheet.html
- OWASP CI/CD Security Cheat Sheet (least privilege, isolation): https://cheatsheetseries.owasp.org/cheatsheets/CI_CD_Security_Cheat_Sheet.html
- OWASP Top 10 CI/CD Security Risks (CICD-SEC-2, -5, -6, -7): https://owasp.org/www-project-top-10-ci-cd-security-risks/
- NIST SP 800-204D (CI/CD pipeline security, execution environment hardening): https://csrc.nist.gov/pubs/sp/800/204/d/final
- CISA/NSA "Defending CI/CD Environments" CSI (June 2023): https://media.defense.gov/2023/Jun/28/2003249466/-1/-1/0/CSI_DEFENDING_CI_CD_ENVIRONMENTS.PDF
- SLSA v1.0 (build track, provenance, threats): https://slsa.dev/spec/v1.0/
- CIS Controls v8 (4.x secure configuration, 8.x audit log management)
- NIST SP 800-92 (log management, retention)

### GitHub documentation
- Managing access to self-hosted runners using groups: https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access
- Runner groups (concept): https://docs.github.com/en/actions/concepts/runners/runner-groups
- Actions Runner Controller (ephemeral/JIT): https://docs.github.com/en/actions/concepts/runners/actions-runner-controller
- Just-in-time self-hosted runners (changelog): https://github.blog/changelog/2023-06-02-github-actions-just-in-time-self-hosted-runners
- Self-hosted runners reference (ephemeral): https://docs.github.com/en/actions/reference/runners/self-hosted-runners

### Cross-platform CI runner security
- GitLab Runner security (non-root, capability dropping, network segmentation): https://docs.github.com/runner/security/ → https://docs.gitlab.com/runner/security/

### Industry practitioner guidance
- AWS DevOps Blog — self-hosted runners at scale (runner groups, dedicated accounts, OIDC): https://aws.amazon.com/blogs/devops/best-practices-working-with-self-hosted-github-action-runners-at-scale-on-aws/
- Equinor AppSec — GitHub Actions self-hosted runners (threat model, ephemeral, least privilege): https://equinor.github.io/appsec/toolbox/version-control/gh-actions-runners/
- DEV Community — self-hosted runners as shared secrets (label guessing, state retention, cache poisoning): https://dev.to/kozhevniko/self-hosted-ci-runners-are-shared-secrets-threat-modelling-your-build-infrastructure-2ji3
- Medium — regulated-environment DevSecOps (runner groups: build/deploy-stg/deploy-prd separation): https://medium.com/@kmoussa/building-an-ai-assisted-devsecops-pipeline-for-a-highly-regulated-on-prem-environment-the-full-c620a5480f15

### Project-internal
- `specs/010-headless-verification-runner/checklists/security.md` (CHK037–CHK039)
- `specs/010-headless-verification-runner/spec.md` (FR-002, FR-023, FR-024, NFR-004)
- `research/security-gaps-security-and-supply-chain-best-practices.md` (§4.3 CHK038 preliminary recommendation)
- `research/security-supply-chain-headless-verification-runner.md` (CHK036–CHK039 least privilege mapping)
- `AGENTS.md` (CI dormancy, Gate 3 verification contract)
- `VERIFICATION_PROTOCOL.md` (§3 manual protocol, Gate 3 contract)

---

*End of research deliverable. No code edited; no commit made. Companion to `research/security-gaps-security-and-supply-chain-best-practices.md` (§4.3). Closes checklist gap CHK038 (recommendation; spec owner applies via FR-027 + Governance Annex A).*
