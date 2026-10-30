# Environment — Local OSS Quality Gate

**Feature**: 011-local-oss-quality-gate
**Recorded**: 2026-10-29

## Host

| Component | Value | Command |
|---|---|---|
| OS | Fedora Linux 44 (KDE Plasma Desktop Edition), Linux 7.2.11-200.fc44.x86_64 | `uname -sr`; `grep PRETTY_NAME /etc/os-release` |
| .NET SDK | 10.0.400 (solution targets net6.0) | `dotnet --version` |
| Python | 3.14.8 | `python3 --version` |
| Node.js | v22.23.1 | `node --version` |
| jscpd | 5.4.2 | `npx --yes jscpd@5.4.2 --version` |
| Semgrep | 1.182.1 (in `.venv-quality-gate/`) | `.venv-quality-gate/bin/semgrep --version` |
| ReportGenerator | 5.5.12 (local tool) | `dotnet tool list` |
| Trivy | not installed at T007; installed for T035 with the pinned release binary | `trivy --version` |
| Git commit at setup | `02a76f7` | `git rev-parse HEAD` |

## Python environment

```bash
python3 -m venv .venv-quality-gate
.venv-quality-gate/bin/pip install -r scripts/quality-gate/requirements-dev.txt \
  -r scripts/quality-gate/requirements-ci.txt
```

Verified:

```text
pytest 9.2.0
hypothesis 6.170.13
pyyaml 6.0.3
1.182.1
```

The environment lives in `.venv-quality-gate/`, which is gitignored.

## Commands that must resolve the package

From the repository root:

```bash
export PYTHONPATH=scripts/quality-gate
python -m quality_gate --help
```

or invoke the interpreter directly with the same `PYTHONPATH`.

## Notes

- The machine has a local Stardew Valley install, so the pre-change build does not need `BUILDDEPS_TOKEN`.
- CI uses Python 3.12; the local venv uses 3.14. Both are supported by the pinned dependencies.
- `dotnet reportgenerator` is a local tool; `reportgenerator` alone is not on `PATH`.
