# Web research — ecossistema e estado externo (2026-11-05)

> **Escopo:** pesquisa web atualizada para contextualizar `OBJETIVO.md`/`ROADMAP.md`. Foco em fontes primárias/alta confiabilidade. Limitações registradas quando o acesso direto foi bloqueado.
> **Método:** `websearch`/`webfetch` (smapi.io, GitHub releases, JunimoServer docs, wiki SMAPI compat); tentativa de acesso direto ao Nexus (#39740) retornou 403 nesta sessão. `npx skills find research` consultado para descobrir skills de pesquisa disponíveis no ecossistema `skills` (não instaladas/acionadas automaticamente aqui).

## Fatos verificados (alta confiabilidade)

| Tema | Fato verificado | Fonte | Observação |
|---|---|---|---|
| SMAPI (atual) | **SMAPI 4.5.2** é a versão atual publicada; download/links oficiais disponíveis. | https://smapi.io/ ; https://github.com/Pathoschild/SMAPI/releases/tag/4.5.2 | Página e release alinham. |
| Compatibilidade SDV/SMAPI | SMAPI 4.5.x requer **Stardew Valley 1.6.14 ou posterior** em Linux/macOS/Windows/Steam Deck. | https://smapi.io/ (texto “Requires Stardew Valley 1.6.14 or later…”) | Base técnica do projeto (SMAPI 4.5.2 / SDV 1.6.14+) permanece coerente. |
| Notas SMAPI 4.5.2 | 4.5.2: melhorias de performance, correção do instalador Linux/macOS ao salvar esquema de cores em 4.5.0+, atualizações de tradução. | https://smapi.io/ ; release 4.5.2 | Contexto de manutenção; não altera mecânicas do Living Roots. |
| Lista de compatibilidade SMAPI | A lista de compatibilidade de mods em smapi.io **não é mais atualizada de forma abrangente mensalmente**; futuro incerto (“Sunset on the mod compatibility list”). | https://smapi.io/mods | Caveat para consultas de ecossistema: usar também Nexus/GitHub/wikis específicos do mod. |
| JunimoServer (referência) | **JunimoServer** é servidor dedicado always-on para Stardew Valley (Docker/headless, farm 24/7 sem host jogador aberto; clientes vanilla; mods SMAPI suportados no servidor; CLI/VNC/SMAPI console; backups/volumes). Latest release marcada como instável no site; preview builds recomendados. | https://docs.junimoserver.com/ | **Referência arquitetural** para automação/E2E/D1 no futuro; **não é evidência** de comportamento do Living Roots nem cobre seu runner host-only atual. |
| SMAPIDedicatedServerMod (referência comunitária) | Projeto comunitário para servidor dedicado/headless com SMAPI; documentação menciona execução Linux sem GUI via `xvfb-run` (exemplo no README). | https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod | Referência histórica/comunitária; avaliar maturidade/compatibilidade antes de adotar. |

## Contexto competitivo/ecossistema (com limitações)

| Item | Estado nesta pesquisa | Fonte / limitação |
|---|---|---|
| **ReGen Valley** (Nexus #39740, robertsvalley) | Acesso direto ao Nexus bloqueado (HTTP 403) nesta sessão; **não foi possível revalidar** texto/versão/downloads ao vivo. Citações prévias do repo indicam overhaul regenerativo com soil health e efeitos ligados à saúde média da fazenda (distinto do foco per-tile determinístico do Living Roots). | https://www.nexusmods.com/stardewvalley/mods/39740 (bloqueado); ver `OBJETIVO.md`/`ROADMAP.md`/memos anteriores para contexto já registrado. **Tratar como contexto, não prova nova.** |
| Mods adjacentes de qualidade/colheita | Não revalidados individualmente via Nexus nesta sessão (risco de bloqueio/volatilidade). Mantêm-se como *adjacentes* (demanda por qualidade/yield), não equivalentes ao desenho Living Roots (per-tile + loop agroecológico + evidência). | Referências já registradas: Nexus #47141, #51999, #54375 (ver `OBJETIVO.md`). |

## Implicações para o Living Roots (honestas)

1. **Base técnica atualizada:** SMAPI 4.5.2 / SDV 1.6.14+ confirmados como baseline oficial — sem mudança de direção necessária.
2. **D1/multiplayer real:** continua bloqueado por ambiente; JunimoServer/soluções headless são **referências** para investigação futura de automação multi-cliente, não substituem atestação real nem provam FR-009/A8 em Living Roots.
3. **Claims de mercado:** manter diferenciação (coerência per-tile + compost/mulch/crescimento/qualidade + educativo + MP host-authoritative + evidência reproduzível) com honestidade sobre `machine-pass` ≠ `verified` e D1 pendente.
4. **Caveat de fontes voláteis:** Nexus/wiki podem mudar ou bloquear scraping; registrar data/fonte/limitação em atualizações futuras.

*Atualizado: 2026-11-05 (pesquisa web nesta sessão).*