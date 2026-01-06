# Guia de Testes In-Game - US01-01 e US01-02

## 1. Introdução

Este guia fornece instruções completas e detalhadas para testar in-game as funcionalidades implementadas nas User Stories US01-01 (Persistência de Saúde do Solo) e US01-02 (Visualização de Saúde do Solo) do mod LivingRoots para Stardew Valley.

### O que está sendo testado

-   **US01-01 - Persistência de Saúde do Solo**: Sistema de armazenamento e recuperação de dados de saúde do solo entre sessões de jogo, incluindo carregamento automático, salvamento automático, cache em memória e proteções de segurança.

-   **US01-02 - Visualização de Saúde do Solo**: Sistema de exibição visual da saúde do solo através de tooltips, overlays coloridos, feedback visual ao usar enxada, e configurações personalizáveis de visualização.

### Objetivo dos testes

Garantir que todas as funcionalidades funcionem corretamente em condições normais, edge cases, situações de performance e cenários de segurança, proporcionando uma experiência de jogo fluida e confiável.

---

## 2. Pré-requisitos

### 2.1. Software Necessário

-   **Stardew Valley** (versão 1.6.0 ou superior)
-   **SMAPI** (Stardew Modding API) - versão compatível com o mod
-   **LivingRoots Mod** - versão com US01-01 e US01-02 implementadas
-   **Content Patcher** (se necessário para mods dependentes)

### 2.2. Ambiente de Teste

-   Um save de jogo limpo ou dedicado para testes
-   Acesso ao console de comandos do SMAPI (tecla `F2` por padrão)
-   Ferramentas básicas de jogo: enxada, regador, sementes
-   Múltiplas localizações acessíveis: fazenda, mina, cidade, etc.

### 2.3. Preparação do Save de Teste

**Recomendações:**

-   Criar um novo save especificamente para testes
-   Desbloquear várias localizações (fazenda, mina, cidade, deserto)
-   Ter acesso a ferramentas básicas
-   Ter alguns tiles cultivados e não cultivados para comparação

**Passos de preparação:**

1. Inicie um novo save de jogo
2. Jogue até desbloquear a enxada (disponível desde o início)
3. Cultive alguns tiles na fazenda (cerca de 20-30 tiles)
4. Explore outras localizações (cidade, mina, deserto)
5. Salve o jogo
6. Anote a data/hora do save para referência

### 2.4. Configuração do Mod

Verifique se o arquivo de configuração do mod está configurado adequadamente:

```json
{
    "EnableVisualization": true,
    "OverlayOpacity": 0.5,
    "TooltipEnabled": true,
    "EnableHoeFeedback": true
}
```

### 2.5. Acesso ao Console

-   Pressione `F2` para abrir o console do SMAPI
-   Verifique se o mod LivingRoots está carregado corretamente
-   Teste o comando `lr_version` para confirmar

---

## 3. Comandos Disponíveis

### 3.1. lr_version

Exibe informações sobre a versão do mod e seu UniqueID.

**Sintaxe:**

```
lr_version
```

**Parâmetros:**

-   Nenhum

**Exemplo de uso:**

```
lr_version
```

**Saída esperada:**

```
[LivingsRoots] LivingRoots v1.0.0 (UniqueID: LivingRoots.Mod)
```

**Como usar para testes:**

-   Verifique se o mod está carregado corretamente
-   Confirme a versão antes de iniciar os testes
-   Use para documentar qual versão foi testada

**Teste rápido:**

1. Abra o console com `F2`
2. Digite `lr_version`
3. Verifique se a versão e UniqueID aparecem corretamente
4. Anote a versão para relatórios de bugs

---

## 4. Testes de Persistência (US01-01)

### 4.1. Carregamento Automático de Dados

#### Teste 4.1.1: Carregamento Inicial

**Objetivo:** Verificar se os dados de saúde do solo são carregados corretamente ao iniciar o jogo.

**Passos:**

1. Inicie o Stardew Valley com o mod LivingRoots instalado
2. Carregue um save existente que tenha dados de saúde do solo
3. Abra o console (`F2`) e verifique se não há erros de carregamento
4. Navegue para a fazenda
5. Passe o mouse sobre tiles cultivados

**Resultado Esperado:**

-   O jogo carrega sem erros
-   Dados de saúde do solo são carregados automaticamente
-   Console mostra mensagens de carregamento bem-sucedido
-   Tooltips mostram valores de saúde corretos

**Como Verificar:**

-   Observe o console para mensagens como `[LivingRoots] Loaded X soil health records`
-   Verifique tooltips em tiles conhecidos
-   Compare com valores anteriores (se documentados)

---

#### Teste 4.1.2: Carregamento após Alteração Externa

**Objetivo:** Testar se o mod detecta e carrega alterações feitas externamente nos arquivos de dados.

**Passos:**

1. Carregue um save e anote a saúde de alguns tiles específicos
2. Salve o jogo e feche completamente
3. Abra manualmente o arquivo de dados do mod (localizado na pasta do save)
4. Modifique alguns valores de saúde (mantendo dentro do range 0-100)
5. Salve o arquivo
6. Reinicie o Stardew Valley e carregue o save
7. Verifique os tiles modificados

**Resultado Esperado:**

-   O mod carrega os valores modificados
-   Tooltips refletem os novos valores
-   Console mostra carregamento bem-sucedido

**Como Verificar:**

-   Compare os valores nos tooltips com os modificados manualmente
-   Verifique se não há erros no console

---

### 4.2. Salvamento Automático

#### Teste 4.2.1: Salvamento ao Salvar Jogo

**Objetivo:** Verificar se os dados de saúde do solo são salvos automaticamente antes do jogo salvar.

**Passos:**

1. Carregue um save de teste
2. Use a enxada em alguns tiles para modificar a saúde do solo
3. Anote os valores de saúde dos tiles modificados
4. Salve o jogo (menu ou atalho)
5. Feche o jogo completamente
6. Reinicie e carregue o save
7. Verifique os mesmos tiles

**Resultado Esperado:**

-   Valores de saúde são preservados após recarregar
-   Console mostra mensagem de salvamento antes do save do jogo
-   Não há perda de dados

**Como Verificar:**

-   Compare os valores antes e depois do recarregamento
-   Verifique o console para mensagens de salvamento
-   Confirme que os valores são idênticos

---

#### Teste 4.2.2: Salvamento Automático Contínuo

**Objetivo:** Testar se o salvamento ocorre corretamente em múltiplos ciclos de save/load.

**Passos:**

1. Carregue um save
2. Modifique a saúde de 10 tiles diferentes
3. Salve o jogo
4. Reinicie e carregue
5. Modifique mais 10 tiles
6. Salve novamente
7. Repita o processo 5 vezes
8. Verifique todos os tiles modificados

**Resultado Esperado:**

-   Todos os tiles mantêm seus valores corretos
-   Não há corrupção de dados
-   Console mostra salvamentos bem-sucedidos em cada ciclo

**Como Verificar:**

-   Documente os valores de cada tile em cada ciclo
-   Confirme que não há perda ou corrupção
-   Verifique logs do console

---

### 4.3. Cache em Memória

#### Teste 4.3.1: Performance com Cache

**Objetivo:** Verificar se o cache em memória melhora a performance ao acessar dados frequentemente.

**Passos:**

1. Carregue um save com muitos tiles de saúde do solo (500+ tiles)
2. Mova-se rapidamente entre tiles, passando o mouse sobre eles
3. Observe a fluidez dos tooltips
4. Repita o movimento 10 vezes
5. Anote se há lag ou atrasos perceptíveis

**Resultado Esperado:**

-   Tooltips aparecem instantaneamente
-   Não há lag perceptível
-   Acesso aos dados é rápido e fluido

**Como Verificar:**

-   Cronometre o tempo de resposta dos tooltips
-   Observe se há frame drops
-   Compare com acessos a tiles não cacheados (primeiro acesso)

---

#### Teste 4.3.2: Cache Invalidation

**Objetivo:** Testar se o cache é invalidado corretamente quando os dados são modificados.

**Passos:**

1. Carregue um save
2. Passe o mouse sobre um tile e anote o valor exibido
3. Use a enxada no tile para modificar a saúde
4. Passe o mouse novamente sobre o mesmo tile
5. Verifique se o valor atualizou

**Resultado Esperado:**

-   Valor no tooltip atualiza imediatamente após modificação
-   Cache não retém valores obsoletos
-   Modificações são refletidas instantaneamente

**Como Verificar:**

-   Compare o valor antes e depois da modificação
-   Confirme que o tooltip mostra o novo valor
-   Verifique se não há atraso na atualização

---

### 4.4. Validação de Coordenadas e Valores

#### Teste 4.4.1: Valores Válidos (0-100)

**Objetivo:** Verificar se o sistema aceita e armazena corretamente valores no range permitido.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles para gerar diferentes valores de saúde
3. Observe os tooltips para confirmar os valores
4. Salve e recarregue
5. Verifique se os valores permanecem no range 0-100

**Resultado Esperado:**

-   Todos os valores exibidos estão entre 0 e 100
-   Valores são preservados corretamente após save/load
-   Não há valores negativos ou acima de 100

**Como Verificar:**

-   Documente os valores de vários tiles
-   Confirme que todos estão no range correto
-   Verifique após recarregar

---

#### Teste 4.4.2: Coordenadas Válidas

**Objetivo:** Testar se o sistema valida e armazena coordenadas corretamente.

**Passos:**

1. Carregue um save
2. Navegue para diferentes áreas da fazenda (cantos extremos)
3. Use a enxada em tiles em coordenadas extremas
4. Observe os tooltips
5. Salve e recarregue
6. Verifique os mesmos tiles

