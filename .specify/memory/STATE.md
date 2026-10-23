# Session State — feat/008-smoke-validation

> ## ▶ RESUME POINT (2026-10-22, feature COMPLETE — all 184 tasks implemented and verified)
>
> **Nothing was left running** except one known leaked fixture from a killed suite
> run (pid `320074`, a stub X server under `/tmp/tmp.Veou0Gw9Md/`, ~4 h old) — it is
> another session's to deal with; **do not kill it**. The tree is committed and
> clean apart from three items owned by OTHER sessions on this host (an unrelated
> dirty file under `.specify/bugs/`, and the untracked `.fabro/` and
> `.specify/assessments/automate-ingame-testing/` directories) — do not commit,
> revert or delete those.
>
> **The feature is IMPLEMENTED and VERIFIED.** `specs/008-smoke-validation/tasks.md`
> holds **184 tasks, all marked `[X]`, 0 open** — Phases 1–7 (the original build),
> Phases 8 and 9 (the two convergence rounds' backlogs, 44 + 50 tasks) and the
> `### Follow-up` block (T184, found during implementation).
>
> **The measured verification (2026-10-22):**
> - **1 165 bash assertions, 0 failures** — CLI **255**, pre-flight **380**, log gate
>   **167**, integration **363** — all four suites in a single run.
> - **1 272 C# assertions**: 1 271 passed, 1 skipped, 0 failed. `dotnet build` 0
>   errors; `dotnet format --verify-no-changes` clean.
> - **The static shell lint is at zero findings** across all eight files in the
>   `lefthook` scope, verified with an out-of-tree `shellcheck` 0.11.0 (the binary
>   is **absent on this host**, so the hook self-skips here).
> - **A real-game run exits `0`**: `SUCCESS: Mod loaded without errors.` in 39–40 s,
>   and — for the first time — **`mod-success-marker: The 'Stardew Valley - Living
>   Roots' mod was loaded successfully!`**. That marker is emitted by the mod's own
>   `GameLaunched` handler, which SMAPI reaches only after the title screen; the
>   older script killed the game the instant the mod-resolution header appeared, so
>   it had never been observed. Its presence is what turns "the mod resolved" into
>   "the mod's own code ran and reported success". It needed the bounded,
>   non-gating 30 s marker wait (T162) and the matching FR-005 amendment.
> - **Thirteen retained run logs** under
>   `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/`.
> - The real-game run left **no orphaned processes**.
>
> **Three environment traps, all recorded in `tasks.md` and here:**
> 1. `/tmp` is a 2 GB tmpfs that other sessions fill — it reached **100 %** during
>    this session. A full tmpfs makes the X server fail to create its socket
>    (exit `10`) and the build hang, and shows up as
>    `cat: erro de escrita: Não há espaço disponível no dispositivo`; a run
>    reporting `10` or timing out at the harness deadline is an **environment
>    symptom, not a verdict**. Free `/tmp` before a verification run, and never
>    delete another session's leftovers. **`TMPDIR=/var/tmp` is the working
>    fallback** — a separate filesystem with free space, and **outside `$HOME`**,
>    so SMAPI does not `~`-anonymise the mods path.
> 2. Run the suites with a `TMPDIR` **outside `$HOME`**. A `TMPDIR` inside `$HOME`
>    moves the fixture tree under the home directory; SMAPI then
>    `~`-anonymises the mods path it prints and TC-14 fails on a correct run.
> 3. Do not run two suites that rebuild the repo tree concurrently — they
>    contend on `LivingRoots/obj/` and the deploy digest assertion can fail
>    with exit `12`. **Serialise the suite RUNS, not just the test writing.**
>    The precise map: `test-logparse.sh` and `test-integration.sh` do a real
>    `dotnet build`; `test-cli.sh` stubs `dotnet`; `test-preflight.sh` blocks
>    `dotnet` outright.

## ▶ IMPLEMENTATION STATE (2026-10-21, wave 1 done — 130 of 183 tasks complete)

**Wave 1 (the whole TDD wall) is written and committed** — 32 test tasks across the six suite
files, +4,348 lines. The wall is open; wave 2 (the implementation) is running.

| Wave | Content | Status |
|---|---|---|
| 1 (TDD) | all 32 test tasks in `scripts/tests/` | ✅ committed |
| 1 (docs) | T119, T134, T174–T180 | ✅ committed |
| 2 | 31 tasks on `scripts/smoke-validation.sh` — **sequential**, one file, split S1…S4 | 🔄 S1 running |
| 3 | the plan / data-model / quickstart / spec / tasks.md / lefthook documents — must follow wave 2, because they describe the *shipped* code | ⏳ |
| 4 | T182 (full suite + the constitution's three gates) and T183 (the real-game run) — last, alone, and the only things that attest completeness | ⏳ |

**Wave 2 groups, on the one script file, run SEQUENTIALLY:**
- **S1 — the failure report & the exit codes (7)**: T158, T168, T118, T125, T121, T124, T159
- **S2 — the pre-flight order, the bash-4 check & the inode guard (3)**: T169, T101, T122
- **S3 — the build, the deploy & the path validation (10)**: T156, T160, T163, T172, T123, T126, T100, T120, T157, T167
- **S4 — the log gate, the readiness wait & the process groups (11)**: T155, T161, T162, T170, T171, T173, T098, T099, T102, T091, T103

**Measured RED evidence:**

*Log-gate suite — 167 assertions, 160 pass, 7 fail:*
- `[graphics_device] exits 3` → got **17** — T135, genuine RED, S4/T155.
- `[graphics_device] report: mentions …` and `does not exit 17` — same cause.
- `stale lines: phase 1 was satisfied by the current run's sentinel` and
  `stale lines: phase 2 timed out …` — T151, genuine RED, S4/T173.
- `[mod_alert] exits 5` → got **12** — **NOT a gate defect**: 12 is
  `EX_BUILD_ARTIFACT_FAILURE`, i.e. the run aborted at the build phase and the
  log gate never ran. Cause: this suite run **overlapped the integration suite**,
  and both rebuild `LivingRoots/obj/` and deploy into the same mods folder, so the
  deploy digest comparison aborted. Re-verify it alone in T182 before treating it
  as a finding — the ALERT leg is already implemented (`lvl == "ERROR" || lvl == "ALERT"`).

*Pre-flight suite — 406 assertions, 400 pass, 6 fail:*
- `a host without 'sleep' fails closed with 9` → got **10**, and
  `a host without 'mkfifo' fails closed with 9` → got **10** — **T144**, genuine
  RED, S3/T163. **Both `sleep` and `mkfifo` are missing from the script's
  `REQUIRED_TOOLS` sweep** — the property test derived the tool set from the
  script's own source and caught `mkfifo`, which the original finding named only
  `sleep`. Without them in the sweep the run continues past the check and dies at
  the display (10) instead of failing closed (9); `mkfifo` additionally breaks
  the FIFO creation ("cannot create the Xvfb output pipe").
- `T137 [sleep]` and `T137 [mkfifo]: the script's TOOL_PACKAGE map carries an
  entry` — **T137**, genuine RED, S3/T157: the map has no entry for either, so
  the sweep would report `unknown-package`.

*Integration suite — TC-1 got exit **143** (SIGTERM):* **not** a defect. The
real-game case was killed while three suites ran concurrently; a CLI-suite script
was hung for 8+ minutes. Corroborates the contention lesson below.

**Lesson, now enforced:** the suite runs are **serialised**, not just the test
*writing* — and the orchestrator must not run a suite while the agents are
running one either. A launch-free suite still reaches the build phase, so
running any two suites at once can corrupt a run's deploy digest or kill it
mid-flight. **T182 is the single authoritative run, done alone.**

**The precise contention map** (measured, not assumed):
- `test-logparse.sh` and `test-integration.sh` perform a **real `dotnet build`**
  of the mod and deploy into the shared mods folder. They contend with each
  other and with anything else that reaches the build phase, and the symptom is
  a spurious **exit `12`** (the deploy-digest comparison) or a **SIGTERM (143)**
  when one run's timeout fires inside the other's tree.
- `test-cli.sh` uses a **stubbed `dotnet`** (a sandbox "repository" reached
  through a symlink) — it never touches `LivingRoots/obj/`.
- `test-preflight.sh` **blocks `dotnet`** outright, so no test in it can ever
  rebuild the real mod.
- So: the two rebuilding suites must never overlap, and no suite may run while
  a rebuilding one runs. The pre-flight suite's 6 failures are **genuine RED**
  (the `sleep`/`mkfifo` sweep gap), not contention — proved by running the old
  and new scripts side by side on the "host without `sleep`" case and getting
  byte-identical behaviour.

**The 10 intended REDs in the integration suite (358 assertions, 348 pass, 10 fail) —
and the implementation task each one demands:**

| RED | Implied task |
|---|---|
| `TC-38: the run against the declared <ModFolderName> succeeds` → exit **12**, plus "the deploy target the build really uses is the one that was removed" and two run-log rows | **T156** |
| `TC-40: the report carries the removal's error` (the exit 12 and "names the deploy target" are already GREEN) | **T160** |
| `TC-41: the header's launch timestamp is not the run-start instant` | **T161** |
| `TC-42: the stdout reports the mod's success marker` (exit 0 is GREEN) | **T162** |
| `TC-44: the game's last line reaches the run log` + the build output's last line | **T170** |

T159 was already implemented (`close_open_phase`), so T139's assertions are GREEN
guards, not REDs. The other new cases (T093, T107, T127, T147, T148, T166) are
GREEN guards.

**Two corrections to what I recorded earlier:**
1. The integration suite's **TC-1 exit `143`** was **not** contention. Another
   agent reported that, while stopping a stuck run, it used a `pkill` pattern that
   also matched another session's real-game invocation of `smoke-validation.sh`
   and killed it. That is the actual cause; my earlier attribution to concurrent
   builds was wrong. The contention hazard is still real and is still the reason
   the two rebuilding suites must not overlap — but this particular failure was a
   stray signal.
2. The suite has a **launch artefact** worth knowing before the final run: a
   `nohup … &` launch makes bash ignore SIGINT/SIGHUP, which silently breaks
   **TC-7 and TC-17's SIGHUP rows**. A **foreground** run passes them. T183 must
   run the real-game case in the foreground.

**Why the fixture projects live inside the repository:** FR-016 (a) prefix-tests
`--mod-project` against the canonicalised **repository root**, so a fixture
project outside the tree is rejected. The suite therefore uses
`temp_test/smoke-fixture/` (already gitignored) and removes it afterwards.

## The `TOOL_PACKAGE` map was refactored — and the T137 test was coupled to its SOURCE SHAPE

S2 (T101: "let the command line be parsed before the host-tool sweep") moved the
sweep and, in the move, converted the `TOOL_PACKAGE` **associative array** into a
`tool_package_for()` **case function** (`scripts/smoke-validation.sh:1629-1665`).
The **behaviour is unchanged** — the same tool→package names, the same
`unknown-package` fallback, the same report line.

But the **pre-flight suite's T137 case parses the map out of the script's
source**, so it found 0 rows and 31 assertions started failing. That is a
**defect in the test, not in the implementation**: the task asks for the
*remediation package to be asserted*, which is a statement about the **report the
script prints**, not about how the script stores its table.

**The fix (T137's second half, still open):** rewrite the assertion to check the
**reported** package — withhold each tool, run the script, and assert the
failure report names the right package (`Xvfb` → `xorg-x11-server-Xvfb`,
`glxinfo`/`xdpyinfo` → `mesa-utils`, `flock` → `util-linux`, …) — so it holds
whatever the internal representation is. A source-shape parser is a change
detector, not a specification.

**S3 must also add `sleep` and `mkfifo`** to **both** `REQUIRED_TOOLS` (29
entries today, neither present) **and** `tool_package_for()`, or the sweep will
keep reporting `unknown-package` for them and the "a host without `sleep`
fails closed with `9`" case will keep failing (it currently gets `10`).

## How to verify bash-3 compatibility — build a real bash 3, don't audit

T169 requires the script to stay usable on a host whose bash is too old to run
the pipeline. The first implementation claimed the file was "parseable by bash 3"
— which turned out to be the **wrong bar**: it also has to **run**.

The verification that actually settled it used **two real bash 3 environments** —
the `bash:3.2` container (**3.2.57**) and CentOS 5 (**3.2.25**, glibc):

- `bash:3.2 -n scripts/smoke-validation.sh` → exit 0 (it parses).
- A **real run** on bash 3.2 died with `line 575: argv[@]: unbound variable` and
  **exit 1** — under `set -u`, bash 3.2 treats the expansion of an *empty* array
  as an unbound variable, so `set -- "${argv[@]}"` aborted the script before the
  pre-flight could report the missing requirement. Fixed with the
  `"${argv[@]+"${argv[@]}"}"` idiom, which yields 0 args, preserves quoting and
  works on bash 3.2 **and** 5.3.
- The audit of every bash-4-only construct is now clean: `declare -A` (comment
  only), `typeset -A`, `local -A`, `declare -n`, `local -n`, `mapfile`,
  `readarray`, `coproc`, `&>>`, `;;&`, `[[ -v ]]`, `${v@Q}`, `${!p@}`, negative
  array indices — **all zero**. `wait -f` is a *runtime* option of the `wait`
  builtin, so it costs nothing at parse time and is only reached after the
  pre-flight's check. `printf -v` is bash 3.1+ and also post-pre-flight.
- Measured on bash 3.2: `--help` → 65 stdout lines, 0 stderr, **exit 0**;
  `--version` → `smoke-validation.sh 0.1.0`, **exit 0**; `-h` → exit 0; an
  unknown option → **exit 14**, 0 bytes on stdout, exactly **one**
  What→Where→Fix report on stderr and **no run log created**; 13 further
  parser branches all exit 14; and a **real run** fails closed with **9**
  (`What: bash 4 or newer is required (found 3.2.25(1)-release)`).

**Reuse this method** — it is the only way to be sure, and it found two defects
an audit could not: the empty-array expansion, and a regression where
`expand_tilde` expanded a `~` path against an unset `HOME` (making `--game-path
'~'` exit **14** instead of the mandated **13**, and expanding `~/x` to `/x`).

## The atomic-commit-hook commits EVERYTHING staged — a real hazard under concurrency

`.specify/scripts/atomic-commit-hook.sh` is:

```bash
git add "$FILE"
git commit -m "feat/fix/docs: $SUMMARY — $FILE — $WHY"
```

`git commit` with no pathspec commits **every staged file**, not just `$FILE`. With
two agents working concurrently, **one agent's commit can carry the other
agent's work** — and the commit message then names the wrong file.

This happened: S3's T156 change (`resolve_mod_folder_name`) landed inside
**86c16ff**, which is **S2's** "T101/T169/T122 final state" commit. Nothing was
lost and the final state was verified green, but **a commit SHA no longer
reliably indicates which agent's work it carries**. Do not audit the history by
message alone — use `git log -S "<a distinctive string>"` to find where a change
actually landed.

**Mitigation for the orchestrator:** before every commit, run
`git diff --cached --stat` and confirm it shows **only** your file. When
another agent's file is already staged, either commit it deliberately (with a
message that names both files) or unstage it first — but **never** let it ride
along silently.

## The current suite baselines (after wave 2's S1–S5a)

| Suite | Assertions | Result |
|---|---|---|
| `test-cli.sh` | 255 | **255 passed, 0 failed** |
| `test-preflight.sh` | 380 | **380 passed, 0 failed** |
| `test-logparse.sh` | 167 | **167 passed, 0 failed** — was 160/7 |
| `test-integration.sh` | 359 | 352 passed, 7 failed — the 7 were REDs now closed by S5a/S5b plus TC-9's, which T184 re-pointed |

**The static shell lint over all seven shell files is at ZERO findings**, except
`scripts/tests/test-integration.sh`, which the plan agent found still had **7**
(findings measured per file, not assumed): it used `# shellcheck source=lib/harness.sh`,
but a **`source=` directive only takes effect under `shellcheck -x`**, and the
`lefthook` step lints **each staged file on its own** at the default severity, so
it never follows the sourced harness. The other three suites use a targeted
`# shellcheck disable=SC1091` with a reason. T164's code half closes the fourth.

**The graphics markers were extracted from the installed assembly, not guessed.**
SMAPI 4.5.2's own text (read out of `StardewModdingAPI.dll`: `Your graphics card`
/ ` isn't compatible with Stardew Valley. ` / `https://smapi.io/troubleshoot/no-suitable-gpu`
/ `Technical details:`) plus MonoGame's `NoSuitableGraphicsDeviceException`
(`Failed to create graphics device!`). The old `GPU: Could not detect` marker
exists in **no** SMAPI or MonoGame string.

**The crash-window fix, measured.** On a **44-line** crash file the old
`tail -n 30` window showed only stack frames 11–40 — **the failure record was
gone**. The new window (starting at the first line of the 30th **record** from
the end) shows all 4 records plus the 40 frames. That is the whole point of
FR-007's "a .NET stack trace is routinely 20–40 lines long".

**bash 3.2 verified with 24 assertions**: parse, `--help` exit 0 with the full
11-row table, `--version` exit 0, `-h` byte-identical, an unknown option exit 14
with exactly one What→Where→Fix report and **no run log**, a real run exit 9, and
a no-argument run exit 9 with **no "unbound variable"**.

## Wave 2 is complete — the script is final (2026-10-22)

**31 script tasks, in five sequential groups, all committed and verified.**

| Group | Tasks | The substance |
|---|---|---|
| S1 | T158, T168, T159, T118, T125, T121, T124 | the ERR handler is subshell-safe and runs with `set +e` (so the mandated exit `13` cannot be replaced by a raw status); the failed phase is closed; the EXIT trap's report is stamped and emitted for a non-zero exit; every group signal names the target PID; the forwarded signal is logged **before** it is sent |
| S2 | T169, T101, T122 | the **bash-4 requirement moved behind the parser** (the file must stay parseable *and runnable* on bash 3, which is why the tool→package table became a `case` function); the sweep runs after the command line is parsed; the inode freshness guard is evaluated **independently of the sentinel wait** |
| S3 | T156, T157, T160, T163, T167, T172, T103 | the deploy folder is the **effective `ModFolderName`**, **read not pinned**; the deploy-target removal is **fatal** with its own diagnostic; `sleep` and `mkfifo` are swept; the build's process group is a script-global |
| S5a | T155, T173, T098, T099 | the graphics markers are matched against a record's message **and its continuation lines**; the readiness probes are scoped to the post-sentinel region; the gate's context reaches three verdicts; the crash-file fallback comes from the trailing **records** |
| S5b | T161, T162, T170, T171, T102 | the launch epoch reaches the run log and the header field is relabelled `run started:`; the mod's `GameLaunched` marker is polled for in a **bounded, non-gating 30 s window**; the residual partial line is emitted after the read loop; the display number is re-read after the deadline loop; the build output is dumped before the timeout report |

**FR-005 was amended in the same commit as T162**, naming the one narrow exception:
> The script MUST terminate the game process immediately after the log check passes
> — with ONE narrow and bounded exception: the script MUST NOT wait longer than
> 30 seconds, and never past the `--timeout` deadline FR-009 defines, for the
> mod's own `GameLaunched` success marker after the mod-resolution header …
> the marker is a secondary, non-gating signal, so the expiry of that wait MUST
> NOT change the verdict — without leaving orphaned processes.

## Two things to know for any future run on this host

1. **`/tmp` fills up** — it is a 2 GB tmpfs and other sessions leave multi-GB
   build trees in it. The tell-tale symptom is `cat: erro de escrita: Não há
   espaço disponível no dispositivo` and the X server failing with exit `10`.
   **That is an ENVIRONMENT SYMPTOM, never a verdict.** `TMPDIR=/var/tmp` is
   the working fallback: a separate 1 GB tmpfs, and **outside `$HOME`**, so
   SMAPI does not `~`-anonymise the mods path (that only happens with a
   `TMPDIR` inside `$HOME`). Never delete another session's leftovers.
2. **The static lint is live**: `shellcheck` 0.11.0 is **absent** on this host,
   so the `lefthook` step self-skips here; the gate was verified with an
   out-of-tree binary (the Fedora RPM extracted with `rpm2cpio | cpio` — no
   system change) and **all eight** files in the hook's scope lint clean.

## T164 — the shellcheck lint, executed (2026-10-21)
`shellcheck` is **not installed** on this host (the `lefthook` step self-skips), but
the package **is** available in the Fedora repos. It was fetched **without touching
the system** — `dnf repoquery --location shellcheck` →
`http://fedora.c3sl.ufpr.br/…/ShellCheck-0.11.0-4.fc44.x86_64.rpm`, extracted with
`rpm2cpio | cpio` into `/tmp/opencode/sc/usr/bin/shellcheck` — and run over the
seven shell files. **32 findings, 0 errors.** Categorised:

| Kind | Count | Examples | Verdict |
|---|---|---|---|
| SC2329 "function never invoked" | 3 | `on_error`, `on_exit`, `forward_signal` | **false positive** — they are trap-invoked; needs a targeted suppression |
| SC1091 "not following ./lib/harness.sh" | 4 | the `source` line in each test file | **real gate problem** — the `lefthook` step runs `shellcheck {staged_files}` with no `-x`, so staging any test file would fail the hook. Needs a `source=` directive or `-x` |
| SC2016 "expressions don't expand in single quotes" | 3 | the awk programs | **false positive** — intentional |
| SC2088 "tilde does not expand in quotes" | 2 | the literal `~` arms in `case` | **false positive** — intentional |
| SC2178/SC2128 array-vs-string | 4 | `window` in `test-logparse.sh` | **false positive at runtime** — `_trailing_window()` has `local -a window=()`; `_assert_staging()` assigns the *string*. Different scopes. The names should still differ |
| SC2155 declare-and-assign | 3 | `run-all.sh`, `test-cli.sh` | real, low-risk |
| SC2034 write-only variable | 8 | `game_status` (script), `RUN_STDOUT`, `RUN_STDERR`, `RUN_ENV_ASSIGNMENTS`, `RUN_DEADLINE_SECS`, `RUN_PERFORMED`, `TC1_CODE`, `TC36_CODE`, `delay` | real — the constitution's simplicity principle (the same class as T088) |
| SC2295 expansion inside `${..}` | 2 | `test-preflight.sh:2388`, `test-integration.sh:1160` | real — a pattern-matching hazard |
| SC2004 unnecessary `$`/`${}` on an arithmetic variable | 1 | `test-cli.sh:170` | real, cosmetic |
| SC2317 unreachable command | 1 | `harness.sh:114` | real, cosmetic |

The lefthook `shellcheck` step must therefore end **passing** on a commit that
stages a test file — otherwise installing the binary would block every commit.

**Date**: 2026-10-21
**Branch**: `fix/protected-paths-script-confirm`
**Feature**: Smoke Validation Script (spec 008)

## COMPLETION SUMMARY

### Round 8 — checklist review (done) + implementation (done)

**Checklists: 361/361 passing** across `operational-readiness` (92), `requirements` (16),
`script-reliability` (89), `security-safety` (82) and `smapi-integration` (82). All five
were re-verified against the artifacts by five parallel review agents, and **ten parallel
research agents** gathered the supporting evidence (path-allowlist scope; declared side-effect
completeness; test coverage under ISO/IEC/IEEE 29119; timeout budget scope; SMAPI marker-file
semantics; process-group signal hygiene; TOCTOU; umask & file-mode policy; CLI option
inventory; SMAPI log-parsing robustness).

**The user accepted all five round-8 decisions** and they are integrated in `spec.md`
(§ "Session 2026-10-19 (checklist review — round 8, user decisions)"): the path allowlist is
withdrawn for environment-derived names in favour of quoting; a single normative
Command-Line Options table owns the CLI surface; exit `17` classifies a SMAPI abort and
`16` became reachable; nine launch-free test cases plus a coverage-summary table close the
traceability gaps; and the open-without-gating rule became normative in FR-012 item 7.

### Convergence phase 1 (T025–T059) — done

The round-8 audit appended 35 convergence tasks; all are implemented. The substantive
changes to `scripts/smoke-validation.sh`:

- **Signal handling (T025, T030, T055, T042, T041).** An external SIGHUP/SIGINT/SIGTERM
  now ends the run as a **genuine signal death** — `WIFSIGNALED` set, the caller observing
  `129`/`130`/`143` — at every phase. Verified end to end with a raw-`waitpid` probe.
  Every group signal is guarded by the mandatory PGID assertion; a failed assertion in the
  cleanup path aborts with `13` instead of claiming success.
- **Build bound (T026).** The rebuild runs in **its own process group** and the
  `--build-timeout` signals the **group**, so the MSBuild workers cannot survive the script
  and keep writing into the shared build output and the deploy target (FR-016 (b)). Child
  reaping is bounded (`reap_child`), so no child can hang the script past its bound.
- **Log freshness (T027).** The run sentinel's timestamp is compared against the launch
  timestamp `T0` **as UTC** — an explicit `Z`, because `date -d` parses a bare timestamp as
  *local* time and the local UTC offset would shift the comparison by hours.
- **Phase order (T033).** The flock, the existing-process check and the log baseline now run
  **after** the build-and-verify step, as FR-012 (0) pins them.
- **Verdict and report (T031, T032, T034, T036–T040, T057).** The mod's own success marker
  is detected and reported as a secondary, non-gating signal; the mod's `Trace` records join
  the failure context (so the exception type and HResult reach the report); a
  `SMAPI-crash.txt` written by **this** run fails the run; both SHA-256 digests are always
  named; the trailing records (capped at 30) are the context when no failure record matched;
  the `Fix:` line is the trailing line; every phase is timed; the failure report carries a
  timestamp, a level and the run ID; a failing run prints a stdout summary too.
- **Paths and tools (T043, T045, T046, T056, T058, T059).** The script's own authored
  literals are validated against `[A-Za-z0-9._-]+`; the deploy target and the SMAPI markers
  are canonicalised with `realpath -m` and prefix-tested with the component-boundary test;
  the tool sweep covers every tool the script uses and names the package that provides each
  one; the `--force` victim records its executable path and start time; the mod folder name,
  UniqueID and entry assembly are **derived** from the mod project's manifest.

### Convergence phase 2 (T060–T090) — done

A fresh assessment of the **present state** of the code (not a diff) covered FR-001…FR-017,
SC-001…SC-015, both user stories, the edge cases, the declared side effects, the 11 plan
decisions, the 20 data-model rules, all 59 existing tasks and the constitution's six
principles and three gates — **115 intent items**. It found **31 gaps** (2 `missing`,
26 `partial`, 2 `contradicts`, 3 `unrequested`; 0 CRITICAL, 3 HIGH, 11 MEDIUM, 17 LOW), which
are now **Phase 7** of `tasks.md` and all closed. The substantive changes:

- **T060** — `--mod-project` is canonicalised with `realpath -e` and prefix-tested against the
  canonicalized repository root; a project outside the repository is rejected before the build.
- **T061** — the third log-freshness guard is complete: the baseline inode is recorded
  before the stale log's removal and the recreated inode is compared against it, so SMAPI's
  `player-N` fallback can no longer leave a stale log deciding the verdict.
- **T062** — the process-group invariant is **asserted**: the child helper waits for the
  group to equal the child's PID and the run aborts with `13` rather than recording a group
  the script is forbidden to signal.
- **T065, T071** — the cleanup runs **before** the verdict on both paths, so the exit code
  and the stdout verdict can never disagree, and the stdout summary carries every phase's
  duration (FR-014).
- **T063, T066, T067, T075** — the deferred save check is labelled `SKIPPED:`, the run log
  holds every line the script emits, every signal is logged with its PID and signal name
  (plus the grace period and each SIGKILL escalation), and every line of the failure report
  carries the stamp while remaining an exact line of the log.
- **T068–T070, T076–T082, T086, T089** — the relative `--mods-path` rule applies only to a
  user-supplied value; the tool sweep covers every invoked external; the crash-file context
  comes from the normalised record stream; the built path derives from the manifest's
  `EntryDll`; the `--help` examples use `--opt=VALUE`; path arguments are passed after `--`;
  the containment helper no longer calls `die` inside a command substitution; the manifest
  read is attempted; the orphan-Xvfb detection falls back to `pidof`; the cleanup's group
  kills are isolated; `env` left the required tool set; `internal_real` is used.
- **T064, T072, T073, T085, T083, T084** — the spec's lock-descriptor clause now states the
  shipped behaviour with its evidence, the declared side effects list is complete, the
  data-model describes the code as it ships, and the plan's gate rows and the quickstart's
  coverage table match the measured reality.
- **T087, T088, T090** — the unrequested `SKIP_INTEGRATION` alias and the write-only harness
  state are gone, and the two wall-clock performance budgets are now **scaling** assertions
  (immune to host load) so the constitution's Gate 1 is reliably green.

### Test suite

`scripts/tests/`: a shared harness, a runner and four property suites.

| Suite | Assertions | Covers |
|---|---|---|
| `test-cli.sh` | 233 | TC-16, TC-18, TC-19, the 11-row option-surface property, the no-abbreviation property, the relative-`--mods-path` table |
| `test-preflight.sh` | 167 | TC-20…TC-24, the FR-012 item 8 tool sweep, the 7-row path-property matrix, the `--mod-project` validation, the `pidof`-only orphan detection |
| `test-logparse.sh` | 79 | the 9-row synthetic-log fixture matrix, the false-pass guard, the sentinel-freshness property, the inode-freshness table, the crash-file context |
| `test-integration.sh` | 305 | TC-1…TC-7, TC-9…TC-12, TC-14…TC-17, TC-25, TC-26, **TC-27…TC-33** (the new lifecycle cases) |

**784 assertions, all green.**

### The tool was verified against the REAL game

`./scripts/smoke-validation.sh` against the installed Stardew Valley + SMAPI 4.5.2:
**exit `0`, `SUCCESS: Mod loaded without errors.`** — the mod genuinely loads and the gate
reads the real log. Gate 3 is satisfied. The run log carries the launch contract, every
phase duration, the inode-freshness verdict, the `SKIPPED:` label and the verdict.

### The constitution's three gates (re-verified 2026-10-21)

| Gate | Result |
|---|---|
| **Gate 1** — static checks and tests | `dotnet build` 0 errors · `dotnet format --verify-no-changes` clean · `dotnet test` **1271 passed, 1 skipped, 0 failed** (the two load-sensitive wall-clock budgets are now scaling assertions) |
| **Gate 2** — product-level quality bar | `--help` renders the 11-row normative table and every published exit code; the failure reports follow "What → Where → How to fix" with a trailing `Fix:` line; idempotency is asserted by TC-25 |
| **Gate 3** — end-to-end verification | the real installation loads the mod and the run exits `0` with the success verdict; the `bash -n` gate was executed and **fails the hook** on a deliberately broken `scripts/tests/*.sh` |

### Environment facts (re-verified 2026-10-21)
- `Xvfb` **installed** (`xorg-x11-server-Xvfb` 21.1.25) — Gate 3 unblocked.
- `lefthook` is **2.2.2** and honours the array-form `glob:`; the `bash -n` step was
  demonstrated to fail the hook on a broken test file. `shellcheck` is still **absent**, so
  the static lint half of Gate 1 self-skips.
- Graphics verified: `xdpyinfo` reports GLX + RANDR; `glxinfo -B` exits 0 with `llvmpipe`;
  full `glxinfo` lists `GL_ARB_framebuffer_object` and `GL_EXT_framebuffer_object`.
- Measured: rebuild ~5 s warm; launch→verdict ~21 s; the whole run ~31 s.
- **`/tmp` is a 2 GB tmpfs and other sessions fill it.** A full tmpfs makes the X server
  fail to create its socket (exit `10`) and the build hang — a run that reports `10` or that
  times out at the harness deadline is an **environment** symptom, not a verdict. Free
  `/tmp` before a verification run.
- **Run the suites with the default `TMPDIR`.** A `TMPDIR` inside `$HOME` moves the fixture
  tree under the home directory; SMAPI then `~`-anonymises the mods path it prints, and
  TC-14's "`Mods go here:` names the resolved mods directory" assertion fails on a correct
  run. The default `/tmp` is outside `$HOME`, so no anonymisation applies.
- **Do not run two suites that rebuild the repo tree concurrently** — they contend on
  `LivingRoots/obj/` and the deploy digest assertion can fail with exit `12`.

## Remaining
- **None.** All **184** tasks in `specs/008-smoke-validation/tasks.md` are complete
  and marked `[X]`, and the feature is verified: 1 165 bash assertions and 1 272 C#
  assertions with 0 failures, the static shell lint clean over all eight files, and
  a real-game run exiting `0` with the mod's own success marker reported.
- **The next convergence round's backlog is already written down** — the quickstart's
  70-row per-MUST-sub-clause coverage table names every sub-clause that has **no
  assertion**, and each is a missing regression assertion. `/speckit.converge` can
  turn those rows straight into test tasks.
- One environmental loose end, not ours: a leaked stub X server (pid `320074`) under
  `/tmp/tmp.Veou0Gw9Md/`, from a suite run killed by a host restart hours ago. It
  belongs to the session that started it.

## The uncovered MUST sub-clauses — the next convergence round's backlog

T109 replaced the quickstart's blanket "Covered" coverage row with a **70-row
per-clause table**, one row per MUST sub-clause, each marked **C** (covered) /
**P** (partially covered, gap named) / **N** (no assertion at all), with the
covering assertion named. FR-013 is recorded as **"Guarded indirectly"** (the
CLI suite's option-surface property), and SC-006 — the quickstart's own contract
— is **N**: no suite reads `quickstart.md`.

That table is the honest coverage record, and the **N** rows are a real gap in
the *test suite*, not in the implementation. They are deliberately **not**
appended as tasks here: appending is `/speckit.converge`'s job, and the table
already makes each one traceable. The next convergence round should read
`quickstart.md`'s per-clause table and turn the **N** rows into test tasks —
they are the same class of gap Phase 9's TDD section was built from (an
uncovered MUST sub-clause is a missing regression assertion).

**The notable N rows:** FR-002's "every child launch closes the descriptor
(`9<&-`)" and its marker-removal clause; FR-005's "`set -m` enabled/disabled
around each launch", the "`9<&-`" clause and the "does not wait for `Saved`"
negative clause; FR-007's "allocation rule (MUST)" (nothing asserts the retired
code `8` is never emitted); FR-010's "each step isolated (`|| true`)" and the
reverse-allocation order; FR-011's "the victim's PID, exe path and start time
recorded before the first signal"; FR-012 (0)'s order, (6)'s marker removal, and
the **open-without-gating** rule; FR-014's **umask (MUST)**; FR-015's "the order
of the sections"; FR-016 (b)'s exact build command line and (d)'s "either file
missing" / "strictly older" branches; FR-017's "is not a directory" / "is not
writable".
