# Idea Research: Fechar [NV] de causa-raiz remota dos PRs #790–#796

- **Slug**: nv-remote-log-closure
- **Created**: 2026-11-22T03:34:24Z
- **Evidence confidence (overall)**: high

## Users & Demand

- O maintainer e a sessão de orquestração precisam distinguir "falha de provisão de segredo" de "regressão de código" ao interpretar checks vermelhos; o prompt de 2026-11-21 pede explicitamente a resolução autônoma máxima dos `[NV]`. — [source: pasted text + `.specify/memory/STATE.md:33`] (confidence: high)

## Prior Art

- Ciclo 025 (`025-ci-secret-guard-explicitness`) já tornou explícita a guarda `BUILDDEPS_TOKEN` em `build_and_test.yml` e escreveu a fronteira de confiança em `docs/quality-gate.md:42-48`, mas manteve a causa step-level como `[NV]`. — [source: `docs/quality-gate.md:48`, `.specify/validations/025-*/validate.md:38`] (confidence: high)
- Em 2026-11-20 os PRs #790–#796 foram mesclados/feched após provisão owner; o STATE.md registra o fechamento, mas não a re-leitura dos logs. — [source: `.specify/memory/STATE.md:202-204`] (confidence: high)

## Market & Context

- Não há mercado externo envolvido. O custo de inação é decisório: leitores de `STATE.md` continuam tratando como desconhecido um fato que é verificável com comando determinístico. — [ASSUMPTION] (confidence: medium)

## Data & Constraints

- `gh run view 44641194770 --log-failed` (executado 2026-11-22T03:32Z) → `##[error]BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` no job `Build and Test`, seguido de `##[error]Process completed with exit code 1.`; data do log: 2026-11-19T23:52:15Z. — [source: GitHub Actions API via `gh`, host `github.com`, policy: allowlisted] (confidence: high)
- `gh run view 44664287245 --log-failed` (2026-11-22T03:32Z) → mesmo erro no job `Check Formatting`; data do log: 2026-11-20T02:56:37Z. — [source: GitHub Actions API via `gh`, policy: allowlisted] (confidence: high)
- `gh run view 44664266743 --log-failed` (2026-11-22T03:34Z) → `Build and Test > Fetch game assemblies`, `##[error]BUILDDEPS_TOKEN is not set...`, exit code 1; data do log: 2026-11-20T02:56:25Z. — [source: GitHub Actions API via `gh`, policy: allowlisted] (confidence: high)
- `gh pr list --state open` em 2026-11-22T03:31Z → `[]` (zero PRs abertos); `gh pr list --state all` confirma #790–#796 mesclados/fechados até 2026-11-21. — [source: GitHub API via `gh`] (confidence: high)
- Restrição: a re-leitura é feita agora, com os PRs já encerrados; o valor é de auditoria histórica, não de desbloqueio de merge. — [source: evidência local] (confidence: high)

## Evidence Against the Idea

- Os logs são retidos por tempo limitado pelo GitHub; amanhã o mesmo comando pode não reproduzir a evidência. Mitigação: citar run IDs, timestamps e trechos exatos no artefato persistido.
- Promover o `[NV]` pode ser lido como "status remoto verificado para sempre". Mitigação: registrar a verificação com data/hora e escopo (causa step-level dos #790–#796), não generalizar para outros PRs.
- A maior parte dos `[NV]` restantes é humano/owner-gated (D1, atestação E, SC-004, tag/release, `RUNNER_AUDIT_STORE`, aprovador distinto) e não é resolvível por logs. — [source: `.specify/memory/STATE.md:206`] (confidence: high)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: o log de 2026-11-13 mencionado no STATE.md está disponível?] — não é necessário: três execuções independentes de 19/20-11 confirmam a mesma causa.

## Sources

- GitHub Actions via `gh run view <id> --log-failed` (host: github.com, policy: allowlisted) — run IDs 44641194770, 44664287245, 44664266743.
- GitHub PR list via `gh pr list --state open|all` (host: github.com, policy: allowlisted).
- Arquivos locais: `docs/quality-gate.md`, `.specify/memory/STATE.md`, `.specify/validations/025-ci-secret-guard-explicitness/validate.md`.
