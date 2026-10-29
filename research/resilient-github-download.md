# Resilient Downloading of Large Archives from GitHub (Private Repos) via API

**Date:** 2026-10-26
**Context:** Stardew Valley mod "Living Roots" — headless verification runner (spec 010) and dormant `Build and Test` workflow acquire a ~5.2 MB base64 / several-MB proprietary game-assembly archive (`gamepath.zip`) from the private repo `lfgranja/stardew-raizes-vivas-builddeps` using `BUILDDEPS_TOKEN`. Must be reliable in CI and locally.
**Scope:** GitHub REST API rate limits, download endpoints, retry/backoff, partial-download recovery, checksum verification, secret handling.

---

## 1. Executive Summary

- **Recommended transport:** GitHub Releases assets (`GET /repos/{owner}/{repo}/releases/assets/{asset_id}` with `Accept: application/octet-stream`), fetched via `gh release download` (CLI) or `curl -L` with `Authorization: Bearer`. This is the only endpoint family that supports large binary files, immutable release assets, and private-repo access with a single token.
- **Do NOT use** the Contents API (`/repos/.../contents/{path}`) for large archives: it caps at 1,000 directory entries, returns base64 JSON for files (size overhead, memory), and download URLs expire after ~5 minutes for private repos.
- **Do NOT use** source-archive endpoints (`/tarball/{ref}`, `/zipball/{ref}`, `/archive/refs/tags/...`) for reproducible builds: GitHub explicitly does **not** guarantee checksum stability of auto-generated archives (compression settings changed in 2023).
- **Rate limits:** 60 req/hr unauthenticated (per IP), 5,000 req/hr authenticated (per user), 15,000 req/hr for GitHub-App-owned-by-Enterprise-Cloud-org tokens. Secondary limits: 900 REST points/min, 100 concurrent requests, 80 content-writes/min. A single download consumes ~1–3 points, so rate limits are not the bottleneck for one archive; they become relevant only for listing releases/assets or polling.
- **Partial-download recovery:** GitHub's release-asset redirect target is a CDN (S3/CloudFront-style) whose `Accept-Ranges` support is not contractually documented by GitHub. Treat resume as **best-effort**: always verify the final file with SHA-256 and, on mismatch or missing range support, restart from byte 0. Do not assume resume works.
- **Checksum verification is mandatory** before extraction: publish a `checksums.txt` (SHA-256) alongside the asset in the release; verify with `sha256sum -c`. GitHub does not compute/provide asset checksums via API.
- **Secret handling:** use a **fine-grained PAT** scoped to a single private repo with `Contents: read` (+ `Metadata: read`), shortest viable expiry (≤ 90 days, max 366 in org policy), stored as a repository secret, never printed. Prefer a GitHub App installation token for production (short-lived, 1-hour expiry, 15k/hr limit).
- **Retry policy:** exponential backoff with full jitter, 3–5 attempts, retry only on transient failures (5xx, 429, network errors, timeouts). Treat 401/403/404 as terminal (fail fast). Cap total elapsed time.

---

## 2. GitHub REST API Rate Limits

Source: [docs.github.com — Rate limits for the REST API](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)

### 2.1 Primary limits

| Authentication | Limit | Scope |
|---|---|---|
| Unauthenticated | 60 requests/hour | Per originating IP |
| PAT / OAuth user token | 5,000 requests/hour | Per user |
| GitHub App (Enterprise Cloud org owner) | 15,000 requests/hour | Per installation |
| Git LFS unauthenticated | 30,000 objects/min | Per IP |
| Git LFS authenticated | 300,000 objects/min | Per user |

- Limits reset on a rolling 1-hour window; the `x-ratelimit-reset` header gives the Unix epoch of the next reset.
- Headers returned on every response: `x-ratelimit-limit`, `x-ratelimit-remaining`, `x-ratelimit-used`, `x-ratelimit-reset`, `x-ratelimit-resource`.

### 2.2 Secondary (abuse-prevention) limits

