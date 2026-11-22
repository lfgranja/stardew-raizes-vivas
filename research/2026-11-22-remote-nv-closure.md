# Evidência remota verificada ao vivo — PRs #790–#796

**Lido em**: 2026-11-22T03:32–03:38Z
**Escopo**: causa-raiz **step-level** dos checks vermelhos dos PRs de atualização de dependências #790–#796.
**Não cobre**: D1, atestação humana E de Gate 3, SC-004, tag/release pública, `RUNNER_AUDIT_STORE` (#523), aprovação de revisor distinto, status de PRs não observados ao vivo.

## Comandos executados

```bash
gh run view 44641194770 --log-failed
gh run view 44664287245 --log-failed
gh run view 44664266743 --log-failed
gh secret list
gh api repos/lfgranja/stardew-raizes-vivas/dependabot/secrets --jq '.secrets[]?.name'
gh pr list --state open --limit 100
```

## Execuções remotas (GitHub Actions, host `github.com`, policy: allowlisted)

| Run ID | Data do log | Job / step | Trecho literal |
|--------|-------------|------------|----------------|
| `44641194770` | 2026-11-19T23:52:15Z | `Build and Test` | `##[error]BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` / `##[error]Process completed with exit code 1.` |
| `44664287245` | 2026-11-20T02:56:37Z | `Check Formatting` | `##[error]BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` / `##[error]Process completed with exit code 1.` |
| `44664266743` | 2026-11-20T02:56:25Z | `Build and Test` → `Fetch game assemblies` | `##[error]BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` / `##[error]Process completed with exit code 1.` |

As três execuções exibem o mesmo erro de provisão antes de qualquer compilação ou teste. Não há evidência de regressão de código nestes logs.

## Provisão de segredo

`gh secret list` (2026-11-22T03:37Z) → `BUILDDEPS_TOKEN` (atualizado em 2026-10-18T13:51:38Z) e `SONAR_TOKEN` existem como **segredos de repositório**.
`gh api repos/lfgranja/stardew-raizes-vivas/dependabot/secrets` → **lista vazia** (nenhum segredo escopado ao Dependabot).

Conclusão escopada: os PRs do Dependabot #790–#796 não receberam `BUILDDEPS_TOKEN`, e os jobs que tentavam baixar `gamepath.zip` falharam no step de busca de assemblies com `exit code 1`. Isto resolve o `[NV]` step-level registrado em `docs/quality-gate.md:48` e `.specify/memory/STATE.md:33` para aqueles PRs.

## Estado atual dos PRs

`gh pr list --state open --limit 100` em 2026-11-22T03:31Z → `[]` (zero PRs abertos). PRs #790–#796 estão mesclados/fechados; nenhum foi reaberto ou reexecutado por este ciclo.

## Honestidade e limites

- A verificação é **ao vivo via `gh`**, datada; não é inferência de LLM.
- A retenção de logs pelo GitHub é limitada: os trechos literais acima são a evidência persistida caso os runs expirem.
- O comando `gh api .../dependabot/secrets` lista **nomes**, nunca valores; nenhuma credencial foi copiada, exibida ou versionada.
- Nenhuma afirmação remota fora deste escopo é promovida: tudo o mais permanece `[NV]`.

## Correção de atribuição (2026-11-22T05:20Z, ciclo 034)

- Os três runs citados acima foram lidos em 2026-11-22 e datam de **19–20/11**, em branches `dependabot/...`. Dois deles (`coverlet.collector-10.1.1`, `JsonSchema.Net-9.7.0`) correspondem aos PRs **#811/#812** conforme `STATE.md`/`PROGRESSO.md`, não necessariamente aos PRs #790–#796 da observação original de 18/11.
- `gh run list --limit 300` filtrado para 2026-11-18 não retornou runs nesta sessão. Portanto: **verificado** é o mecanismo (`BUILDDEPS_TOKEN` ausente no escopo Dependabot) nos runs de 19–20/11; **inferência** é estender a mesma causa aos PRs de 18/11. Nenhuma afirmação de leitura direta dos logs de 18/11 é feita.