**Resultado Esperado:**

-   Coordenadas são armazenadas corretamente
-   Tooltips mostram valores corretos mesmo em posições extremas
-   Não há erros de coordenadas

**Como Verificar:**

-   Anote as coordenadas aproximadas dos tiles testados
-   Confirme que os valores são mantidos após recarregar
-   Verifique se não há erros no console

---

### 4.5. Proteção contra Ataques DoS

#### Teste 4.5.1: Limite de Tiles por Localização (MaxTilesPerLocation: 500)

**Objetivo:** Verificar se o sistema respeita o limite de 500 tiles por localização.

**Passos:**

1. Carregue um save
2. Use a enxada em mais de 500 tiles na mesma localização (fazenda)
3. Observe o comportamento após atingir o limite
4. Verifique o console para mensagens de limite

**Resultado Esperado:**

-   Sistema aceita até 500 tiles por localização
-   Após atingir 500, novos tiles não são registrados ou substituem os mais antigos
-   Console mostra mensagem de aviso sobre limite

**Como Verificar:**

-   Conte quantos tiles foram registrados
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

#### Teste 4.5.2: Limite de Localizações por Save (MaxLocationsPerSave: 50)

**Objetivo:** Testar se o sistema respeita o limite de 50 localizações diferentes por save.

**Passos:**

1. Carregue um save com acesso a muitas localizações
2. Use a enxada em tiles em mais de 50 localizações diferentes
3. Observe o comportamento após atingir o limite
4. Verifique o console para mensagens

**Resultado Esperado:**

-   Sistema aceita até 50 localizações
-   Após atingir 50, novas localizações não são registradas
-   Console mostra mensagem de aviso

**Como Verificar:**

-   Documente quantas localizações foram registradas
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

#### Teste 4.5.3: Limite Total de Tiles (MaxTilesPerSave: 30.000)

**Objetivo:** Verificar se o sistema respeita o limite total de 30.000 tiles por save.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles até atingir 30.000 tiles totais
3. Observe o comportamento após atingir o limite
4. Verifique o console para mensagens

**Resultado Esperado:**

-   Sistema aceita até 30.000 tiles totais
-   Após atingir o limite, novos tiles não são registrados
-   Console mostra mensagem de aviso

**Como Verificar:**

-   Monitore o total de tiles registrados
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

#### Teste 4.5.4: Coordenadas Extremas (MaxAbsoluteTileCoordinate: ±10.000)

**Objetivo:** Testar se o sistema rejeita coordenadas fora do range permitido.

**Passos:**

1. Carregue um save
2. Navegue para áreas extremas do mapa (se acessível)
3. Tente usar a enxada em tiles com coordenadas além de ±10.000
4. Observe o comportamento
5. Verifique o console para mensagens de erro

**Resultado Esperado:**

-   Sistema rejeita coordenadas fora do range
-   Console mostra mensagem de erro
-   Tile não é registrado
-   Sistema continua funcionando normalmente

**Como Verificar:**

-   Verifique mensagens de erro no console
-   Confirme que tiles com coordenadas inválidas não são registrados
-   Observe se o sistema não crasha

---

### 4.6. Sanitização de Nomes de Arquivo

#### Teste 4.6.1: Nomes de Save com Caracteres Especiais

**Objetivo:** Verificar se o sistema sanitiza corretamente nomes de save com caracteres especiais.

**Passos:**

1. Crie um novo save com nome contendo caracteres especiais (ex: "Teste@#$%")
2. Jogue e use a enxada em alguns tiles
3. Salve o jogo
4. Verifique se o arquivo de dados do mod foi criado com nome sanitizado
5. Recarregue o save
6. Verifique se os dados foram carregados corretamente

**Resultado Esperado:**

-   Arquivo de dados é criado com nome sanitizado (sem caracteres especiais perigosos)
-   Dados são carregados corretamente
-   Não há erros de arquivo

**Como Verificar:**

-   Verifique a pasta do save no sistema de arquivos
-   Confirme que o nome do arquivo está sanitizado
-   Verifique se o recarregamento funciona

---

#### Teste 4.6.2: Nomes de Save com Espaços e Unicode

**Objetivo:** Testar se o sistema lida corretamente com espaços e caracteres Unicode.

**Passos:**

1. Crie um save com nome contendo espaços e Unicode (ex: "Fazenda da João Ñ")
2. Jogue e use a enxada em tiles
3. Salve o jogo
4. Verifique o arquivo de dados
5. Recarregue o save

**Resultado Esperado:**

-   Arquivo de dados é criado com nome apropriado
-   Dados são carregados corretamente
-   Não há problemas com codificação

**Como Verificar:**

-   Verifique a pasta do save
-   Confirme que o arquivo existe e pode ser lido
-   Verifique o recarregamento

---

#### Teste 4.6.3: Limite de Tamanho do Nome (MaxSaveIdLength: 200)

**Objetivo:** Verificar se o sistema lida com nomes de save muito longos.

**Passos:**

1. Crie um save com nome muito longo (mais de 200 caracteres)
2. Jogue e use a enxada em tiles
3. Salve o jogo
4. Verifique o arquivo de dados
5. Recarregue o save

**Resultado Esperado:**

-   Sistema trunca ou sanitiza o nome se necessário
-   Arquivo de dados é criado com nome válido
-   Dados são carregados corretamente

**Como Verificar:**

-   Verifique o nome do arquivo criado
-   Confirme que está dentro dos limites do sistema de arquivos
-   Verifique o recarregamento

---

#### Teste 4.6.4: Limite de Tamanho do Nome de Localização (MaxLocationNameLength: 100)

**Objetivo:** Testar se o sistema lida com nomes de localização muito longos.

**Passos:**

1. Carregue um save
2. Navegue para uma localização (se possível com nome longo)
3. Use a enxada em tiles
4. Salve o jogo
5. Verifique o arquivo de dados
6. Recarregue o save

**Resultado Esperado:**

-   Sistema sanitiza ou trunca nomes de localização longos
-   Dados são salvos e carregados corretamente
-   Não há erros

**Como Verificar:**

-   Verifique o conteúdo do arquivo de dados
-   Confirme que os nomes de localização são válidos
-   Verifique o recarregamento

---

## 5. Testes de Visualização (US01-02)

### 5.1. Tooltips de Hover

#### Teste 5.1.1: Exibição Básica de Tooltip

**Objetivo:** Verificar se os tooltips são exibidos corretamente ao passar o mouse sobre tiles.

**Passos:**

1. Carregue um save
2. Navegue para a fazenda
3. Passe o mouse sobre tiles com saúde do solo
4. Observe o tooltip exibido

**Resultado Esperado:**

-   Tooltip aparece ao passar o mouse
-   Mostra valor de saúde do solo (0-100)
-   Mostra status (Pobre/Moderado/Saudável)
-   Tooltip desaparece ao remover o mouse
-   Posição do tooltip segue o cursor

**Como Verificar:**

-   Observe se o tooltip aparece instantaneamente
-   Verifique se as informações estão corretas
-   Confirme que o tooltip não obstrui a visão
-   Teste em diferentes posições do cursor

---

#### Teste 5.1.2: Formato do Tooltip

**Objetivo:** Verificar se o tooltip exibe as informações no formato correto.

**Passos:**

1. Carregue um save
2. Passe o mouse sobre tiles com diferentes níveis de saúde
3. Observe o formato do tooltip para cada caso

**Resultado Esperado:**

-   Formato: "Saúde do Solo: XX (Status)"
-   Exemplos:
    -   "Saúde do Solo: 25 (Pobre)"
    -   "Saúde do Solo: 50 (Moderado)"
    -   "Saúde do Solo: 85 (Saudável)"
-   Números são exibidos com precisão apropriada
-   Status corresponde ao range de saúde

**Como Verificar:**

-   Documente o formato exibido
-   Confirme que está de acordo com o esperado
-   Verifique a consistência entre diferentes tiles

---

#### Teste 5.1.3: Tooltip em Tiles sem Saúde

**Objetivo:** Verificar o comportamento do tooltip em tiles que não têm saúde do solo registrada.

**Passos:**

1. Carregue um save
2. Passe o mouse sobre tiles nunca cultivados
3. Observe se tooltip é exibido

**Resultado Esperado:**

-   Tooltip não é exibido para tiles sem saúde
-   Ou tooltip mostra "Sem dados de saúde"
-   Não há erros ou crashes

**Como Verificar:**

-   Observe se há tooltip em tiles não cultivados
-   Verifique se há mensagens de erro no console
-   Confirme que o comportamento é consistente

---

#### Teste 5.1.4: Performance de Tooltips

**Objetivo:** Verificar se os tooltips não causam lag quando exibidos rapidamente.

**Passos:**

1. Carregue um save com muitos tiles de saúde
2. Mova o mouse rapidamente sobre muitos tiles
3. Observe se há lag ou atraso
4. Repita o movimento 10 vezes

**Resultado Esperado:**

-   Tooltips aparecem e desaparecem suavemente
-   Não há lag perceptível
-   Frame rate permanece estável
-   Não há atraso na atualização

**Como Verificar:**

-   Observe a fluidez da exibição
-   Monitore o frame rate (se possível)
-   Verifique se há stuttering

---

### 5.2. Overlays de Cor em Tiles

#### Teste 5.2.1: Cores de Saúde - Solo Pobre (0-33%)

**Objetivo:** Verificar se tiles com saúde pobre (0-33%) são exibidos com a cor correta.

**Passos:**