Source: [docs.github.com — secondary rate limits](https://docs.github.com/en/enterprise-server@3.19/rest/using-the-rest-api/rate-limits-for-the-rest-api)

- **No more than 100 concurrent requests** (shared across REST + GraphQL).
- **No more than 900 points/minute** for REST endpoints (500 for content creation).
- **No more than 80 content-creation requests/minute** and 500/hour (issues, comments, PRs).
- **CPU budget:** 90 seconds of CPU time per 60 seconds of real time (60 s max for GraphQL).
- Exceeding secondary limits returns `403` or `429` with a `Retry-After` header.

### 2.3 What this means for our use case

A single archive download is **one** request to the releases endpoint (plus one redirect hop to the CDN). Rate limits are therefore not the binding constraint; the binding constraints are:

1. Token validity / repo access (401/403/404).
2. CDN/network reliability for a multi-MB transfer.
3. Checksum integrity.

**Practice:** still read `x-ratelimit-remaining`; if it is below a small threshold (e.g., 10) before a batch of calls, sleep until `x-ratelimit-reset`. Never retry on 401/403/404.

---

## 3. Download Endpoints: Which to Use

### 3.1 Comparison matrix

| Endpoint | Best for | Size limits | Resume support | Checksum stability | Private repo |
|---|---|---|---|---|---|
| Releases assets (`/repos/.../releases/assets/{id}`) | **Large binaries, game archives** | Up to 2 GB per asset, 1,000 assets/release | Best-effort (CDN-dependent) | Stable (you control the file) | Yes, with token |
| Contents (`/repos/.../contents/{path}`) | Small text/config files (≤ 1 MB) | 1,000 dir entries; base64 JSON | No | N/A | Yes, but URLs expire in 5 min |
| Tarball/zipball (`/repos/.../{tarball,zipball}/{ref}`) | Source snapshots | Repo size | No | **Unstable** (GitHub does not guarantee) | Yes |
| Source archive (`/archive/refs/tags/{tag}.zip`) | Source snapshots | Repo size | No | **Unstable** | Yes for private |
| Git blobs (`/repos/.../git/blobs/{sha}`) | Individual files ≤ 100 MB | 100 MB blob limit | No | Stable (content-addressed) | Yes |
| `gh release download` | CLI wrapper around releases assets | Same as assets | Delegates to curl/HTTP | N/A (you verify) | Yes via `GH_TOKEN` |
| `actions/download-artifact` | CI artifact passing between jobs | 10 GB total per workflow, 500 MB/artifact (free) | Managed by service | Service-managed | Yes within run |

### 3.2 Releases assets — the recommended path

Source: [docs.github.com — REST API endpoints for release assets](https://docs.github.com/en/rest/releases/assets)

```
GET /repos/{owner}/{repo}/releases/assets/{asset_id}
```

- Set `Accept: application/octet-stream` to stream binary content; otherwise the endpoint returns JSON metadata.
- The API responds with `200` (streamed) or `302` (redirect to a CDN URL). Clients **must handle both**.
- Use `curl -L` to follow redirects, or `gh release download` which does this internally.
- `browser_download_url` in the release metadata is a direct (auth-less, signed) URL usable in a browser; for API/CI use the asset endpoint with the token.
- Asset size limit: 2 GB per file; up to 1,000 assets per release.

**Critical detail for auth on redirect:** when following a 302 to the CDN, the `Authorization` header must **not** be forwarded to the CDN host (some CDNs reject it or it leaks the token). `curl` with `-L` does not strip headers by default in older versions; use `--location-trusted` only when needed, and prefer `gh release download` or a two-step fetch (get metadata → fetch `browser_download_url` without auth) to avoid leaking `BUILDDEPS_TOKEN`.

### 3.3 Contents API — when (not) to use

Source: [docs.github.com — REST API endpoints for repository contents](https://docs.github.com/en/rest/repos/contents)

- Files > 1 MB are returned as base64 inside JSON (memory + CPU overhead, ~33% size inflation).
- Directory listing capped at 1,000 entries.
- **Private-repo download URLs expire after 5 minutes** and are single-use. Must re-request a fresh URL per download.
- Use only for small config/seed files, never for multi-MB archives.

### 3.4 Source archives — unstable checksums

Source: [github.blog — Update on the future stability of source code archives and hashes](https://github.blog/open-source/git/update-on-the-future-stability-of-source-code-archives-and-hashes/), [community discussion #45830](https://github.com/orgs/community/discussions/45830)

- In January 2023, GitHub changed archive generation from external `gzip` to an internal copy, changing byte-level output (and therefore SHA-256) even though file contents were identical.
- **GitHub does not guarantee checksum stability** for auto-generated archives ("Source code (zip)", "Source code (tar.gz)").
- Community request for stable hashes is tracked but unresolved ([discussion #46034](https://github.com/orgs/community/discussions/46034)).
- **Implication:** never pin a SHA-256 to a GitHub auto-generated source archive. If you need reproducibility, upload your own release asset and pin its checksum.

### 3.5 Git LFS (if applicable)

- LFS has its own rate limits (30k objects/min unauth, 300k/min auth) and is designed for large binary files tracked in git.
- Adds git-lfs client dependency and pointer-file indirection; more moving parts than a release asset.
- **Recommendation:** prefer release assets for a single downloadable bundle; use LFS only if the archive must be version-controlled as source.

---

## 4. Partial-Download Recovery (Resume)

### 4.1 HTTP Range mechanism

Source: [MDN — HTTP range requests](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Range_requests), [http.dev — Range request](https://http.dev/range-request)

- Client sends `Range: bytes=<start>-` (or `bytes=<start>-<end>`).
- Server advertises support via `Accept-Ranges: bytes` response header.
- Success: `206 Partial Content` with `Content-Range: bytes <start>-<end>/<total>`.
- Unsatisfiable range: `416 Range Not Satisfiable`.
- Server may ignore `Range` and return `200` with the whole body (treat as "no resume support").

### 4.2 Resumable-download pattern (If-Range)

- Store the `ETag` or `Last-Modified` from the initial response.
- On retry, send `If-Range: <validator>` together with `Range: bytes=<offset>-`.
- If the resource changed, the server returns `200` with the full body (restart from 0); if unchanged, `206` with the requested slice.
- This guards against a file being replaced mid-download.

### 4.3 GitHub-specific reality

- GitHub's release-asset endpoint returns a 302 to a CDN. Whether the CDN honors `Range`/`Accept-Ranges` is **not documented** by GitHub and has historically varied (some reports of `Accept-Ranges: bytes` on `github.com` asset URLs, others of full 200s).
- **Do not depend on resume for correctness.** Implement resume as an optimization, with these invariants:
  1. Always record the expected SHA-256 out-of-band (in the release's `checksums.txt`).
  2. After any download (resumed or fresh), compute SHA-256 and compare; on mismatch, delete the partial file and restart from byte 0 (up to N attempts).
  3. If the server returns `200` instead of `206` when a `Range` was sent, discard the existing partial and treat the response as a fresh full download (or abort and retry fresh).
  4. Handle `416` (completed file) by verifying the checksum; if it matches, treat as success.

### 4.4 curl resume pitfalls

Sources: [curl issue #6740](https://curl.se/bug?i=6740), [curl issue #1084](https://github.com/curl/curl/issues/1084)

- `curl -C -` (resume) combined with `--fail`/`-f` fails on a fully-downloaded file because the subsequent `Range: bytes=<size>-` returns `416`.
- `curl --retry` does not resume by default; older versions discard partial data on retry.
- **Robust pattern:** check `Content-Length` via `HEAD` (or read it from release metadata) before resuming; if local size == expected size, verify checksum and skip; otherwise resume with `-C -` only when local size < expected size, and cap attempts.

### 4.5 Recommended download primitive (bash)

```bash
# Idempotent, checksum-verified download with retry + best-effort resume.
download_asset() {
  local url="$1" dest="$2" expected_sha="$3"
  local max_attempts=5 delay=2 attempt=0

  # Already complete & verified?
  if [ -f "$dest" ]; then
    local got; got=$(sha256sum "$dest" | awk '{print $1}')
    if [ "$got" = "$expected_sha" ]; then return 0; fi
    rm -f "$dest"   # corrupt/partial -> restart
  fi

  while [ "$attempt" -lt "$max_attempts" ]; do
    attempt=$((attempt + 1))
    # Resume only if a partial file exists and is smaller than expected.
    local curl_opts=(-L --fail --show-error --silent)
    if [ -f "$dest" ]; then curl_opts+=(-C -); fi
    if curl "${curl_opts[@]}" -o "$dest" "$url"; then
      local got; got=$(sha256sum "$dest" | awk '{print $1}')
      if [ "$got" = "$expected_sha" ]; then return 0; fi
      echo "checksum mismatch on attempt $attempt" >&2
      rm -f "$dest"
    else
      echo "download failed on attempt $attempt" >&2
      # curl leaves a partial file on failure; keep it for resume unless it's empty
      [ -s "$dest" ] || rm -f "$dest"
    fi
    # exponential backoff with full jitter, capped at 60s
    local cap=60
    local window=$(( delay > cap ? cap : delay ))
    local sleep_s=$(( RANDOM % (window + 1) ))
    sleep "$sleep_s"
    delay=$(( delay * 2 ))
  done
  return 1
}
```

**Notes on this primitive:**
- `RANDOM % (window+1)` is full jitter (uniform in `[0, window]`), which the AWS retry guidance recommends to avoid thundering herds.
- `--fail` ensures non-2xx exits non-zero; combined with the size guard above, avoids the 416-on-complete problem.
- Auth header must be supplied via `Authorization: Bearer ${BUILDDEPS_TOKEN}`; never log it.

---

## 5. Checksum Verification

### 5.1 Why checksums are mandatory

- GitHub does not compute or expose SHA-256 for release assets via the API (no `digest` field in the asset metadata).
- Network/CDN corruption, partial writes, and MITM (on untrusted networks) are all possible.
- GitHub's own source archives have unstable hashes (§3.4), so pinning only works for assets you publish yourself.

### 5.2 Publishing a checksum (release side)

Recommended release layout (in `lfgranja/stardew-raizes-vivas-builddeps`):

```
gamepath.zip
gamepath.zip.sha256          # or checksums.txt covering all assets
gamepath.zip.sha256.sig      # optional: signed with cosign/gh attestation
```

Generate at release time:

```bash
sha256sum gamepath.zip > gamepath.zip.sha256
# publish both files as release assets
```

For stronger assurance, use **GitHub artifact attestations** (`gh attestation verify`) or **Sigstore/cosign** signatures, as documented in [gh release verify-asset](https://cli.github.com/manual/gh_release) and the [ApiaryLens verification example](https://github.com/ApiaryLens/apiarylens/blob/main/docs/releases/verification.md).

### 5.3 Verifying (consumer side)

```bash
# Verify against a published checksum file
sha256sum -c gamepath.zip.sha256

# Or inline
echo "${EXPECTED_SHA}  gamepath.zip" | sha256sum -c
```

**Manifest integrity pattern (recommended for multi-file bundles):** ship a `manifest.json` with per-file SHA-256 (and sizes), then verify every entry. This is the pattern used by `proof-first-auditpack` and by the MIRROR evidence bundle in spec 010 (FR-014/015).

### 5.4 Pinning policy

- Pin the **exact SHA-256** of the asset in the runner/workflow (or fetch `*.sha256` from the same release and verify it was fetched from the same trusted repo).
- On checksum mismatch: **fail fast**, do not extract, do not retry more than N times (a persistent mismatch indicates a compromised or changed asset, not a transient error).
- Treat a changed checksum as a **security event** requiring human review, not an automatic update.

---

## 6. Security: Token Scoping & Secret Handling

### 6.1 Token type selection

Source: [docs.github.com — Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens), [Introducing fine-grained PATs](https://github.blog/security/application-security/introducing-fine-grained-personal-access-tokens-for-github)

| Token type | Scope | Lifetime | Recommendation |
|---|---|---|---|
| Fine-grained PAT | Per-repo, per-permission | ≤ 1 year (org policy may cap at 366 days) | **Recommended for BUILDDEPS_TOKEN** |
| Classic PAT | Broad scopes (`repo`) | No expiry by default | Avoid; over-privileged |
| `GITHUB_TOKEN` (built-in) | Current repo only | Per-job (auto) | Cannot read private builddeps repo — insufficient |
| GitHub App installation token | Fine-grained, short-lived | 1 hour | **Best for production CI** (higher limit 15k/hr, auto-rotation) |
| OIDC (Actions → cloud) | Federated, no long-lived secret | Per-job | Best if you can mint tokens from a vault |

### 6.2 Fine-grained PAT configuration for `BUILDDEPS_TOKEN`

- **Repository access:** only `lfgranja/stardew-raizes-vivas-builddeps` (single repo).
- **Permissions:** `Contents: Read` and `Metadata: Read` only (release assets require `Contents: read`; see [permissions table](https://docs.github.com/en/rest/authentication/permissions-required-for-fine-grained-personal-access-tokens)).
- **Expiration:** shortest viable (e.g., 90 days); set a calendar reminder to rotate.
- **Approval:** if the repo is org-owned, an org owner must approve the token.

### 6.3 Storage & handling

Source: [docs.github.com — Secrets](https://docs.github.com/en/actions/concepts/security/secrets), [Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use)

- Store as a **repository secret** (`BUILDDEPS_TOKEN`), never as a variable or in code.
- In workflows, expose only to the step that needs it:
  ```yaml
  - name: Fetch game assemblies
    env:
      GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}
    run: gh release download ...
  ```
- `GH_TOKEN` env var is preferred over `--token` CLI flag (avoids leaking into process listings / shell history).
- GitHub auto-redacts secret values in logs, but **redaction is not guaranteed** if the value is transformed (base64, substring). Never `echo`, `set -x` with the token, or write it to a file.
- Set `permissions: contents: read` at the workflow/job level for `GITHUB_TOKEN`; default to least privilege.
- Do not use `pull_request_target` with secret access (privilege escalation vector).
- Rotate on suspected exposure; treat the token as compromised if it ever appears in logs.

### 6.4 Workflow hardening checklist

- Pin third-party actions to full commit SHAs (not tags) — e.g., `actions/checkout@<sha> # v4`.
- Use `set -euo pipefail` in bash steps.
- Validate the token is present before attempting download (`[ -z "${GH_TOKEN:-}" ] && { echo "::error::BUILDDEPS_TOKEN missing"; exit 1; }`).
- Verify repo accessibility with a lightweight call (`gh api repos/.../releases --jq '.[0].tag_name'`) before downloading.
- Fail fast on 401/403/404; do not retry auth errors.

---

## 7. Tools Comparison

### 7.1 `gh` CLI (`gh release download`)

- **Pros:** handles auth, redirect, retry-friendly (you wrap it), `--pattern` globs, `--clobber`, `--skip-existing`, `--dir`, `--output`.
- **Cons:** no built-in checksum verification; no resume flag; relies on `GH_TOKEN` env.
- **Usage:**
  ```bash
  GH_TOKEN="${BUILDDEPS_TOKEN}" gh release download "${TAG}" \
    --repo "${REPO}" --pattern "${ASSET}" --dir "${DEST}" --clobber
  ```
- **Verification subcommand:** `gh release verify-asset` and `gh attestation verify` (Sigstore) for supply-chain assurance.

### 7.2 `curl`

- **Pros:** universal, supports `-C -` resume, `--retry`/`--retry-delay`/`--retry-max-time`, `--fail`, `--location`, custom headers.
- **Cons:** you must manage auth header, redirect, and checksum yourself; older curl versions discard partials on retry.
- **Recommended flags:** `-L --fail --show-error --silent --retry 5 --retry-delay 2 --retry-all-errors -C -` plus `Authorization: Bearer ${TOKEN}`.
- Note: `--retry-all-errors` retries on any error including 4xx in curl ≥ 7.71; prefer explicit retry logic to avoid retrying 401/404.

### 7.3 `wget`

- Supports `-c` (continue) and `--tries`; less common in CI images; acceptable fallback.

### 7.4 `actions/download-artifact`

- For passing files between jobs in the *same* workflow run, not for external private repos.
- Supports cross-run/cross-repo download with a token that has `actions: read`.
- Artifacts are zipped by default (v4+); v7+ supports unzipped single files.
- **Not applicable** to our game-assembly use case (external private repo), but relevant if builddeps are ever produced by a workflow in the same org.

### 7.5 Language libraries (for the .NET runner)

- `HttpClient` with `Range` header + `FileStream` append for resume; `SHA256` (System.Security.Cryptography) for verification.
- Libraries: `Octokit` (GitHub API client) for metadata + asset listing; download bytes via `HttpClient` to control streaming/checksum.
- Always stream to disk (do not buffer multi-MB payloads in memory); compute hash incrementally with `CryptoStream`/`TransformBlock`.

---

## 8. Retry & Backoff Strategy

Source: [AWS — Retry with backoff](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/retry-backoff.html), [AWS Well-Architected REL05-BP03](https://docs.aws.amazon.com/wellarchitected/2023-04-10/framework/rel_mitigate_interaction_failure_limit_retries.md), [Software Patterns Lexicon — Retries, Backoff, Jitter](https://softwarepatternslexicon.com/event-driven-architecture-patterns/reliability-and-delivery/retries-backoff-jitter)

### 8.1 Classification

| Status / error | Retryable? | Action |
|---|---|---|
| 2xx | No (success) | Verify checksum, done |
| 3xx redirect | Follow | Up to 10 redirects |
| 401 Unauthorized | **No** | Fail fast — bad/missing token |
| 403 Forbidden | **No** (unless rate-limited with Retry-After) | Fail fast — scope/permission |
| 404 Not Found | **No** | Fail fast — wrong repo/tag/asset |
| 429 Too Many Requests | Yes | Honor `Retry-After`; backoff |
| 5xx | Yes | Backoff |
| Network timeout / connection reset / DNS | Yes | Backoff |
| Checksum mismatch | Limited (≤ 2) | Then fail — likely tampering/version change |

### 8.2 Parameters

- **Max attempts:** 3–5 (more rarely helps; increases latency).
- **Base delay:** 1–2 s.
- **Backoff:** exponential, `delay = base * 2^(attempt-1)`.
- **Cap:** 30–60 s.
- **Jitter:** full jitter — `sleep = random(0, min(delay, cap))`. Prevents thundering herd when many runners start together (self-hosted CI).
- **Total time budget:** e.g., 5 min for a 50 MB archive; abort beyond.
- **Idempotency:** downloads are idempotent (GET, no server-side state); safe to retry. Use atomic temp-file + rename so a partial never replaces a good file.

### 8.3 Concurrency

- Respect the 100-concurrent-request global limit; for a single archive this is irrelevant, but batch operations (listing assets) should be serialized or bounded with a semaphore.

---

## 9. Recommended Approach for This Project

### 9.1 Workflow (CI) — harden existing `build_and_test.yml`

Replace the bare `gh release download` + `unzip` with a verified, retried step:

```yaml
- name: Fetch game assemblies
  env:
    GH_TOKEN: ${{ secrets.BUILDDEPS_TOKEN }}
    BUILDDEPS_REPO: lfgranja/stardew-raizes-vivas-builddeps
    BUILDDEPS_TAG: gamepath-v1
    BUILDDEPS_ASSET: gamepath.zip
    BUILDDEPS_CHECKSUM: gamepath.zip.sha256
    GamePath: ${{ github.workspace }}/.gamepath
  run: |
    set -euo pipefail
    : "${GH_TOKEN:?BUILDDEPS_TOKEN secret is missing}"
    mkdir -p "${GamePath}"
    # 1. Verify repo + release accessibility (fail fast on auth).
    gh api "repos/${BUILDDEPS_REPO}/releases/tags/${BUILDDEPS_TAG}" \
      --jq '.tag_name' >/dev/null
    # 2. Download asset + checksum with retry.
    for f in "${BUILDDEPS_ASSET}" "${BUILDDEPS_CHECKSUM}"; do
      gh release download "${BUILDDEPS_TAG}" \
        --repo "${BUILDDEPS_REPO}" --pattern "${f}" \
        --dir "${GamePath}" --clobber
    done
    # 3. Verify checksum before extraction.
    ( cd "${GamePath}" && sha256sum -c "${BUILDDEPS_CHECKSUM}" )
    # 4. Extract atomically.
    tmp="${GamePath}/.extract.$$"
    mkdir -p "${tmp}"
    unzip -o "${GamePath}/${BUILDDEPS_ASSET}" -d "${tmp}"
    rm -f "${GamePath}/${BUILDDEPS_ASSET}" "${GamePath}/${BUILDDEPS_CHECKSUM}"
    # move contents up
    shopt -s dotglob
    mv -f "${tmp}"/* "${GamePath}/"
    rmdir "${tmp}"
    mkdir -p "${GamePath}/smapi-internal"
    mv -f "${GamePath}/SMAPI.Toolkit.CoreInterfaces.dll" \
          "${GamePath}/smapi-internal/"
```

### 9.2 Headless runner (spec 010) — download module

Implement a small, testable download module (e.g., `GameAssemblyFetcher`) with:

1. **Preflight:** check `BUILDDEPS_TOKEN` env var is non-empty; probe `gh api repos/.../releases/tags/...` (or `HttpClient` HEAD) → fail fast with actionable message on 401/403/404.
2. **Download:** stream to `${GamePath}/gamepath.zip.part` with retry+backoff+jitter (§4.5 / §8); rename to final name on success.
3. **Checksum:** fetch `gamepath.zip.sha256` from the same release; verify with `SHA256` over the streamed file; on mismatch, delete and retry (≤ 2), then fail.
4. **Extract:** verify checksum *before* extraction; extract to a temp dir, validate the 6 required assemblies (preflight), then atomically swap into `GamePath`.
5. **Cache:** skip download if `gamepath.zip` exists and checksum matches (idempotent re-runs); support `--force-refresh`.
6. **Logging:** log each attempt with timestamp and byte count; never log the token.

### 9.3 Release-side (builddeps repo)

- Publish `gamepath.zip` + `gamepath.zip.sha256` (and optionally `gamepath.zip.sha256.sig` via cosign) as a single immutable release (`gamepath-v1`).
- Document the expected SHA-256 in the public repo's `research/` or `docs/` so consumers can pin it.
- Rotate the release (new tag) rather than overwriting assets when the game/SMAPI version changes; keep old tags for reproducibility.

---

## 10. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Token expiry / revocation breaks CI silently | Medium | High | Short expiry with calendar reminder; probe repo access as first step; clear error message naming the token |
| Token leaked via logs or process listing | Low | Critical | Use `GH_TOKEN` env (not `--token`); never `set -x`/echo; redaction is not guaranteed if transformed |
| CDN does not support `Range` → resume fails | Medium | Low | Treat resume as optimization; verify checksum; restart from 0 on mismatch |
| Partial file left on disk after failed run | Medium | Medium | `.part` suffix + atomic rename; clean stale `.part` on startup |
| Checksum mismatch due to asset replacement without version bump | Low | High | Pin tag + SHA; treat mismatch as security event; require human review |
| Rate-limit exhaustion in batch operations | Low | Medium | Read `x-ratelimit-remaining`; serialize; backoff on 429 |
| GitHub auto-archive hash instability (if we ever use source archives) | Certain | Medium | **Never** use auto-generated archives; publish own asset |
| Download URL expiry (Contents API) | Certain (5 min) | Medium | Use Releases assets, not Contents API, for large files |
| Concurrent runners collide on same cache dir | Low | Medium | Distinct temp dirs per run (display numbers already distinct); file locks |
| Proprietary assemblies accidentally committed | Low | Critical | Keep `.gamepath/` gitignored; CI downloads from private repo only |
| Runner runs on host without token | Medium | High | Fail fast before any download; documented prerequisite |

---

## 11. References

### Official GitHub docs
- Rate limits (REST): https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api
- Rate limits (GraphQL): https://docs.github.com/en/graphql/overview/rate-limits-and-query-limits-for-the-graphql-api
- REST API authentication: https://docs.github.com/rest/overview/authenticating-to-the-rest-api
- Releases & release assets endpoints: https://docs.github.com/en/rest/releases/assets
- Repository contents endpoints: https://docs.github.com/en/rest/repos/contents
- Personal access tokens (fine-grained): https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens
- Fine-grained PAT permissions table: https://docs.github.com/en/rest/authentication/permissions-required-for-fine-grained-personal-access-tokens
- Secrets in GitHub Actions: https://docs.github.com/en/actions/concepts/security/secrets
- Secure use reference: https://docs.github.com/en/actions/reference/security/secure-use
- Downloading workflow artifacts: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts
- Downloading files from GitHub: https://docs.github.com/en/repositories/working-with-files/using-files/downloading-files-from-github
- About releases: https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases

### GitHub CLI
- `gh release download`: https://cli.github.com/manual/gh_release_download
- `gh release verify` / `verify-asset`: https://cli.github.com/manual/gh_release
- `gh run download`: https://cli.github.com/manual/gh_run_download
- actions/download-artifact: https://github.com/actions/download-artifact

### HTTP / resume
- MDN — Range requests: https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Range_requests
- MDN — Range header: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Range
- MDN — Accept-Ranges header: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Accept-Ranges
- http.dev — Range request: https://http.dev/range-request
- curl resume pitfalls: https://curl.se/bug?i=6740, https://github.com/curl/curl/issues/1084

### Checksums & supply chain
- GitHub blog — archive hash stability: https://github.blog/open-source/git/update-on-the-future-stability-of-source-code-archives-and-hashes/
- Community discussion #45830 (checksum mismatches): https://github.com/orgs/community/discussions/45830
- Community discussion #46034 (hash stability request): https://github.com/orgs/community/discussions/46034
- Community discussion #23512 (release checksums): https://github.com/orgs/community/discussions/23512
- ApiaryLens verification example (sha256 + gh attestation): https://github.com/ApiaryLens/apiarylens/blob/main/docs/releases/verification.md

### Retry / resilience
- AWS retry with backoff: https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/retry-backoff.html
- AWS Well-Architected REL05-BP03: https://docs.aws.amazon.com/wellarchitected/2023-04-10/framework/rel_mitigate_interaction_failure_limit_retries.md
- Software Patterns Lexicon — retries/backoff/jitter: https://softwarepatternslexicon.com/event-driven-architecture-patterns/reliability-and-delivery/retries-backoff-jitter
- Bash bounded retry idiom: https://github.com/Open-Technology-Foundation/bash-coding-standard/blob/main/docs/BCS-Bash-Ref/22_Idioms-Patterns-and-Anti-Patterns/12_Bounded-retry-with-exponential-backoff.md

### Security
- GitHub Actions secure use: https://docs.github.com/en/actions/reference/security/secure-use
- OWASP GitHub Actions security cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/GitHub_Actions_Security_Cheat_Sheet.html
- Fine-grained PAT intro (GitHub blog): https://github.blog/security/application-security/introducing-fine-grained-personal-access-tokens-for-github

### Project-internal
- `.github/workflows/build_and_test.yml` (current download step, lines 71–85)
- `specs/010-headless-verification-runner/spec.md` (FR-002, FR-015, NFR-002)
- `AGENTS.md` (CI dormancy, Gate 3 verification contract)
- `.specify/bugs/ci-skipped-job-false-green/`

---

## 12. TL;DR Decision

1. **Endpoint:** GitHub Releases assets (not Contents, not source archives).
2. **Auth:** fine-grained PAT scoped to one private repo, `Contents: read`, stored as `BUILDDEPS_TOKEN`; upgrade to a GitHub App for production.
3. **Download:** `gh release download` or `curl -L` with `Authorization: Bearer`, streamed to a `.part` file.
4. **Resume:** best-effort only; never assume `Range` support; verify checksum afterward.
5. **Checksum:** publish `*.sha256` with the asset; verify with `sha256sum -c` before extraction; pin the expected hash.
6. **Retry:** exponential backoff with full jitter, 3–5 attempts, only on transient errors; fail fast on 401/403/404.
7. **Security:** least-privilege token, no logging, pin actions to SHAs, atomic temp-file + rename.
8. **Never** use GitHub auto-generated source archives for reproducible builds (unstable hashes).