# Multiplayer Live Verification (roadmap D1) — Living Roots

Status: **owed** (não executado) — ver ESC-LR-20261110-005.

## Objetivo

Validar Principle VI (multiplayer first-class) com evidência live: host e farmhands observando valores consistentes de soil health, compost, e machine progress. Stubs (`ThreadSafeGameLoopEventsStub`) atestam o mecanismo mas não são prova live.

## Pré-requisitos

- 2+ instâncias do Stardew Valley com SMAPI + Living Roots (mesmo save, co-op).
- Host (jogador 1) e farmhand (jogador 2).
- Build Release instalado em ambas.

## Procedimento

1. Host: criar fazenda, arar tile (10,10), aplicar composto (saúde → 100).
2. Farmhand: entrar na fazenda, observar tile (10,10) — saúde deve ser 100 (host-authoritative).
3. Farmhand: tentar aplicar agroquímico → deve ser recusado com mensagem (apenas host).
4. Host: aplicar agroquímico → saúde → 90; farmhand observa 90 após sync (FR-009/A8 broadcast).
5. Host: aplicar mulch → farmhand observa mulch state.
6. Host: usar composting bin → farmhand observa progresso.
7. Save + reload em ambas → valores persistentes e consistentes.

## Sinais esperados

- Soil health: host e farmhand mostram o mesmo valor (±0).
- Compost application: farmhand recusado; host aplica; sync via `ModMessageReceived` (SoilHealthSyncMessage).
- Lock contention: segunda ação concorrente recusada com mensagem (inventory intact).

## Evidência a capturar

- Screenshots de ambos os clientes mostrando valores idênticos.
- SMAPI log de ambos (sem erros de sync).
- (Opcional) evidence bundle via `verify-scenario.sh` com cenário multiplayer (futuro).

## Cobertura automatizada existente

A recusa de ações host-only por farmhands **já é coberta** por testes automatizados:

- `ModControllerAgrochemicalTests.*UnsupportedAuthority*` — farmhand recusado antes de mutação.
- Cenário `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json` — runner headless com `PLAYER_ROLE=farmhand` detecta `farmhand_breach`.
- `OnHostSuccess_*`, `OnLockContention_*`, `WithoutLockService_*` — host authority, lock, sync (FR-009).

O que **não** é coberto automaticamente: observação simultânea de valores consistentes entre host e farmhand (requer 2 clientes live).
- Stubs atestam mecanismo (`ModControllerAgrochemicalTests`, `OnHostSuccess_*`, `OnLockContention_*`).
- Constitution Principle VI: "Live multi-client verification is owed to roadmap D1".

## Referências

- `LivingRoots/Controllers/ModController.cs` (FR-009/A8 soil-health MP sync).
- `LivingRoots/Constants.cs` (ModConstants).
- `VERIFICATION_PROTOCOL.md` (Gate 3).
- ESC-LR-20261110-005.
