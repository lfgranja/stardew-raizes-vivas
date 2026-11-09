#!/usr/bin/env bash
# test-020-heirloom-copy.sh — spec 020 / Phase 5 T024 copy lint.
# Proves the crioulo (Sementes Crioulas) i18n entries in default/en/pt
# carry no nitrogen-fixation vocabulary, no Pierre vilification, and name
# Portuguese crioulo vocabulary correctly. Exit-code contract mirrors the
# other smoke tests: 0 = pass, 1 = fail.
set -euo pipefail

ROOT="${1:-$PWD}"
I18N="$ROOT/LivingRoots/i18n"

fail() { echo "FAIL: $*" >&2; exit 1; }
pass() { echo "PASS: $*"; }

[ -f "$I18N/default.json" ] || fail "default.json missing"
[ -f "$I18N/en.json" ] || fail "en.json missing"
[ -f "$I18N/pt.json" ] || fail "pt.json missing"

python3 - "$I18N" <<'PY'
import json, sys

i18n_dir = sys.argv[1]

CRIOULO_KEYS = [
    "item.heirloom.parsnip-seed.name",
    "item.heirloom.parsnip-seed.description",
    "item.heirloom.parsnip.name",
    "item.heirloom.parsnip.description",
    "message.heirloom.seed-saved",
    "message.heirloom.seed-saved-inventory-full",
    "message.heirloom.plant-bonus-authority-unsupported",
]

FORBIDDEN_NITROGEN = [
    "fix nitrogen", "fixes nitrogen", "fixing nitrogen", "nitrogen fixation",
    "nitrogen-fixing", "nitrogênio", "fixar nitrogênio", "fixador de nitrogênio",
    "fijar nitrógeno", "fijación de nitrógeno", "azoto", "sequestro de nitrogênio",
]

FORBIDDEN_PIERRE_VILIFY = [
    "evil", "mal", "bad pierre", "pierre is wrong", "slave", "slavery",
    "esclav", "escravizador", "monopoly man", "capitalista",
]

def load(name):
    with open(f"{i18n_dir}/{name}", encoding="utf-8") as f:
        return json.load(f)

en = load("en.json")
pt = load("pt.json")
default = load("default.json")

errors = []
for locale, data in (("default", default), ("en", en), ("pt", pt)):
    for key in CRIOULO_KEYS:
        if key not in data:
            errors.append(f"{locale}.json missing key {key}")
            continue
        text = str(data[key]).lower()
        for token in FORBIDDEN_NITROGEN:
            if token in text:
                errors.append(f"{locale}.json '{key}' carries forbidden nitrogen vocabulary '{token}'")
        for token in FORBIDDEN_PIERRE_VILIFY:
            if token in text:
                errors.append(f"{locale}.json '{key}' vilifies Pierre ('{token}')")

seed_name_pt = str(pt.get("item.heirloom.parsnip-seed.name", "")).lower()
produce_name_pt = str(pt.get("item.heirloom.parsnip.name", "")).lower()
if "crioula" not in seed_name_pt:
    errors.append("pt.json seed name must contain 'crioula' (Sementes Crioulas register)")
if "crioulo" not in produce_name_pt:
    errors.append("pt.json produce name must contain 'crioulo'")

if errors:
    for e in errors:
        print("FAIL:", e)
    sys.exit(1)
print("PW: all crioulo copy checks passed (nitrogen-free, Pierre respectful, PT register ok)")
PY