1. Carregue um save
2. Identifique tiles com saúde entre 0-33%
3. Observe a cor do overlay
4. Compare com a cor esperada: Marrom avermelhado (#8B4513)

**Resultado Esperado:**

-   Tiles com saúde 0-33% têm overlay marrom avermelhado
-   Cor é claramente distinguível de outras categorias
-   Opacidade é apropriada (não obscurece completamente o tile)

**Como Verificar:**

-   Compare a cor visual com a cor esperada
-   Use ferramentas de captura de cor se disponível
-   Confirme que a cor é consistente entre tiles pobres

---

#### Teste 5.2.2: Cores de Saúde - Solo Moderado (34-66%)

**Objetivo:** Verificar se tiles com saúde moderada (34-66%) são exibidos com a cor correta.

**Passos:**

1. Carregue um save
2. Identifique tiles com saúde entre 34-66%
3. Observe a cor do overlay
4. Compare com a cor esperada: Marrom amarelado (#DAA520)

**Resultado Esperado:**

-   Tiles com saúde 34-66% têm overlay marrom amarelado
-   Cor é claramente distinguível de outras categorias
-   Transição entre pobre e moderado é clara

**Como Verificar:**

-   Compare a cor visual com a cor esperada
-   Confirme que a cor é consistente
-   Verifique a distinção visual

---

#### Teste 5.2.3: Cores de Saúde - Solo Saudável (67-100%)

**Objetivo:** Verificar se tiles com saúde saudável (67-100%) são exibidos com a cor correta.

**Passos:**

1. Carregue um save
2. Identifique tiles com saúde entre 67-100%
3. Observe a cor do overlay
4. Compare com a cor esperada: Marrom esverdeado (#556B2F)

**Resultado Esperado:**

-   Tiles com saúde 67-100% têm overlay marrom esverdeado
-   Cor é claramente distinguível de outras categorias
-   Transição entre moderado e saudável é clara

**Como Verificar:**

-   Compare a cor visual com a cor esperada
-   Confirme que a cor é consistente
-   Verifique a distinção visual

---

#### Teste 5.2.4: Transições de Cor

**Objetivo:** Verificar se as transições entre categorias de cor são claras e intuitivas.

**Passos:**

1. Carregue um save
2. Encontre tiles adjacentes com diferentes categorias de saúde
3. Observe a diferença visual entre eles
4. Verifique se é fácil distinguir as categorias

**Resultado Esperado:**

-   Diferença visual entre categorias é clara
-   Não há confusão entre cores adjacentes
-   Progressão visual (pobre → moderado → saudável) é intuitiva

**Como Verificar:**

-   Compare tiles adjacentes de diferentes categorias
-   Peça a outra pessoa para identificar as categorias visualmente
-   Confirme que as cores são distinguíveis

---

#### Teste 5.2.5: Opacidade do Overlay

**Objetivo:** Verificar se a opacidade do overlay permite ver o tile subjacente.

**Passos:**

1. Carregue um save
2. Observe tiles com overlays de cor
3. Verifique se é possível ver o tile original (grama, terra, etc.)
4. Teste em diferentes condições de iluminação

**Resultado Esperado:**

-   Overlay é transparente o suficiente para ver o tile
-   Cor da saúde é claramente visível
-   Não há obstrução completa do tile

**Como Verificar:**

-   Confirme que você pode identificar o tipo de tile
-   Verifique se a cor de saúde é visível
-   Teste em diferentes áreas do mapa

---

### 5.3. Feedback Visual ao Usar Enxada

#### Teste 5.3.1: Flash Visual

**Objetivo:** Verificar se há um flash visual ao usar a enxada em um tile.

**Passos:**

1. Carregue um save
2. Use a enxada em um tile
3. Observe se há um flash visual
4. Repita em vários tiles

**Resultado Esperado:**

-   Flash visual ocorre ao usar a enxada
-   Flash é visível mas não obstrutivo
-   Cor do flash corresponde à nova saúde do solo
-   Duração do flash é apropriada

**Como Verificar:**

-   Observe se há flash
-   Verifique a cor do flash
-   Confirme que não é muito longo ou muito curto

---

#### Teste 5.3.2: Texto Flutuante

**Objetivo:** Verificar se texto flutuante aparece mostrando a nova saúde do solo.

**Passos:**

1. Carregue um save
2. Use a enxada em um tile
3. Observe se texto flutuante aparece
4. Leia o texto exibido

**Resultado Esperado:**

-   Texto flutuante aparece acima do tile
-   Mostra o novo valor de saúde
-   Texto é legível e visível
-   Texto desaparece após um tempo apropriado

**Como Verificar:**

-   Confirme que o texto aparece
-   Verifique se o valor está correto
-   Observe se o texto é legível
-   Confirme que desaparece apropriadamente

---

#### Teste 5.3.3: Combinação Flash + Texto

**Objetivo:** Verificar se flash e texto flutuante funcionam bem juntos.

**Passos:**

1. Carregue um save
2. Use a enxada em um tile
3. Observe simultaneamente o flash e o texto
4. Verifique se ambos são visíveis e não se sobrepõem

**Resultado Esperado:**

-   Flash e texto aparecem simultaneamente
-   Ambos são claramente visíveis
-   Não há conflito visual
-   Feedback é claro e informativo

**Como Verificar:**

-   Observe se ambos os elementos aparecem
-   Verifique se não há sobreposição problemática
-   Confirme que o feedback é claro

---

#### Teste 5.3.4: Feedback em Diferentes Condições

**Objetivo:** Testar o feedback visual em diferentes condições de saúde inicial.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles com diferentes níveis de saúde inicial
3. Observe o feedback em cada caso
4. Verifique se o feedback é consistente

**Resultado Esperado:**

-   Feedback ocorre independentemente da saúde inicial
-   Flash reflete a nova saúde
-   Texto mostra o novo valor
-   Comportamento é consistente

**Como Verificar:**

-   Teste em tiles pobres, moderados e saudáveis
-   Documente o feedback em cada caso
-   Confirme a consistência

---

### 5.4. Configuração de Visualização

#### Teste 5.4.1: Habilitar/Desabilitar Visualização

**Objetivo:** Verificar se é possível habilitar e desabilitar a visualização.

**Passos:**

1. Abra o arquivo de configuração do mod
2. Altere `EnableVisualization` para `false`
3. Salve a configuração
4. Reinicie o jogo
5. Carregue um save
6. Observe se overlays e tooltips aparecem
7. Repita habilitando a visualização novamente

**Resultado Esperado:**

-   Com `EnableVisualization: false`, não há overlays ou tooltips
-   Com `EnableVisualization: true`, visualização funciona normalmente
-   Mudança é aplicada após reiniciar o jogo

**Como Verificar:**

-   Confirme ausência de visualização quando desabilitado
-   Confirme presença de visualização quando habilitado
-   Verifique se não há erros

---

#### Teste 5.4.2: Ajuste de Opacidade

**Objetivo:** Verificar se a opacidade do overlay pode ser ajustada.

**Passos:**

1. Abra o arquivo de configuração
2. Altere `OverlayOpacity` para diferentes valores (0.1, 0.5, 1.0)
3. Salve a configuração
4. Reinicie o jogo
5. Carregue um save
6. Observe a opacidade dos overlays
7. Repita com outros valores

**Resultado Esperado:**

-   Opacidade muda conforme o valor configurado
-   Valores mais baixos = overlays mais transparentes
-   Valores mais altos = overlays mais opacos
-   Mudança é aplicada após reiniciar

**Como Verificar:**

-   Compare a opacidade visual com o valor configurado
-   Teste múltiplos valores
-   Confirme que a mudança é aplicada

---

#### Teste 5.4.3: Habilitar/Desabilitar Tooltips

**Objetivo:** Verificar se é possível habilitar e desabilitar tooltips.

**Passos:**

1. Abra o arquivo de configuração
2. Altere `TooltipEnabled` para `false`
3. Salve a configuração
4. Reinicie o jogo
5. Carregue um save
6. Passe o mouse sobre tiles
7. Observe se tooltips aparecem
8. Repita habilitando tooltips

**Resultado Esperado:**

-   Com `TooltipEnabled: false`, não há tooltips
-   Com `TooltipEnabled: true`, tooltips funcionam normalmente
-   Overlays continuam funcionando independentemente

**Como Verificar:**

-   Confirme ausência de tooltips quando desabilitado
-   Confirme presença de tooltips quando habilitado
-   Verifique se overlays ainda funcionam

---

#### Teste 5.4.4: Habilitar/Desabilitar Feedback da Enxada

**Objetivo:** Verificar se é possível habilitar e desabilitar o feedback ao usar enxada.

**Passos:**

1. Abra o arquivo de configuração
2. Altere `EnableHoeFeedback` para `false`
3. Salve a configuração
4. Reinicie o jogo
5. Carregue um save
6. Use a enxada em tiles
7. Observe se há flash e texto
8. Repita habilitando o feedback

**Resultado Esperado:**

-   Com `EnableHoeFeedback: false`, não há flash ou texto
-   Com `EnableHoeFeedback: true`, feedback funciona normalmente
-   Outras visualizações continuam funcionando

**Como Verificar:**

-   Confirme ausência de feedback quando desabilitado
-   Confirme presença de feedback quando habilitado
-   Verifique se outras visualizações funcionam

---

### 5.5. Otimizações de Performance

#### Teste 5.5.1: Viewport Culling

**Objetivo:** Verificar se overlays são renderizados apenas para tiles visíveis na tela.

**Passos:**

1. Carregue um save com muitos tiles de saúde (500+)
2. Navegue para uma área com muitos tiles
3. Observe os overlays visíveis
4. Mova-se para uma área diferente
5. Observe se overlays da área anterior desaparecem
6. Volte para a área anterior
7. Verifique se overlays reaparecem

**Resultado Esperado:**

-   Apenas overlays visíveis na tela são renderizados
-   Overlays fora da tela não são renderizados
-   Não há lag ao mover entre áreas
-   Overlays reaparecem ao voltar

**Como Verificar:**

-   Observe se há melhoria de performance
-   Verifique se overlays aparecem/desaparecem apropriadamente
-   Confirme que não há lag

---

#### Teste 5.5.2: Caching de Overlays

**Objetivo:** Verificar se overlays são cacheados para melhorar performance.

**Passos:**

1. Carregue um save
2. Navegue para uma área com overlays
3. Observe a primeira renderização
4. Saia e volte para a mesma área
5. Observe se a renderização é mais rápida
6. Repita 5 vezes

**Resultado Esperado:**

-   Primeira renderização pode ser mais lenta
-   Renderizações subsequentes são mais rápidas
-   Não há lag perceptível após cache
-   Overlays aparecem instantaneamente

**Como Verificar:**

-   Compare o tempo de renderização
-   Observe se há melhoria após primeira renderização
-   Confirme que não há lag

---

#### Teste 5.5.3: Throttling de Atualizações

**Objetivo:** Verificar se as atualizações de visualização são throttled para evitar sobrecarga.

**Passos:**

1. Carregue um save
2. Use a enxada rapidamente em muitos tiles consecutivos
3. Observe se há lag ou atraso
4. Verifique se os feedbacks aparecem em ordem apropriada

**Resultado Esperado:**

-   Não há lag mesmo com ações rápidas
-   Feedbacks aparecem em ordem
-   Sistema não é sobrecarregado
-   Performance permanece estável

**Como Verificar:**

-   Observe se há lag
-   Verifique a ordem dos feedbacks
-   Confirme que o sistema permanece responsivo

---

#### Teste 5.5.4: Performance com Muitos Tiles

**Objetivo:** Testar a performance com uma grande quantidade de tiles de saúde.

**Passos:**

1. Carregue um save com muitos tiles de saúde (idealmente 500+)
2. Navegue rapidamente entre áreas
3. Passe o mouse sobre muitos tiles
4. Use a enxada em vários tiles
5. Observe a performance geral

**Resultado Esperado:**

-   Frame rate permanece estável
-   Não há lag perceptível
-   Tooltips aparecem instantaneamente
-   Feedbacks são exibidos corretamente

**Como Verificar:**

-   Monitore o frame rate (se possível)
-   Observe se há lag ou stuttering
-   Confirme que todas as visualizações funcionam

---

## 6. Testes de Edge Cases

### 6.1. Saves Corrompidos

#### Teste 6.1.1: Arquivo de Dados Corrompido

**Objetivo:** Verificar como o sistema lida com um arquivo de dados corrompido.

**Passos:**

1. Carregue um save e use a enxada em alguns tiles
2. Salve o jogo
3. Feche o jogo
4. Abra manualmente o arquivo de dados do mod
5. Corrompa o arquivo (altere aleatoriamente alguns bytes)
6. Salve o arquivo
7. Reinicie o jogo e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta a corrupção
-   Console mostra mensagem de erro apropriada
-   Mod continua funcionando (pode usar valores padrão ou ignorar dados corrompidos)
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que o jogo não crasha
-   Observe se o mod continua funcionando

---

#### Teste 6.1.2: Arquivo de Dados Ausente

**Objetivo:** Verificar como o sistema lida quando o arquivo de dados não existe.

**Passos:**

1. Carregue um save e use a enxada em tiles
2. Salve o jogo
3. Feche o jogo
4. Exclua o arquivo de dados do mod
5. Reinicie o jogo e carregue o save
6. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta arquivo ausente
-   Console mostra mensagem informativa
-   Mod cria novo arquivo quando necessário
-   Jogo funciona normalmente

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que o jogo não crasha
-   Observe se novo arquivo é criado

---

#### Teste 6.1.3: Save com Dados Inconsistentes

**Objetivo:** Testar se o sistema lida com dados inconsistentes no arquivo.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados manualmente para criar inconsistências (ex: coordenadas inválidas)
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta inconsistências
-   Dados inconsistentes são ignorados ou corrigidos
-   Console mostra mensagens apropriadas
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que dados inconsistentes não causam problemas
-   Observe se o mod continua funcionando

---

### 6.2. Coordenadas Extremas

#### Teste 6.2.1: Coordenadas Negativas Extremas

**Objetivo:** Testar o comportamento com coordenadas negativas extremas.

**Passos:**

1. Carregue um save
2. Navegue para áreas com coordenadas negativas (se acessível)
3. Use a enxada em tiles nessas áreas
4. Observe o comportamento
5. Salve e recarregue
6. Verifique os mesmos tiles

**Resultado Esperado:**

-   Sistema lida com coordenadas negativas
-   Dados são salvos e carregados corretamente
-   Visualizações funcionam normalmente
-   Não há erros

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que dados são salvos/carregados
-   Observe visualizações

---

#### Teste 6.2.2: Coordenadas Positivas Extremas

**Objetivo:** Testar o comportamento com coordenadas positivas extremas.

**Passos:**

1. Carregue um save
2. Navegue para áreas com coordenadas positivas extremas (se acessível)
3. Use a enxada em tiles nessas áreas
4. Observe o comportamento
5. Salve e recarregue
6. Verifique os mesmos tiles

**Resultado Esperado:**

-   Sistema lida com coordenadas positivas extremas
-   Dados são salvos e carregados corretamente
-   Visualizações funcionam normalmente
-   Não há erros

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que dados são salvos/carregados
-   Observe visualizações

---

#### Teste 6.2.3: Coordenadas Fora do Range Permitido

**Objetivo:** Verificar se o sistema rejeita coordenadas fora do range ±10.000.

**Passos:**

1. Carregue um save
2. Se possível, navegue para áreas além de ±10.000 (pode requerer mods ou cheats)
3. Tente usar a enxada em tiles nessas áreas
4. Observe o comportamento
5. Verifique o console

**Resultado Esperado:**

-   Sistema rejeita coordenadas fora do range
-   Console mostra mensagem de erro
-   Tile não é registrado
-   Sistema continua funcionando

**Como Verificar:**

-   Verifique mensagens de erro no console
-   Confirme que tiles não são registrados
-   Observe se o sistema não crasha

---

### 6.3. Valores Inválidos

#### Teste 6.3.1: Valores Negativos

**Objetivo:** Verificar como o sistema lida com valores negativos de saúde.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir valores negativos
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta valores negativos
-   Valores negativos são corrigidos para 0 ou ignorados
-   Console mostra mensagem de aviso
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que valores negativos não são exibidos
-   Observe se tooltips mostram valores válidos

---

#### Teste 6.3.2: Valores Acima de 100

**Objetivo:** Verificar como o sistema lida com valores acima de 100.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir valores acima de 100
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta valores acima de 100
-   Valores são corrigidos para 100 ou ignorados
-   Console mostra mensagem de aviso
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que valores acima de 100 não são exibidos
-   Observe se tooltips mostram valores válidos

---

#### Teste 6.3.3: Valores Não Numéricos

**Objetivo:** Verificar como o sistema lida com valores não numéricos.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir valores não numéricos (ex: "abc")
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta valores não numéricos
-   Valores inválidos são ignorados
-   Console mostra mensagem de erro
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens de erro no console
-   Confirme que valores inválidos não causam problemas
-   Observe se o mod continua funcionando

---

### 6.4. Múltiplos Saves

#### Teste 6.4.1: Alternância entre Saves

**Objetivo:** Verificar se o sistema lida corretamente com alternância entre diferentes saves.

**Passos:**

1. Carregue o Save A
2. Use a enxada em tiles e anote os valores
3. Salve o jogo
4. Volte ao menu principal
5. Carregue o Save B
6. Use a enxada em tiles diferentes
7. Salve o jogo
8. Volte ao menu principal
9. Carregue o Save A novamente
10. Verifique se os valores estão corretos

**Resultado Esperado:**

-   Cada save mantém seus próprios dados
-   Não há mistura de dados entre saves
-   Carregamento funciona corretamente após alternância
-   Console não mostra erros

**Como Verificar:**

-   Compare os valores de cada save
-   Confirme que não há mistura de dados
-   Verifique mensagens no console

---

#### Teste 6.4.2: Múltiplos Saves com Mesmo Nome

**Objetivo:** Verificar se o sistema lida com saves com nomes idênticos.

**Passos:**

1. Crie dois saves com o mesmo nome (em pastas diferentes)
2. Carregue o primeiro save
3. Use a enxada em tiles
4. Salve o jogo
5. Volte ao menu
6. Carregue o segundo save
7. Use a enxada em tiles
8. Salve o jogo
9. Alternne entre os saves
10. Verifique se os dados estão corretos

**Resultado Esperado:**

-   Cada save mantém seus próprios dados
-   Não há conflito entre saves com mesmo nome
-   Sistema usa identificadores únicos internos
-   Dados não são misturados

**Como Verificar:**

-   Compare os dados de cada save
-   Confirme que não há conflito
-   Verifique mensagens no console

---

### 6.5. Múltiplas Localizações

#### Teste 6.5.1: Dados em Múltiplas Localizações

**Objetivo:** Verificar se o sistema lida com dados em múltiplas localizações simultaneamente.

**Passos:**

1. Carregue um save
2. Navegue para a fazenda
3. Use a enxada em 20 tiles
4. Navegue para a cidade
5. Use a enxada em 20 tiles
6. Navegue para a mina
7. Use a enxada em 20 tiles
8. Salve o jogo
9. Recarregue o save
10. Verifique os tiles em todas as localizações

**Resultado Esperado:**

-   Dados de todas as localizações são salvos
-   Dados são carregados corretamente para cada localização
-   Não há mistura de dados entre localizações
-   Visualizações funcionam em todas as localizações

**Como Verificar:**

-   Verifique tiles em cada localização
-   Confirme que os valores estão corretos
-   Observe visualizações em cada localização

---

#### Teste 6.5.2: Alternância Rápida entre Localizações

**Objetivo:** Testar o comportamento ao alternar rapidamente entre localizações.

**Passos:**

1. Carregue um save com dados em múltiplas localizações
2. Navegue rapidamente entre fazenda, cidade e mina
3. Observe as visualizações em cada localização
4. Repita a alternância 10 vezes
5. Verifique se há lag ou erros

**Resultado Esperado:**

-   Visualizações aparecem corretamente em cada localização
-   Não há lag ao alternar
-   Dados são carregados/descarregados corretamente
-   Console não mostra erros

**Como Verificar:**

-   Observe a performance ao alternar
-   Verifique visualizações em cada localização
-   Confirme que não há erros

---

### 6.6. Condições de Jogo Especiais

#### Teste 6.6.1: Teste durante Eventos

**Objetivo:** Verificar se o sistema funciona durante eventos especiais do jogo.

**Passos:**

1. Carregue um save
2. Participe de um evento (festival, casamento, etc.)
3. Durante o evento, observe se há algum comportamento inesperado
4. Após o evento, verifique se os dados estão corretos

**Resultado Esperado:**

-   Sistema não interfere com eventos
-   Dados permanecem corretos após eventos
-   Não há erros ou crashes
-   Visualizações funcionam normalmente após eventos

**Como Verificar:**

-   Observe o comportamento durante eventos
-   Verifique dados após eventos
-   Confirme que não há erros

---

#### Teste 6.6.2: Teste em Diferentes Horários do Dia

**Objetivo:** Verificar se o sistema funciona em diferentes horários do dia (iluminação).

**Passos:**

1. Carregue um save
2. Teste as visualizações de manhã (6:00-12:00)
3. Teste as visualizações à tarde (12:00-18:00)
4. Teste as visualizações à noite (18:00-24:00)
5. Teste as visualizações de madrugada (0:00-6:00)
6. Observe se as cores são visíveis em todos os horários

**Resultado Esperado:**

-   Visualizações são visíveis em todos os horários
-   Cores permanecem distinguíveis
-   Não há problemas de visibilidade
-   Performance é consistente

**Como Verificar:**

-   Observe visualizações em cada horário
-   Confirme que as cores são visíveis
-   Verifique se há problemas de contraste

---

#### Teste 6.6.3: Teste em Diferentes Estações

**Objetivo:** Verificar se o sistema funciona em diferentes estações do jogo.

**Passos:**

1. Carregue um save na primavera
2. Use a enxada em tiles
3. Salve o jogo
4. Avance para o verão e recarregue
5. Verifique os tiles
6. Repita para outono e inverno
7. Observe se as visualizações funcionam em todas as estações

**Resultado Esperado:**

-   Dados são preservados entre estações
-   Visualizações funcionam em todas as estações
-   Cores permanecem distinguíveis
-   Não há problemas específicos de estação

**Como Verificar:**

-   Verifique dados em cada estação
-   Observe visualizações em cada estação
-   Confirme que não há problemas

---

## 7. Testes de Performance

### 7.1. Performance com Muitos Tiles

#### Teste 7.1.1: 100 Tiles

**Objetivo:** Testar a performance com 100 tiles de saúde do solo.

**Passos:**

1. Carregue um save com 100 tiles de saúde
2. Navegue pela área
3. Passe o mouse sobre os tiles
4. Use a enxada em alguns tiles
5. Observe a performance geral

**Resultado Esperado:**

-   Frame rate permanece estável (60 FPS ideal)
-   Não há lag perceptível
-   Tooltips aparecem instantaneamente
-   Feedbacks são exibidos sem atraso

**Como Verificar:**

-   Monitore o frame rate (se possível)
-   Observe se há lag
-   Cronometre o tempo de resposta dos tooltips

---

#### Teste 7.1.2: 500 Tiles

**Objetivo:** Testar a performance com 500 tiles de saúde do solo.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Navegue pela área
3. Passe o mouse sobre os tiles
4. Use a enxada em alguns tiles
5. Observe a performance geral

**Resultado Esperado:**

-   Frame rate permanece estável
-   Pode haver pequeno lag inicial ao carregar
-   Tooltips aparecem rapidamente
-   Feedbacks são exibidos sem atraso significativo

**Como Verificar:**

-   Monitore o frame rate
-   Observe se há lag
-   Compare com o teste de 100 tiles

---

#### Teste 7.1.3: 1000 Tiles

**Objetivo:** Testar a performance com 1000 tiles de saúde do solo.

**Passos:**

1. Carregue um save com 1000 tiles de saúde
2. Navegue pela área
3. Passe o mouse sobre os tiles
4. Use a enxada em alguns tiles
5. Observe a performance geral

**Resultado Esperado:**

-   Frame rate pode diminuir ligeiramente
-   Pode haver lag inicial ao carregar
-   Tooltips aparecem com pequeno atraso
-   Sistema permanece jogável

**Como Verificar:**

-   Monitore o frame rate
-   Observe se há lag
-   Avalie se a performance é aceitável

---

### 7.2. Performance de Salvamento/Carregamento

#### Teste 7.2.1: Tempo de Salvamento

**Objetivo:** Medir o tempo necessário para salvar dados de saúde do solo.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Use um cronômetro
3. Salve o jogo
4. Anote o tempo total de salvamento
5. Repita 5 vezes
6. Calcule a média

**Resultado Esperado:**

-   Salvamento é rápido (menos de 1 segundo idealmente)
-   Tempo é consistente entre tentativas
-   Não há travamentos durante o salvamento
-   Console mostra progresso

**Como Verificar:**

-   Compare os tempos de salvamento
-   Verifique se há variações significativas
-   Confirme que o salvamento é rápido

---

#### Teste 7.2.2: Tempo de Carregamento

**Objetivo:** Medir o tempo necessário para carregar dados de saúde do solo.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Use um cronômetro
3. Anote o tempo total de carregamento
4. Repita 5 vezes
5. Calcule a média

**Resultado Esperado:**

-   Carregamento é rápido (menos de 2 segundos idealmente)
-   Tempo é consistente entre tentativas
-   Não há travamentos durante o carregamento
-   Console mostra progresso

**Como Verificar:**

-   Compare os tempos de carregamento
-   Verifique se há variações significativas
-   Confirme que o carregamento é rápido

---

#### Teste 7.2.3: Impacto no Tempo de Save/Load do Jogo

**Objetivo:** Verificar o impacto do mod no tempo de save/load do jogo.

**Passos:**

1. Carregue um save sem dados do mod (ou desabilite o mod)
2. Salve o jogo e anote o tempo
3. Carregue o jogo e anote o tempo
4. Habilite o mod e carregue o mesmo save
5. Use a enxada em 500 tiles
6. Salve o jogo e anote o tempo
7. Carregue o jogo e anote o tempo
8. Compare os tempos

**Resultado Esperado:**

-   Impacto no tempo de save é mínimo (menos de 0.5 segundos adicional)
-   Impacto no tempo de load é mínimo (menos de 1 segundo adicional)
-   Diferença é aceitável para o usuário

**Como Verificar:**

-   Compare os tempos com e sem o mod
-   Calcule a diferença
-   Avalie se o impacto é aceitável

---

### 7.3. Performance de Visualização

#### Teste 7.3.1: Renderização de Overlays

**Objetivo:** Testar a performance de renderização de overlays.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Navegue para uma área com muitos overlays
3. Observe o frame rate
4. Mova-se rapidamente pela área
5. Observe se há drops de frame

**Resultado Esperado:**

-   Frame rate permanece estável
-   Não há drops significativos de frame
-   Overlays são renderizados suavemente
-   Viewport culling funciona corretamente

**Como Verificar:**

-   Monitore o frame rate
-   Observe se há stuttering
-   Confirme que a renderização é suave

---

#### Teste 7.3.2: Exibição de Tooltips

**Objetivo:** Testar a performance de exibição de tooltips.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Passe o mouse rapidamente sobre muitos tiles
3. Observe se há lag nos tooltips
4. Repita 10 vezes
5. Anote se há problemas

**Resultado Esperado:**

-   Tooltips aparecem instantaneamente
-   Não há lag ao mover o mouse
-   Caching funciona corretamente
-   Performance é consistente

**Como Verificar:**

-   Observe a velocidade de exibição
-   Verifique se há atrasos
-   Confirme que o caching funciona

---

#### Teste 7.3.3: Feedback Visual da Enxada

**Objetivo:** Testar a performance do feedback visual ao usar a enxada.

**Passos:**

1. Carregue um save
2. Use a enxada rapidamente em 50 tiles consecutivos
3. Observe se há lag ou atraso nos feedbacks
4. Repita 5 vezes
5. Anote se há problemas

**Resultado Esperado:**

-   Feedbacks aparecem instantaneamente
-   Não há lag ao usar a enxada rapidamente
-   Throttling funciona corretamente
-   Performance é consistente

**Como Verificar:**

-   Observe a velocidade dos feedbacks
-   Verifique se há atrasos
-   Confirme que não há lag

---

### 7.4. Uso de Memória

#### Teste 7.4.1: Uso de Memória com Poucos Tiles

**Objetivo:** Verificar o uso de memória com poucos tiles de saúde.

**Passos:**

1. Carregue um save com 50 tiles de saúde
2. Use um monitor de memória (se disponível)
3. Anote o uso de memória do jogo
4. Navegue pelo jogo por 10 minutos
5. Anote o uso de memória novamente

**Resultado Esperado:**

-   Uso de memória é baixo
-   Não há vazamento de memória
-   Uso de memória permanece estável
-   Cache em memória funciona corretamente

**Como Verificar:**

-   Compare o uso de memória inicial e final
-   Verifique se há aumento significativo
-   Confirme que não há vazamento

---

#### Teste 7.4.2: Uso de Memória com Muitos Tiles

**Objetivo:** Verificar o uso de memória com muitos tiles de saúde.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Use um monitor de memória (se disponível)
3. Anote o uso de memória do jogo
4. Navegue pelo jogo por 10 minutos
5. Anote o uso de memória novamente

**Resultado Esperado:**

-   Uso de memória é moderado
-   Não há vazamento de memória
-   Uso de memória permanece estável
-   Cache em memória funciona corretamente

**Como Verificar:**

-   Compare o uso de memória inicial e final
-   Verifique se há aumento significativo
-   Confirme que não há vazamento

---

#### Teste 7.4.3: Uso de Memória ao Longo do Tempo

**Objetivo:** Verificar se há vazamento de memória ao jogar por um longo período.

**Passos:**

1. Carregue um save com 500 tiles de saúde
2. Use um monitor de memória (se disponível)
3. Anote o uso de memória inicial
4. Jogue por 30 minutos (navegue, use enxada, salve, etc.)
5. Anote o uso de memória a cada 10 minutos
6. Compare os valores

**Resultado Esperado:**

-   Uso de memória permanece estável
-   Não há aumento contínuo de memória
-   Cache é gerenciado corretamente
-   Não há vazamento de memória

**Como Verificar:**

-   Compare os valores de memória ao longo do tempo
-   Verifique se há tendência de aumento
-   Confirme que não há vazamento

---

## 8. Testes de Segurança

### 8.1. Validação de Entrada

#### Teste 8.1.1: Validação de Valores de Saúde

**Objetivo:** Verificar se o sistema valida corretamente os valores de saúde.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir valores inválidos (negativos, acima de 100, não numéricos)
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta valores inválidos
-   Valores inválidos são corrigidos ou ignorados
-   Console mostra mensagens de aviso
-   Jogo não crasha
-   Dados válidos são carregados corretamente

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que valores inválidos não são exibidos
-   Observe se o jogo funciona normalmente

---

#### Teste 8.1.2: Validação de Coordenadas

**Objetivo:** Verificar se o sistema valida corretamente as coordenadas.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir coordenadas inválidas (fora do range ±10.000)
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta coordenadas inválidas
-   Coordenadas inválidas são ignoradas
-   Console mostra mensagens de erro
-   Jogo não crasha
-   Dados válidos são carregados corretamente

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que tiles com coordenadas inválidas não aparecem
-   Observe se o jogo funciona normalmente

---

#### Teste 8.1.3: Validação de Nomes de Localização

**Objetivo:** Verificar se o sistema valida corretamente os nomes de localização.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Edite o arquivo de dados para incluir nomes de localização inválidos (muito longos, com caracteres perigosos)
6. Salve o arquivo
7. Reinicie e carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema sanitiza nomes de localização
-   Nomes inválidos são corrigidos
-   Console mostra mensagens de aviso
-   Jogo não crasha
-   Dados são carregados corretamente

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que nomes são sanitizados
-   Observe se o jogo funciona normalmente

---

### 8.2. Proteção contra DoS

#### Teste 8.2.1: Limite de Tiles por Localização

**Objetivo:** Verificar se o sistema respeita o limite de 500 tiles por localização.

**Passos:**

1. Carregue um save
2. Use a enxada em mais de 500 tiles na mesma localização
3. Observe o comportamento após atingir 500 tiles
4. Verifique o console para mensagens
5. Tente usar a enxada em mais tiles

**Resultado Esperado:**

-   Sistema aceita até 500 tiles
-   Após atingir 500, novos tiles não são registrados ou substituem os mais antigos
-   Console mostra mensagem de aviso
-   Sistema não crasha
-   Performance permanece estável

**Como Verificar:**

-   Conte quantos tiles foram registrados
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

#### Teste 8.2.2: Limite de Localizações por Save

**Objetivo:** Verificar se o sistema respeita o limite de 50 localizações por save.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles em mais de 50 localizações diferentes
3. Observe o comportamento após atingir 50 localizações
4. Verifique o console para mensagens
5. Tente usar a enxada em novas localizações

**Resultado Esperado:**

-   Sistema aceita até 50 localizações
-   Após atingir 50, novas localizações não são registradas
-   Console mostra mensagem de aviso
-   Sistema não crasha
-   Performance permanece estável

**Como Verificar:**

-   Conte quantas localizações foram registradas
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

#### Teste 8.2.3: Limite Total de Tiles

**Objetivo:** Verificar se o sistema respeita o limite de 30.000 tiles por save.

**Passos:**

1. Carregue um save
2. Use a enxada em muitos tiles até atingir 30.000
3. Observe o comportamento após atingir o limite
4. Verifique o console para mensagens
5. Tente usar a enxada em mais tiles

**Resultado Esperado:**

-   Sistema aceita até 30.000 tiles
-   Após atingir o limite, novos tiles não são registrados
-   Console mostra mensagem de aviso
-   Sistema não crasha
-   Performance permanece estável

**Como Verificar:**

-   Monitore o total de tiles registrados
-   Verifique mensagens no console
-   Confirme que o sistema não crasha

---

### 8.3. Sanitização de Arquivos

#### Teste 8.3.1: Sanitização de Nomes de Arquivo

**Objetivo:** Verificar se o sistema sanitiza corretamente nomes de arquivo.

**Passos:**

1. Crie um save com nome contendo caracteres especiais perigosos (ex: "../../../malicioso")
2. Use a enxada em tiles
3. Salve o jogo
4. Verifique o nome do arquivo de dados criado
5. Confirme que o nome está sanitizado

**Resultado Esperado:**

-   Nome do arquivo está sanitizado
-   Caracteres perigosos são removidos ou substituídos
-   Arquivo é criado em local seguro
-   Não há path traversal

**Como Verificar:**

-   Verifique o nome do arquivo no sistema de arquivos
-   Confirme que está sanitizado
-   Verifique se o arquivo está no local correto

---

#### Teste 8.3.2: Prevenção de Path Traversal

**Objetivo:** Verificar se o sistema previne ataques de path traversal.

**Passos:**

1. Tente criar um save com nome contendo path traversal (ex: "../../etc/passwd")
2. Use a enxada em tiles
3. Salve o jogo
4. Verifique onde o arquivo de dados é criado
5. Confirme que não houve path traversal

**Resultado Esperado:**

-   Sistema previne path traversal
-   Arquivo é criado no local correto
-   Não há acesso a diretórios não autorizados
-   Console mostra mensagem de erro se tentativa é detectada

**Como Verificar:**

-   Verifique o local do arquivo criado
-   Confirme que está no diretório do save
-   Verifique mensagens no console

---

#### Teste 8.3.3: Sanitização de Unicode

**Objetivo:** Verificar se o sistema lida corretamente com caracteres Unicode.

**Passos:**

1. Crie um save com nome contendo caracteres Unicode especiais (ex: homoglyphs)
2. Use a enxada em tiles
3. Salve o jogo
4. Verifique o nome do arquivo de dados
5. Confirme que o nome é seguro

**Resultado Esperado:**

-   Sistema normaliza Unicode
-   Caracteres perigosos são tratados
-   Arquivo é criado com nome seguro
-   Não há problemas de codificação

**Como Verificar:**

-   Verifique o nome do arquivo
-   Confirme que está normalizado
-   Verifique se não há problemas de codificação

---

### 8.4. Proteção de Dados

#### Teste 8.4.1: Isolamento entre Saves

**Objetivo:** Verificar se os dados de diferentes saves são isolados.

**Passos:**

1. Carregue o Save A
2. Use a enxada em tiles e anote os valores
3. Salve o jogo
4. Volte ao menu
5. Carregue o Save B
6. Use a enxada em tiles diferentes
7. Salve o jogo
8. Verifique os arquivos de dados de cada save
9. Confirme que não há mistura de dados

**Resultado Esperado:**

-   Cada save tem seu próprio arquivo de dados
-   Não há mistura de dados entre saves
-   Dados são isolados corretamente
-   Não há vazamento de dados entre saves

**Como Verificar:**

-   Verifique os arquivos de dados de cada save
-   Confirme que são separados
-   Verifique o conteúdo de cada arquivo

---

#### Teste 8.4.2: Proteção contra Modificação Externa

**Objetivo:** Verificar se o sistema lida com modificações externas nos arquivos de dados.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo
4. Feche o jogo
5. Modifique o arquivo de dados externamente
6. Reinicie o jogo
7. Carregue o save
8. Observe o comportamento

**Resultado Esperado:**

-   Sistema detecta modificações externas
-   Dados são validados ao carregar
-   Modificações inválidas são rejeitadas
-   Console mostra mensagens apropriadas
-   Jogo não crasha

**Como Verificar:**

-   Verifique mensagens no console
-   Confirme que modificações inválidas são rejeitadas
-   Observe se o jogo funciona normalmente

---

#### Teste 8.4.3: Backup Automático

**Objetivo:** Verificar se o sistema cria backups dos dados.

**Passos:**

1. Carregue um save
2. Use a enxada em tiles
3. Salve o jogo várias vezes
4. Verifique se há backups dos arquivos de dados
5. Confirme que os backups são criados corretamente

**Resultado Esperado:**

-   Sistema cria backups dos dados
-   Backups são criados antes de sobrescrever
-   Backups são mantidos por um período apropriado
-   Backups podem ser usados para recuperação

**Como Verificar:**

-   Verifique a pasta do save por arquivos de backup
-   Confirme que existem backups
-   Verifique o conteúdo dos backups

---

## 9. Melhores Práticas para Testar Mods

### 9.1. Preparação do Ambiente

#### 9.1.1. Use Saves Dedicados para Testes

**Por que é importante:**

-   Evita corromper saves de jogo principal
-   Permite testar de forma mais agressiva
-   Facilita a repetição de testes
-   Mantém o jogo principal limpo

**Como fazer:**

1. Crie um novo save especificamente para testes
2. Use um nome claro (ex: "Teste_LivingRoots")
3. Mantenha este save separado dos saves de jogo
4. Use este save para todos os testes

**Dicas:**

-   Mantenha múltiplos saves de teste para diferentes cenários
-   Documente o propósito de cada save
-   Faça backup dos saves de teste regularmente

---

#### 9.1.2. Mantenha o Console Aberto

**Por que é importante:**

-   Permite ver mensagens de erro e aviso
-   Ajuda a identificar problemas rapidamente
-   Facilita o debug de issues
-   Fornece informações para relatórios de bugs

**Como fazer:**

1. Mantenha o console do SMAPI aberto (`F2`)
2. Minimize mas não feche o console
3. Verifique o console regularmente durante testes
4. Anote mensagens de erro ou aviso

**Dicas:**

-   Use scroll do console para ver mensagens anteriores
-   Copie mensagens de erro para relatórios
-   Verifique o console após cada ação importante

---

#### 9.1.3. Documente os Testes

**Por que é importante:**

-   Permite reproduzir bugs
-   Facilita comunicação com desenvolvedores
-   Ajuda a identificar padrões
-   Mantém registro do que foi testado

**Como fazer:**

1. Crie um documento de teste
2. Anote cada teste realizado
3. Documente resultados esperados e obtidos
4. Tire screenshots de bugs ou comportamentos inesperados

**Dicas:**

-   Use templates para documentação de testes
-   Inclua data/hora de cada teste
-   Anote a versão do mod testada
-   Documente o ambiente (sistema operacional, versão do jogo, etc.)

---

### 9.2. Execução de Testes

#### 9.2.1. Teste de Forma Sistemática

**Por que é importante:**

-   Garante que todas as funcionalidades são testadas
-   Evita esquecer testes importantes
-   Permite identificar problemas de forma mais eficiente
-   Facilita a repetição de testes

**Como fazer:**

1. Siga a ordem dos testes neste guia
2. Complete cada teste antes de avançar
3. Marque testes como concluídos
4. Volte e repita testes se necessário

**Dicas:**

-   Use um checklist para acompanhar o progresso
-   Não pule testes mesmo que pareçam simples
-   Anote testes que falharam para revisão posterior
-   Priorize testes críticos (persistência, segurança)

---

#### 9.2.2. Teste em Diferentes Condições

**Por que é importante:**

-   Identifica problemas específicos de certas condições
-   Garante que o mod funciona em todos os cenários
-   Previne bugs em situações incomuns
-   Melhora a qualidade geral do mod

**Como fazer:**

1. Teste em diferentes horários do dia
2. Teste em diferentes estações
3. Teste em diferentes localizações
4. Teste com diferentes quantidades de dados

**Dicas:**

-   Documente as condições de cada teste
-   Compare resultados entre diferentes condições
-   Identifique padrões de problemas
-   Reporte problemas específicos de condições

---

#### 9.2.3. Teste Edge Cases

**Por que é importante:**

-   Identifica problemas em situações extremas
-   Previne crashes em cenários incomuns
-   Melhora a robustez do mod
-   Descobre bugs que testes normais não encontram

**Como fazer:**

1. Teste com valores extremos (muito altos, muito baixos)
2. Teste com coordenadas extremas
3. Teste com saves corrompidos
4. Teste com ações rápidas e repetitivas

**Dicas:**

-   Não tenha medo de testar situações incomuns
-   Documente edge cases testados
-   Reporte comportamentos inesperados mesmo que não causem crashes
-   Considere edge cases que usuários podem encontrar

---

### 9.3. Relato de Bugs

#### 9.3.1. Relate Bugs de Forma Detalhada

**Por que é importante:**

-   Facilita a reprodução do bug
-   Ajuda desenvolvedores a identificar a causa
-   Aumenta a chance de o bug ser corrigido
-   Fornece contexto para a correção

**Como fazer:**

1. Descreva o bug em detalhes
2. Inclua passos para reproduzir
3. Forneça screenshots ou logs
4. Documente o ambiente e versões

**Dicas:**

-   Use templates de relato de bugs
-   Inclua mensagens do console
-   Especifique a versão do mod e do jogo
-   Descreva o comportamento esperado vs. obtido

---

#### 9.3.2. Forneça Contexto Adequado

**Por que é importante:**

-   Ajuda a entender o contexto do bug
-   Permite identificar a causa raiz
-   Facilita a priorização da correção
-   Fornece informações para testes de regressão

**Como fazer:**

1. Descreva o que estava fazendo antes do bug
2. Inclua informações sobre o save usado
3. Liste outros mods instalados
4. Descreva o ambiente de teste

**Dicas:**

-   Seja específico sobre as condições
-   Inclua informações sobre configurações
-   Mencione se o bug é reproduzível
-   Descreva a frequência do bug

---

#### 9.3.3. Use Canais Apropriados

**Por que é importante:**

-   Garante que o relato seja visto pelos desenvolvedores
-   Permite acompanhamento do bug
-   Facilita comunicação
-   Mantém organização dos relatos

**Como fazer:**

1. Use issues do GitHub para relatos de bugs
2. Siga templates de issue
3. Inclua etiquetas apropriadas
4. Responda a perguntas dos desenvolvedores

**Dicas:**

-   Verifique se o bug já foi relatado
-   Use um título descritivo para a issue
-   Inclua links para testes relacionados
-   Mantenha a issue atualizada com novas informações

---

### 9.4. Melhoria Contínua

#### 9.4.1. Aprenda com os Testes

**Por que é importante:**

-   Melhora a qualidade dos testes futuros
-   Identifica áreas que precisam de mais atenção
-   Desenvolve habilidades de teste
-   Contribui para a melhoria do mod

**Como fazer:**

1. Revise os testes realizados
2. Identifique áreas que poderiam ser testadas melhor
3. Sugira novos testes
4. Compartilhe experiências com outros testadores

**Dicas:**

-   Documente lições aprendidas
-   Sugira melhorias no guia de testes
-   Participe de discussões sobre testes
-   Contribua com casos de teste adicionais

---

#### 9.4.2. Mantenha-se Atualizado

**Por que é importante:**

-   Permite testar novas funcionalidades
-   Garante que testes estão atualizados
-   Identifica regressões em novas versões
-   Contribui para a melhoria contínua

**Como fazer:**

1. Acompanhe as atualizações do mod
2. Leia notas de release
3. Teste novas versões assim que disponíveis
4. Atualize o guia de testes conforme necessário

**Dicas:**

-   Inscreva-se em notificações de release
-   Participe de discussões sobre desenvolvimento
-   Teste funcionalidades em desenvolvimento
-   Reporte regressões em novas versões

---

#### 9.4.3. Colabore com a Comunidade

**Por que é importante:**

-   Melhora a qualidade do mod para todos
-   Permite aprender com outros testadores
-   Aumenta a cobertura de testes
-   Fortalece a comunidade do mod

**Como fazer:**

1. Compartilhe resultados de testes
2. Ajude outros testadores
3. Contribua com melhorias no guia
4. Participe de discussões

**Dicas:**

-   Seja respeitoso e construtivo
-   Compartilhe conhecimento e experiências
-   Ajude novos testadores
-   Contribua com a documentação

---

## 10. Checklist de Validação Final

### 10.1. Checklist de Funcionalidades Principais

#### US01-01 - Persistência de Saúde do Solo

-   [ ] **Carregamento Automático**

    -   [ ] Dados são carregados ao iniciar o jogo
    -   [ ] Console mostra mensagens de carregamento
    -   [ ] Não há erros ao carregar
    -   [ ] Dados são carregados corretamente após save/load

-   [ ] **Salvamento Automático**

    -   [ ] Dados são salvos antes do jogo salvar
    -   [ ] Console mostra mensagens de salvamento
    -   [ ] Não há erros ao salvar
    -   [ ] Dados são preservados após save/load

-   [ ] **Cache em Memória**

    -   [ ] Cache melhora a performance
    -   [ ] Cache é invalidado corretamente
    -   [ ] Não há valores obsoletos no cache
    -   [ ] Acesso aos dados é rápido

-   [ ] **Validação de Coordenadas e Valores**

    -   [ ] Valores no range 0-100 são aceitos
    -   [ ] Coordenadas são validadas corretamente
    -   [ ] Valores inválidos são rejeitados
    -   [ ] Coordenadas inválidas são rejeitadas

-   [ ] **Proteção contra DoS**

    -   [ ] Limite de 500 tiles por localização é respeitado
    -   [ ] Limite de 50 localizações por save é respeitado
    -   [ ] Limite de 30.000 tiles por save é respeitado
    -   [ ] Sistema não crasha ao atingir limites

-   [ ] **Sanitização de Nomes de Arquivo**
    -   [ ] Nomes com caracteres especiais são sanitizados
    -   [ ] Nomes com espaços e Unicode são tratados
    -   [ ] Nomes muito longos são truncados
    -   [ ] Nomes de localização são sanitizados

---

#### US01-02 - Visualização de Saúde do Solo

-   [ ] **Tooltips de Hover**

    -   [ ] Tooltips aparecem ao passar o mouse
    -   [ ] Tooltips mostram valor e status corretos
    -   [ ] Tooltips desaparecem ao remover o mouse
    -   [ ] Tooltips não causam lag

-   [ ] **Overlays de Cor em Tiles**

    -   [ ] Solo pobre (0-33%) tem cor marrom avermelhado
    -   [ ] Solo moderado (34-66%) tem cor marrom amarelado
    -   [ ] Solo saudável (67-100%) tem cor marrom esverdeado
    -   [ ] Transições de cor são claras
    -   [ ] Opacidade é apropriada

-   [ ] **Feedback Visual ao Usar Enxada**

    -   [ ] Flash visual ocorre ao usar enxada
    -   [ ] Texto flutuante aparece com novo valor
    -   [ ] Flash e texto funcionam bem juntos
    -   [ ] Feedback é consistente em diferentes condições

-   [ ] **Configuração de Visualização**

    -   [ ] Visualização pode ser habilitada/desabilitada
    -   [ ] Opacidade pode ser ajustada
    -   [ ] Tooltips podem ser habilitadas/desabilitadas
    -   [ ] Feedback da enxada pode ser habilitado/desabilitado

-   [ ] **Otimizações de Performance**
    -   [ ] Viewport culling funciona corretamente
    -   [ ] Overlays são cacheados
    -   [ ] Atualizações são throttled
    -   [ ] Performance é estável com muitos tiles

---

### 10.2. Checklist de Edge Cases

-   [ ] **Saves Corrompidos**

    -   [ ] Sistema lida com arquivo corrompido
    -   [ ] Sistema lida com arquivo ausente
    -   [ ] Sistema lida com dados inconsistentes
    -   [ ] Jogo não crasha com saves corrompidos

-   [ ] **Coordenadas Extremas**

    -   [ ] Sistema lida com coordenadas negativas extremas
    -   [ ] Sistema lida com coordenadas positivas extremas
    -   [ ] Sistema rejeita coordenadas fora do range
    -   [ ] Não há erros com coordenadas extremas

-   [ ] **Valores Inválidos**

    -   [ ] Sistema lida com valores negativos
    -   [ ] Sistema lida com valores acima de 100
    -   [ ] Sistema lida com valores não numéricos
    -   [ ] Valores inválidos são corrigidos ou ignorados

-   [ ] **Múltiplos Saves**

    -   [ ] Dados não são misturados entre saves
    -   [ ] Sistema lida com saves com mesmo nome
    -   [ ] Alternância entre saves funciona corretamente
    -   [ ] Cada save mantém seus próprios dados

-   [ ] **Múltiplas Localizações**

    -   [ ] Dados funcionam em múltiplas localizações
    -   [ ] Alternância rápida entre localizações funciona
    -   [ ] Não há mistura de dados entre localizações
    -   [ ] Visualizações funcionam em todas as localizações

-   [ ] **Condições de Jogo Especiais**
    -   [ ] Sistema funciona durante eventos
    -   [ ] Sistema funciona em diferentes horários
    -   [ ] Sistema funciona em diferentes estações
    -   [ ] Não há problemas específicos de condição

---

### 10.3. Checklist de Performance

-   [ ] **Performance com Muitos Tiles**

    -   [ ] 100 tiles não causam lag
    -   [ ] 500 tiles têm performance aceitável
    -   [ ] 1000 tiles são gerenciáveis
    -   [ ] Frame rate permanece estável

-   [ ] **Performance de Salvamento/Carregamento**

    -   [ ] Salvamento é rápido (< 1s)
    -   [ ] Carregamento é rápido (< 2s)
    -   [ ] Impacto no save/load do jogo é mínimo
    -   [ ] Tempos são consistentes

-   [ ] **Performance de Visualização**

    -   [ ] Overlays são renderizados suavemente
    -   [ ] Tooltips aparecem instantaneamente
    -   [ ] Feedbacks são exibidos sem atraso
    -   [ ] Não há drops de frame

-   [ ] **Uso de Memória**
    -   [ ] Uso de memória é baixo com poucos tiles
    -   [ ] Uso de memória é moderado com muitos tiles
    -   [ ] Não há vazamento de memória
    -   [ ] Uso de memória é estável ao longo do tempo

---

### 10.4. Checklist de Segurança

-   [ ] **Validação de Entrada**

    -   [ ] Valores de saúde são validados
    -   [ ] Coordenadas são validadas
    -   [ ] Nomes de localização são validados
    -   [ ] Validações funcionam corretamente

-   [ ] **Proteção contra DoS**

    -   [ ] Limite de tiles por localização é respeitado
    -   [ ] Limite de localizações por save é respeitado
    -   [ ] Limite total de tiles é respeitado
    -   [ ] Sistema não crasha ao atingir limites

-   [ ] **Sanitização de Arquivos**

    -   [ ] Nomes de arquivo são sanitizados
    -   [ ] Path traversal é prevenido
    -   [ ] Unicode é tratado corretamente
    -   [ ] Arquivos são criados em locais seguros

-   [ ] **Proteção de Dados**
    -   [ ] Dados de diferentes saves são isolados
    -   [ ] Modificações externas são detectadas
    -   [ ] Backups são criados automaticamente
    -   [ ] Dados são protegidos adequadamente

---

### 10.5. Checklist de Compatibilidade

-   [ ] **Compatibilidade com Stardew Valley**

    -   [ ] Mod funciona com a versão atual do jogo
    -   [ ] Mod não interfere com funcionalidades do jogo
    -   [ ] Mod funciona com saves existentes
    -   [ ] Não há conflitos com o jogo base

-   [ ] **Compatibilidade com SMAPI**

    -   [ ] Mod carrega corretamente com SMAPI
    -   [ ] Comandos do mod funcionam
    -   [ ] Console do SMAPI mostra mensagens apropriadas
    -   [ ] Não há erros de carregamento do mod

-   [ ] **Compatibilidade com Outros Mods**
    -   [ ] Mod funciona com mods populares
    -   [ ] Não há conflitos conhecidos
    -   [ ] Mod pode ser desabilitado sem problemas
    -   [ ] Ordem de carregamento não causa problemas

---

### 10.6. Checklist de Experiência do Usuário

-   [ ] **Facilidade de Uso**

    -   [ ] Visualizações são intuitivas
    -   [ ] Tooltips são informativos
    -   [ ] Cores são distinguíveis
    -   [ ] Feedback visual é claro

-   [ ] **Documentação**

    -   [ ] Guia do usuário está claro
    -   [ ] Configurações são explicadas
    -   [ ] Exemplos são fornecidos
    -   [ ] Perguntas frequentes são respondidas

-   [ ] **Acessibilidade**

    -   [ ] Cores têm contraste adequado
    -   [ ] Texto é legível
    -   [ ] Visualizações não obstruem a visão
    -   [ ] Opções de configuração são flexíveis

-   [ ] **Satisfação Geral**
    -   [ ] Mod melhora a experiência de jogo
    -   [ ] Funcionalidades são úteis
    -   [ ] Performance é aceitável
    -   [ ] Mod é estável e confiável

---

### 10.7. Checklist Final de Validação

Antes de considerar os testes completos, verifique:

-   [ ] Todos os testes principais foram executados
-   [ ] Todos os edge cases foram testados
-   [ ] Todos os testes de performance foram concluídos
-   [ ] Todos os testes de segurança foram realizados
-   [ ] Todos os bugs encontrados foram documentados
-   [ ] Todos os bugs críticos foram reportados
-   [ ] Documentação de testes está completa
-   [ ] Screenshots de bugs foram capturados
-   [ ] Logs do console foram salvos
-   [ ] Versão do mod testada foi documentada
-   [ ] Ambiente de teste foi documentado
-   [ ] Resultados foram compartilhados com desenvolvedores

---

## Conclusão

Este guia fornece uma abordagem completa e sistemática para testar as funcionalidades US01-01 (Persistência de Saúde do Solo) e US01-02 (Visualização de Saúde do Solo) do mod LivingRoots para Stardew Valley.

### Pontos Chave

1. **Teste de Forma Sistemática**: Siga a ordem dos testes e não pule etapas
2. **Documente Tudo**: Mantenha registros detalhados de todos os testes
3. **Teste Edge Cases**: Não tenha medo de testar situações extremas
4. **Relate Bugs de Forma Detalhada**: Forneça contexto suficiente para reprodução
5. **Colabore com a Comunidade**: Compartilhe resultados e melhore o mod

### Próximos Passos

Após completar os testes deste guia:

1. **Revise os Resultados**: Analise os resultados e identifique padrões
2. **Reporte Bugs**: Crie issues no GitHub para bugs encontrados
3. **Sugira Melhorias**: Contribua com ideias para melhorias
4. **Teste Novas Versões**: Continue testando versões futuras do mod
5. **Ajude Outros Testadores**: Compartilhe experiências e conhecimento

### Recursos Adicionais

-   **Repositório GitHub**: [LivingRoots Repository](https://github.com/seu-usuario/LivingRoots)
-   **Documentação do Desenvolvedor**: `LivingRoots/docs/`
-   **Guia do Usuário**: `LivingRoots/docs/SOIL_HEALTH_VISUALIZATION_USER_GUIDE.md`
-   **Issues e Discussões**: GitHub Issues e Discussions

### Agradecimentos

Obrigado por dedicar seu tempo para testar o LivingRoots! Seus testes ajudam a garantir que o mod seja estável, seguro e divertido para todos os jogadores.

---

**Versão do Guia**: 1.0.0
**Última Atualização**: 2026-01-06
**Versão do Mod Testada**: 1.0.0
**Autores**: Equipe de Desenvolvimento LivingRoots
