# PROGRESSO.md — Living Roots / Raízes Vivas

Âncora de progresso verificável. Atualizações são append-only: fatos locais executados são registrados com evidência; afirmações remotas são marcadas `[NV]` até observação ao vivo via `gh`.

## Estado verificado — 2026-11-20

- **Âncora**: alvo `/home/luis/development/stardew-raizes-vivas` @ HEAD `7df732b`. `.specify/feature.json` → `specs/028-quality-gate-pytest-wiring`. Fonte de aparato: factory `b1deab1` (somente leitura).
- **Ciclos entregues**: 026 (overlay cache), 027 (flakiness de relógio de parede), 028 (quality-gate pytest wiring). PR #810 (ciclo 028) `MERGED` em 2026-11-20T02:13:21Z.
- **Gates locais**: `bash scripts/ci/aggregate-gate.sh` → **ALL STEPS PASSED** nesta sessão. `scripts/release-gate.sh` registrado **ALL STEPS PASSED** em `047228e` (7 etapas, `GamePath=/home/luis/stardew-assemblies`; smoke launch-free 51 arquivos / 50 passed / 0 failed / 1 skipped).
- **Suítes registradas**: pytest quality-gate 180/0 após o ciclo 028; xUnit 1999 passed / 0 failed / 4 skipped (2003) no ciclo 027; Gate 3 host-only `machine-pass` com 3 sinais concordantes (bundle `evidence-bundle-run-20261120T012343Z-6b29`), sem `verified` humano.
- **Contagens por script**: 25 diretórios em `specs/` (21 com `spec.md`), 20 assessments, 23 validations, 23 releases, 40 bugs, 24 arquivos de research. Nenhum `*.generated.md` existe.
- **PRs abertos [NV para merge]**: #811 e #812, ambos `MERGEABLE`; `sdd-gate` e `verify` pass, mas `Build and Test`, `Check Formatting` e Quality Gate build-analysis/gate falham por `BUILDDEPS_TOKEN is not set` no contexto de segredo do Dependabot. Não é regressão de código; merge bloqueado por provisão owner-gated.
- **Dívidas autônomas**: 2 de 5 candidatos `.archify/` inválidos (sobreposição de fases/corridor; schema antigo); `docs/architecture/` ausente; seis scripts de gate da factory materializados apenas como diagnóstico (falham por baselines da factory, N/A como autoridade conforme Bloco 0.a).
- **[NV / humano]**: D1 (sessão host+farmhand ao vivo), atestação humana E de Gate 3, execução do protocolo SC-004, tag/release `0.2.0` e canais públicos, `RUNNER_AUDIT_STORE` (#523), segredos do Dependabot e aprovação de revisor distinto.

