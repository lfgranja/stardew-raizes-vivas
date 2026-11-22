# Idea Research: Promoção dev→main CI-green na fila SWE

- **Slug**: swe-escalation-queue
- **Created**: 2026-11-22T05:32:37Z
- **Evidence confidence (overall)**: high

## Users & Demand

- O maintainer precisa ver explicitamente que o único bloqueio restante para `main` é humano (aprovador), não técnico. — [source: prompt + `gh`] (confidence: high)

## Prior Art

- Fila oficial: `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml`, 7 itens, formato `id/data/origem/tipo_023/bloqueio/.../status/owner/refs`. — [source: leitura local] (confidence: high)

## Data & Constraints

- `gh pr view 821` → `baseRefName=main`, `headRefOid=4b92cd8`, `state=OPEN`, `mergeable=MERGEABLE`, `mergeStateStatus=BLOCKED`, `reviewDecision=REVIEW_REQUIRED`. — [source: GitHub API via `gh`] (confidence: high)
- `gh pr checks 821 --watch --interval 30` → `RC=0` às 2026-11-22T05:31:56Z; todos os checks `pass` ou `skipping` por design. — [source: `gh`] (confidence: high)
- Proteção de `main` lida ao vivo: required `sdd-gate`, `strict:true`, `reviews:1`, `enforce_admins:true`. — [source: `gh api .../branches/main/protection`] (confidence: high)
- `release.yml` dispara apenas em tags `v*.*.*`; `STATE.md` registra 0 tags/0 releases públicos — publicação é owner-only. — [source: leitura local] (confidence: high)

## Evidence Against the Idea

- Registrar "CI green" pode ser lido como autorização para mesclar; mitigação: texto explícito de que o merge é SWE-gated e não foi executado.

## Sources

- GitHub API/`gh` (allowlisted); arquivos locais do alvo.
