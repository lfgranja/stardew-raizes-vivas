# Idea Research: Correção de atribuição de evidência e SemVer

- **Slug**: evidence-attribution-fix
- **Created**: 2026-11-22T05:19:30Z
- **Evidence confidence (overall)**: high

## Users & Demand

- Leitores da trilha SDD precisam que cada afirmação remota tenha atribuição exata ao run observado. — [source: prompt / `STATE.md`] (confidence: high)

## Prior Art

- Ciclos 025–033 usam SemVer PATCH em `release.md`; 031 = 0.1.6, 033 = 0.1.8. — [source: artefatos mesclados] (confidence: high)

## Data & Constraints

- `.specify/releases/032-*/release.md` após merge: Slug → Date → Validation → Changelog; **SemVer 0.1.7 ausente**. — [source: leitura local 2026-11-22T05:19Z] (confidence: high)
- `docs/quality-gate.md:48` original falava em PRs "observed red on 2026-11-18"; runs lidos em 2026-11-22 foram de 2026-11-19T23:52Z e 2026-11-20T02:56Z, em branches `dependabot/...` (incluindo coverlet-10.1.1 e JsonSchema.Net-9.7.0, que o PROGRESSO atribui a #811/#812). — [source: `gh run view --log-failed` + `STATE.md`] (confidence: high)
- `gh run list --limit 300` filtrado para 2026-11-18 retornou vazio; não há verificação direta dos logs daquela data nesta sessão. — [source: `gh` ao vivo] (confidence: high)
- Mecanismo é idêntico em todos os runs lidos: `BUILDDEPS_TOKEN is not set` no step de busca de assemblies; segredo existe no repo, não no escopo Dependabot. — [source: logs] (confidence: high)

## Evidence Against the Idea

- Restringir a atribuição pode parecer "desfazer" o fechamento do `[NV]`; mitigação: manter o mecanismo como verificado e marcar a extensão histórica como inferência.

## Sources

- Arquivos locais mesclados; GitHub Actions via `gh` (allowlisted).
