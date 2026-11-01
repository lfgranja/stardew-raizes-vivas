# Assessment: Shellcheck issues S131, S7679, S1066

## Bug Report
Process Shell script issues (S131 add default case, S7679 assign positional params, S1066 merge if).

Many issues across scripts/tests/*.sh and scripts/smoke-validation.sh. Fix by adding default cases to case statements, assigning positional params to local vars, merging nested ifs. Follow bug workflow. Test with shellcheck if available.

## Assessment
- Scope: `scripts/*.sh`, `scripts/tests/*.sh`, and potentially subdirs under `scripts/`. 
- Issues:
  - S131: `case` statements missing default `*)` case
  - S7679: Use local variables to assign positional parameters to avoid modifying `$1`, `$2`, etc. (or just assign before using)
  - S1066: Merge nested `if` statements when possible
- Tools: `shellcheck` not installed; need to detect patterns manually or install? Or just fix by patterns.
- Follow bug workflow: assess -> fix -> test. Must record in these files.
- Priority: fix all occurrences across mentioned files.

## Suspected locations
Need to grep for patterns:
- case statements without `*)`
- positional params used directly in complex ways? S7679 typically means assign `$1` to a local var like `local arg="$1"` rather than using `$1` repeatedly in a way that might be confusing or modifying positional params. Or in some shells better practice.
- nested ifs like `if ...; then if ...; then`

Also check `smoke-validation.sh` and all test scripts.

## Plan
1. Create assessment.md (done)
2. Find all case statements without default and add `*) ;;` or appropriate default
3. Find positional params used directly and assign to local vars at start of functions
4. Merge nested ifs where semantically equivalent
5. Run shellcheck if becomes available or just apply fixes
6. Document in fix.md, test.md

But need to be careful about semantics.
