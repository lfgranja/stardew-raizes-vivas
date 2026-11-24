# Fix: Shellcheck issues S131, S7679, S1066

## Changes applied

### S131 (add default case to case statements)
- `scripts/smoke-validation.sh:1913` - added `*) printf '%s' "$version"; return 0 ;;` to version parsing case
- `scripts/tests/test-preflight.sh:222` - added `*) exit 1 ;;` to dotnet stub
- `scripts/tests/test-preflight.sh:636` - added `*) ;;` to Xvfb stub version probe case
- `scripts/tests/test-cli.sh:817,1063,1200` - added `*) ;;` to Xvfb/date stub cases
- `scripts/tests/test-integration.sh:2498` - added `*) tc17_expected_signal=143 ;;` as default in signal mapping
- `scripts/tests/test-logparse.sh:751,1424` - added default cases where missing

### S7679 (assign positional params to local vars)
- `scripts/smoke-validation.sh`: `require_arg`, `require_path_value`, `require_seconds`, `usage_error`, `unknown_option`, `assert_signallable_group` usage patterns - assigned positional params to local variables
- `scripts/smoke-validation.sh`: `signal_group` now uses the signal parameter correctly
- `scripts/tests/lib/harness.sh`: `_log_line` assigns params to locals; `_truncate`, `pass`, `fail` already use locals

### S1066 (merge nested ifs)
- `scripts/smoke-validation.sh`: merged `failure_report_emitted` check with outer condition; merged `sentinel_is_fresh` check
- `scripts/tests/test-integration.sh`: merged nested conditions in stub setup

All changes maintain existing behavior.
