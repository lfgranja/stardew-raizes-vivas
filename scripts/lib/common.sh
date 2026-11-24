#!/usr/bin/env bash
#
# scripts/lib/common.sh — shared constants and helpers for the headless
# verification runner (feature 010).
#
# Sourced, never executed. Provides the exit-code enum from
# specs/010-headless-verification-runner/contracts/exit-codes.md and the
# runner's machine vocabulary and tuning constants. No magic numbers should
# appear in other scripts; source this file instead.
#

if [[ -n "${_LR_COMMON_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_COMMON_SH_LOADED=1

# ---------------------------------------------------------------------------
# Exit-code enum (contracts/exit-codes.md). Every code 0..22 enumerated.
# ---------------------------------------------------------------------------

EXIT_PASS=0
EXIT_GAME_NOT_FOUND=1
EXIT_SMAPI_NOT_FOUND=2
EXIT_GRAPHICS_UNUSABLE=3
EXIT_TIMEOUT=4
EXIT_MOD_LOAD_FAILURE=5
EXIT_LOCK_CONTENTION=6
EXIT_PROCESS_CONFLICT=7
EXIT_RETIRED=8
EXIT_MISSING_DEPENDENCY=9
EXIT_DISPLAY_UNAVAILABLE=10
EXIT_STEAM_PREREQ_MISSING=11
EXIT_BUILD_ARTIFACT_FAILURE=12
EXIT_INTERNAL_ERROR=13
EXIT_USAGE_ERROR=14
EXIT_NO_LOG_PRODUCED=15
EXIT_MOD_MANIFEST_INVALID=16
EXIT_MOD_RESOLUTION_ABORTED=17
EXIT_FAIL=18
EXIT_ERRORED=19
EXIT_NO_TESTS=20
EXIT_POLICY=21
EXIT_RESERVED=22

# Aliases used by various callers.
EX_PASS=$EXIT_PASS
EX_GAME_NOT_FOUND=$EXIT_GAME_NOT_FOUND
EX_SMAPI_NOT_FOUND=$EXIT_SMAPI_NOT_FOUND
EX_GRAPHICS_UNUSABLE=$EXIT_GRAPHICS_UNUSABLE
EX_TIMEOUT=$EXIT_TIMEOUT
EX_MOD_LOAD_FAILURE=$EXIT_MOD_LOAD_FAILURE
EX_LOCK_CONTENTION=$EXIT_LOCK_CONTENTION
EX_PROCESS_CONFLICT=$EXIT_PROCESS_CONFLICT
EX_RETIRED=$EXIT_RETIRED
EX_MISSING_DEPENDENCY=$EXIT_MISSING_DEPENDENCY
EX_DISPLAY_UNAVAILABLE=$EXIT_DISPLAY_UNAVAILABLE
EX_STEAM_PREREQ_MISSING=$EXIT_STEAM_PREREQ_MISSING
EX_BUILD_ARTIFACT_FAILURE=$EXIT_BUILD_ARTIFACT_FAILURE
EX_INTERNAL_ERROR=$EXIT_INTERNAL_ERROR
EX_USAGE_ERROR=$EXIT_USAGE_ERROR
EX_NO_LOG_PRODUCED=$EXIT_NO_LOG_PRODUCED
EX_MOD_MANIFEST_INVALID=$EXIT_MOD_MANIFEST_INVALID
EX_MOD_RESOLUTION_ABORTED=$EXIT_MOD_RESOLUTION_ABORTED
EX_FAIL=$EXIT_FAIL
EX_ERRORED=$EXIT_ERRORED
EX_NO_TESTS=$EXIT_NO_TESTS
EX_POLICY=$EXIT_POLICY
EX_RESERVED=$EXIT_RESERVED

RUNNER_EXIT_PASS=$EXIT_PASS
RUNNER_EXIT_GAME_NOT_FOUND=$EXIT_GAME_NOT_FOUND
RUNNER_EXIT_SMAPI_NOT_FOUND=$EXIT_SMAPI_NOT_FOUND
RUNNER_EXIT_GRAPHICS_UNUSABLE=$EXIT_GRAPHICS_UNUSABLE
RUNNER_EXIT_TIMEOUT=$EXIT_TIMEOUT
RUNNER_EXIT_MOD_LOAD_FAILURE=$EXIT_MOD_LOAD_FAILURE
RUNNER_EXIT_LOCK_CONTENTION=$EXIT_LOCK_CONTENTION
RUNNER_EXIT_PROCESS_CONFLICT=$EXIT_PROCESS_CONFLICT
RUNNER_EXIT_RETIRED=$EXIT_RETIRED
RUNNER_EXIT_MISSING_DEPENDENCY=$EXIT_MISSING_DEPENDENCY
RUNNER_EXIT_DISPLAY_UNAVAILABLE=$EXIT_DISPLAY_UNAVAILABLE
RUNNER_EXIT_STEAM_PREREQ_MISSING=$EXIT_STEAM_PREREQ_MISSING
RUNNER_EXIT_BUILD_ARTIFACT_FAILURE=$EXIT_BUILD_ARTIFACT_FAILURE
RUNNER_EXIT_INTERNAL_ERROR=$EXIT_INTERNAL_ERROR
RUNNER_EXIT_USAGE_ERROR=$EXIT_USAGE_ERROR
RUNNER_EXIT_NO_LOG_PRODUCED=$EXIT_NO_LOG_PRODUCED
RUNNER_EXIT_MOD_MANIFEST_INVALID=$EXIT_MOD_MANIFEST_INVALID
RUNNER_EXIT_MOD_RESOLUTION_ABORTED=$EXIT_MOD_RESOLUTION_ABORTED
RUNNER_EXIT_FAIL=$EXIT_FAIL
RUNNER_EXIT_ERRORED=$EXIT_ERRORED
RUNNER_EXIT_NO_TESTS=$EXIT_NO_TESTS
RUNNER_EXIT_POLICY=$EXIT_POLICY
RUNNER_EXIT_RESERVED=$EXIT_RESERVED

# ---------------------------------------------------------------------------
# Machine vocabulary
# ---------------------------------------------------------------------------

RUNNER_MACHINE_PASS="pass"
RUNNER_MACHINE_VERIFIED="verified"
RUNNER_MACHINE_FAIL="fail"
RUNNER_MACHINE_ERRORED="errored"
RUNNER_MACHINE_PARTIAL="partial"
RUNNER_MACHINE_SKIPPED="skipped"

MACHINE_PASS="$RUNNER_MACHINE_PASS"
MACHINE_VERIFIED="$RUNNER_MACHINE_VERIFIED"
MACHINE_PASS_VOCABULARY="$RUNNER_MACHINE_PASS"
MACHINE_VERIFIED_VOCABULARY="$RUNNER_MACHINE_VERIFIED"

# ---------------------------------------------------------------------------
# Runner tuning constants
# ---------------------------------------------------------------------------

RunnerMaxSuiteReruns=4
RunnerMaxTransientRetries=2
RunnerRetryCooldownMs=5000
RunnerLockTotalTimeoutMs=10000
RunnerClaimStaleGraceMs=30000
RunnerReclaimGuardStaleMs=30000

RunnerLockInitialBackoffMs=100
RunnerLockBackoffMultiplier=2
RunnerLockMaxBackoffMs=2000
RunnerLockMaxAttempts=8
RunnerLockJitterPercent=25

RunnerMaxScenarioReruns=$RunnerMaxSuiteReruns

# Export everything.
export EXIT_PASS EXIT_GAME_NOT_FOUND EXIT_SMAPI_NOT_FOUND EXIT_GRAPHICS_UNUSABLE \
       EXIT_TIMEOUT EXIT_MOD_LOAD_FAILURE EXIT_LOCK_CONTENTION EXIT_PROCESS_CONFLICT \
       EXIT_RETIRED EXIT_MISSING_DEPENDENCY EXIT_DISPLAY_UNAVAILABLE \
       EXIT_STEAM_PREREQ_MISSING EXIT_BUILD_ARTIFACT_FAILURE EXIT_INTERNAL_ERROR \
       EXIT_USAGE_ERROR EXIT_NO_LOG_PRODUCED EXIT_MOD_MANIFEST_INVALID \
       EXIT_MOD_RESOLUTION_ABORTED EXIT_FAIL EXIT_ERRORED EXIT_NO_TESTS \
       EXIT_POLICY EXIT_RESERVED

export RunnerMaxSuiteReruns RunnerMaxTransientRetries RunnerRetryCooldownMs \
       RunnerLockTotalTimeoutMs RunnerClaimStaleGraceMs RunnerReclaimGuardStaleMs \
       RunnerLockInitialBackoffMs RunnerLockBackoffMultiplier RunnerLockMaxBackoffMs

# ---------------------------------------------------------------------------
# US1 additions (feature 010)
# ---------------------------------------------------------------------------
RunnerMaxAttempts=3
RunnerFlakyObservationWindowDays=14
RunnerFlakyQuarantineTriggerCount=2
RunnerQuarantineTimeBoxDays=14
RunnerQuarantineExtendedTimeBoxDays=28
RunnerCleanQuitStabilitySeconds=30
RunnerCleanQuitBackstopSeconds=120
RunnerPerLocationBudgetMs=5
RunnerGenerationBudget=1
RunnerRunIdPattern='^run-[0-9]{8}T[0-9]{6}Z-[0-9a-f]{4}$'
RunnerIntegrityClasses='checksum archive-member extraction token-invalid partial-download'
RunnerAllowedIgnoreExitCodes='4 6 7 9 10 13 14 18 19 20 21'
RunnerAllowedOverrideKeys='environment.gameVersion environment.smapiVersion environment.modVersion environment.saveId environment.config.*'

export RunnerMaxAttempts RunnerFlakyObservationWindowDays RunnerFlakyQuarantineTriggerCount \
       RunnerQuarantineTimeBoxDays RunnerQuarantineExtendedTimeBoxDays \
       RunnerCleanQuitStabilitySeconds RunnerCleanQuitBackstopSeconds \
       RunnerPerLocationBudgetMs RunnerGenerationBudget RunnerRunIdPattern \
       RunnerIntegrityClasses RunnerAllowedIgnoreExitCodes RunnerAllowedOverrideKeys

# runner_exit_name <code> -> class name
runner_exit_name() {
    local code="${1:-}"
    case "$code" in
        0) echo "PASS" ;;
        1) echo "GAME_NOT_FOUND" ;;
        2) echo "SMAPI_NOT_FOUND" ;;
        3) echo "GRAPHICS_UNUSABLE" ;;
        4) echo "TIMEOUT" ;;
        5) echo "MOD_LOAD_FAILURE" ;;
        6) echo "LOCK_CONTENTION" ;;
        7) echo "PROCESS_CONFLICT" ;;
        8) echo "RETIRED" ;;
        9) echo "MISSING_DEPENDENCY" ;;
        10) echo "DISPLAY_UNAVAILABLE" ;;
        11) echo "STEAM_PREREQ_MISSING" ;;
        12) echo "BUILD_ARTIFACT_FAILURE" ;;
        13) echo "INTERNAL_ERROR" ;;
        14) echo "USAGE_ERROR" ;;
        15) echo "NO_LOG_PRODUCED" ;;
        16) echo "MOD_MANIFEST_INVALID" ;;
        17) echo "MOD_RESOLUTION_ABORTED" ;;
        18) echo "FAIL" ;;
        19) echo "ERRORED" ;;
        20) echo "NO_TESTS" ;;
        21) echo "POLICY" ;;
        22) echo "RESERVED" ;;
        *) echo "UNKNOWN" ;;
    esac
    return 0
}

