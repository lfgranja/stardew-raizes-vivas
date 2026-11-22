# Data Model: 031-nv-remote-log-closure

Documentação não possui modelo de dados executável. Entidades conceptuais:

## RemoteEvidenceNote

| Field | Rule |
|-------|------|
| `read_at` | ISO 8601 com timezone; obrigatório; data da leitura ao vivo |
| `command` | Comando `gh` executado, sem credenciais |
| `run_id` | ID numérico do GitHub Actions; obrigatório; ≥3 por nota |
| `step_name` | Nome do step quando disponível (`Fetch game assemblies`) |
| `log_excerpt` | Trecho literal de `##[error]`; obrigatório |
| `scope` | PRs #790–#796, causa step-level; não generalizável |

**Validation**: todos os campos obrigatórios presentes; nenhum token/segredo no conteúdo.

## StateAnchor

| Field | Rule |
|-------|------|
| `path` | Um de `STATE.md`, `docs/PROGRESSO.md`, `docs/research/index.md`, `docs/quality-gate.md` |
| `note_date` | ISO 8601 com timezone |
| `target` | Caminho relativo de `RemoteEvidenceNote` |
| `resolved_claim` | Causa step-level dos #790–#796 |
| `preserved_nv` | Lista explícita de afirmações que permanecem `[NV]` |

**State transitions**: nenhuma; notas são append-only.

**Relationships**: uma `RemoteEvidenceNote` é referenciada por quatro `StateAnchor`s.