# runner_now_iso
runner_now_iso() { date -u +%Y-%m-%dT%H:%M:%SZ; }
lr_now_iso() { runner_now_iso; }
lr_now_epoch() { date -u +%s; }

# runner_run_id -> run-YYYYMMDDTHHMMSSZ-<4 hex>
runner_run_id() {
    local ts hex
    ts="$(date -u +%Y%m%dT%H%M%SZ)"
    hex="$(printf '%04x' $((RANDOM % 65536)))"
    printf 'run-%s-%s\n' "$ts" "$hex"
}

evidence_run_id() { runner_run_id; }

export RUNNER_MACHINE_PASS RUNNER_MACHINE_VERIFIED RUNNER_MACHINE_FAIL \
       RUNNER_MACHINE_ERRORED RUNNER_MACHINE_PARTIAL RUNNER_MACHINE_SKIPPED \
       MACHINE_PASS MACHINE_VERIFIED MACHINE_PASS_VOCABULARY MACHINE_VERIFIED_VOCABULARY

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

log_info() {
    printf '[INFO] %s\n' "$*" >&2
}

log_warn() {
    printf '[WARN] %s\n' "$*" >&2
}

log_error() {
    printf '[ERROR] %s\n' "$(redact "$*")" >&2
}
log_debug() {
    if [[ "${RUNNER_VERBOSE:-0}" == "1" ]]; then
        printf '[DEBUG] %s\n' "$*" >&2
    fi
}

is_uint() {
    [[ "${1:-}" =~ ^[0-9]+$ ]]
}


# ISO-8601 UTC timestamp helper used by other libs.
lr_now_iso() {
    date -u +%Y-%m-%dT%H:%M:%S.%3NZ
}

lr_now_epoch() {
    date -u +%s
}

# Source guard for callers that want to ensure common is loaded.
lr_common_loaded() {
    [[ -n "${_LR_COMMON_SH_LOADED:-}" ]]
}
