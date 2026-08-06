# ROADMAP — Corrida no Espaço

Acompanhamento da implementação completa e funcional do projeto.
Marque cada item conforme for concluído: `[ ]` → `[x]`.

**Legenda de status das fases:** ⬜ Não iniciada · 🟡 Em andamento · ✅ Concluída

> Complementa `docs/ESCOPO.html` (28/07/2026). O escopo foi escrito **antes** do commit
> `Rebuilding in unity 6.3` e descreve um estado que não existe mais — os scripts do
> template Space Shooter foram removidos e a `Game.unity` foi esvaziada. Onde os dois
> divergirem, **este arquivo é o atual**. A análise de publicação e política da Play no
> escopo continua válida.

---

## ⏸️ Retomar aqui — parado em 06/08/2026, à noite

**Onde paramos:** o Bloco A da Fase 6 Parte 4 está **escrito e não rodado**. O código compila
até onde dá para saber sem abrir a Unity, mas nenhuma ferramenta nova foi executada e nada foi
testado. O jogo no aparelho ainda é o de antes do Bloco A: uma fase, um tipo de obstáculo.

**Primeira coisa ao voltar, nesta ordem:**

1. Conferir o Console depois da recompilação.
2. `git add -A && git commit` — as ferramentas de montagem precisam estar commitadas **antes**
   de rodar, senão não voltam do git quando forem apagadas.
3. **Tools → Corrida no Espaço → Montagem → Criar fases e fichas de obstáculo**
4. **Tools → Corrida no Espaço → Montagem → Montar combate** (de novo, para o spawner na cena
   pegar a versão nova)
5. Testar: a Fase 1 tem de continuar igual ao que já estava aprovado — só Detrito, mesmo ritmo.

**Duas decisões esperando você:**

- **Ganho passivo de velocidade** (pedido do Raffael em 06/08, ver Bloco B) — falta decidir se
  ele sozinho pode chegar à dobra ou se para pouco antes. Muda se dá para vencer sem atirar.
- **Seletor provisório de fase** — hoje não há como testar as Fases 2 e 3, porque o menu de
  seleção só vem no Bloco B. Ofereci pôr um seletor no Inspector do objeto `Race` para destravar
  o teste; são cinco minutos e você não respondeu ainda.

**Depois disso:** Bloco B — menu de fases, progressão, fase sem fim e o leaderboard.

---

## Estado atual

*Revisto em 06/08/2026.*

**Funcional**
- Cena `Menu.unity` com fiação correta: `Menu.cs` no Canvas, BotaoJogar → `OnPlayButton`,
  BotaoSair → `OnQuitButton`, GraphicRaycaster e EventSystem presentes, câmera com tag `MainCamera`.
- **EventSystem migrado** para `InputSystemUIInputModule` (`Menu.unity:934`) — os botões do menu
  respondem ao toque no aparelho. Confirmado no commit `37df3cb`.
- **CanvasScaler corrigido** (`Menu.unity:686-691`): *Scale With Screen Size*, 1080x1920, match 0.5.
- **Retrato travado** e safe area reservada (`ProjectSettings.asset:11,73`).
- Ambas as cenas na lista de build (`Menu`, `Game`).
- Build Android instalando e abrindo no aparelho.
- Conta Unity nova, licença PE Personal ativa. Projeto **não** tem vínculo com organização
  Unity (`cloudProjectId` e `organizationId` vazios) — a conta antiga excluída não afeta nada.
- `applicationId` novo: **`br.com.raffael.corridanoespaco`** (decidido em 02/08/2026).
- Keystore desamarrado da máquina — `AndroidKeystoreName` e `AndroidKeyaliasName` vazios.
- Camada de toque — `Assets/Scripts/Input/TouchInput.cs`, único arquivo da pasta desde 05/08/2026.
  **Validada no aparelho** em 05/08/2026.
- **`Game.unity` montada** (02/08/2026), com `Ship`, `Track`, `LaneDividers` (Borda 0, Divisa 1,
  Divisa 2, Borda 3), `Input` e, desde 05/08, `UI` e `EventSystem`. Foi o `GameSceneSetup` que
  montou — script já apagado, ver Fase 6 — e a arte marcador de lugar dele está em
  `Assets/Art/Placeholder/`.
- **Play Games removido do projeto** (02/08/2026) — ver "Limpeza" abaixo. Sem plugin, sem
  erro de resolução de dependência a cada recompilação.
- **Painel de ferramentas** (05/08/2026) — **Tools → Corrida no Espaço → Painel de ferramentas**
  lista as ferramentas do projeto e, quando houver montagem pendente, separa "A fazer" de
  "Já rodadas" com a data (`ProjectTools.MarkRun`). O menu ficou só com rotina: *Conferir
  configuração* e *Build*. As ferramentas de uso único foram **apagadas depois de rodar**, a
  pedido do Raffael — o menu não acumula item morto, e o que elas produziram está salvo nas cenas.
- **Troca de faixa e menu de pausa aprovados no aparelho** (05/08/2026) — a nave anda **uma faixa
  por arraste**, o hambúrguer pausa o jogo e o "Sair" volta para a `Menu.unity`. É a primeira vez
  que o jogo faz algo de verdade num celular. A pausa é `Time.timeScale = 0`, que o
  `ShipLaneController` lê para ignorar arraste — nenhum script de jogabilidade conhece a UI.
- **A corrida anda** (06/08/2026) — `RaceSpeed` (velocidade do zero ao cruzeiro), campo de
  estrelas rolando na velocidade dela e HUD no rodapé. **O jogo virou uma corrida**: até ontem
  era um seletor de faixa parado no vazio.
- **O jogo está jogável de ponta a ponta** (06/08/2026) — ficha da nave, tiro automático,
  obstáculos com vida e dano, derrota por vida em zero, vitória por dobra, tempo como pontuação,
  tabela de recordes com nome, e o menu com Recordes e a transição do pódio. Aprovado no
  aparelho, com o equilíbrio acertando de primeira.
- **Unity Analytics desligado** — `UnityConnectSettings.asset` com tudo em `0`, e o módulo
  `com.unity.modules.unityanalytics` fora do `manifest.json`. A configuração agora concorda
  com o que a política de privacidade afirma.

**Faltando**
- **Bloco A da Parte 4 escrito, não rodado.** No aparelho, o jogo ainda é o de antes: uma fase,
  um tipo de obstáculo, ritmo constante. Ver "Retomar aqui", no topo.
- Menu de seleção de fase, progressão e fase sem fim (Bloco B).
- Áudio: nenhum `.wav`/`.mp3`/`.ogg` no projeto.
- Ícone e splash próprios.
- `AndroidTargetSdkVersion` ainda em `0` (Automatic) — **prazo: 31/08/2026**.
- Keystore de upload novo (o antigo está perdido).
- Play Games inteiro — plugin, login e conquistas voltam na Fase 7, do zero.

---

## Limpeza de 02/08/2026

O projeto arrastava o plugin do Play Games instalado no setup da conta antiga, que morreu.
O *External Dependency Manager* dele falhava a cada recompilação
(`Failed to fetch com.google.games:gpgs-plugin-support:0.11.01`): o artefato estava no disco,
mas as transitivas `play-services-games-v2` e `play-services-nearby` nunca foram baixadas —
`Assets/GeneratedLocalRepo/` não existia e não havia `.aar` nenhum em `Assets/Plugins/Android/`.
Como a Fase 7 vai instalar um plugin novo contra um projeto GPGS novo, manter o velho era
carregar configuração morta que gritava erro sem entregar nada.

**Removido, com a Unity fechada:**

| Item | Por quê |
|---|---|
| `Assets/GooglePlayGames/` | plugin 0.11.01, da conta excluída |
| `Assets/ExternalDependencyManager/` | vinha junto com o plugin; é quem dava o erro |
| `Assets/Plugins/` | só tinha o `GooglePlayGamesManifest.androidlib` |
| `Assets/GPGSIds.cs` | IDs de conquistas de um projeto que não existe mais |
| `Assets/Scripts/Services/PlayGamesAuth.cs` | não compila sem o plugin — **guardado em `docs/fase-7/`** |
| `ProjectSettings/GooglePlayGameSettings.txt` | App ID `1042052166444` e pacote antigo, ambos mortos |
| `Assets/Adaptive Performance/` | nunca configurado |
| `Assets/Resources/` | pasta vazia |
| `Assets/Editor/com.unity.mobile.notifications/` | o jogo não manda notificação |

**Pacotes tirados do `manifest.json`:** `adaptiveperformance.google.android`, `collab-proxy`
(usamos git), `multiplayer.center` (jogo de um jogador), `timeline`, `visualscripting`
(escrevemos C#), `feature.mobile`, `modules.adaptiveperformance`, `modules.unityanalytics`.

**Mantido de propósito:**
- `TextMesh Pro` — hoje a `Menu.unity` não usa, mas o HUD de velocidade e pontuação vai usar,
  e reimportar depois é mais trabalho do que deixar quieto.
- `Assets/Scripts/VFX/SelfDestruct.cs` — parece sobra do template, mas **não é**:
  `Assets/Art/VFX/Prefabs/Explosion.prefab:4970` aponta para o GUID dele
  (`7c1e4a90b3d54f2e8a6b0d3c92f1e5a4`). Apagar quebra o prefab de explosão, que a Fase 6 vai
  querer. Está documentado em `Assets/Art/CREDITS.md`.
  *(Chegou a ser apagado por engano nesta limpeza e foi restaurado do git com o `.meta` junto —
  GUID conferido contra o prefab, bate.)*
- ~~`TouchTester.cs` / `TouchTestSetup.cs`~~ — foram mantidos aqui por "não custar nada", e o
  julgamento estava errado: custaram o jogo parar de rodar. **Apagados em 05/08/2026**, ver Fase 2.

> Tudo isto está no git. Se algo fizer falta, `git checkout 37df3cb -- <caminho>` traz de volta.
>
> **Para a Fase 7, o que valia a pena guardar:** as duas conquistas antigas se chamavam
> **"Conquista?"** e **"Mais uma tentativa?"**. Os IDs velhos não servem — o projeto GPGS
> será outro —, mas os nomes registram o que você já tinha desenhado.

---

## Fase 0 — Diagnóstico ✅
Bloqueia confiar em qualquer teste no aparelho. Sem saber o erro, não dá para separar bug real de ruído.

> **Resolvido em 31/07/2026.** O erro que se repetia era
> `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class,
> but you have switched active Input handling to Input System package in Player Settings`.
> Origem: o `EventSystem` da `Menu.unity` usava `StandaloneInputModule`, o módulo de UI do Input
> Manager antigo, que lê `UnityEngine.Input` a cada frame. Nenhum script do projeto usa a API
> antiga. Não era ruído: com o módulo morrendo na exceção, a UI não processa toque — os botões
> do menu não respondem no aparelho.

- [x] Atalho para o log — `tools/logcat.ps1` acha o adb da Unity sozinho, confere se o aparelho
      está autorizado e limpa o buffer antes de acompanhar (`-All` mostra tudo, `-Save` grava em arquivo)
- [x] Capturar o erro que fica repetindo no aparelho
- [x] Identificar a origem — configuração de cena, não código: `StandaloneInputModule` no `EventSystem`
- [x] Escrever o conserto — era o `EventSystemUpgrade.cs`, **apagado em 05/08/2026**: a migração
      é de uma vez só e já está feita na cena. Se um dia voltar a fazer falta:
      `git checkout c9d8519 -- Assets/Editor/Tools/EventSystemUpgrade.cs*`
- [x] **Rodar o item de menu acima e rebuildar** — feito; `Menu.unity:934` já está em
      `InputSystemUIInputModule` e o commit `37df3cb` confirma os botões respondendo
- [x] Confirmar se o Console do Unity acusa erro de compilação nos scripts novos — compilou

## Fase 1 — Tornar o jogo testável no celular ✅
São três ajustes pequenos que faziam o app *parecer* quebrado sem estar.
*Aplicados em 31/07/2026 e confirmados no aparelho no commit `37df3cb`.*

- [x] `Menu.unity` — CanvasScaler: *UI Scale Mode* → **Scale With Screen Size**,
      Reference Resolution **1080x1920**, Match **0.5**
      *(estava em Constant Pixel Size: botões de 300x65 px físicos ficavam minúsculos num 1080x2400)*
- [x] Travar orientação em **Portrait** — `defaultScreenOrientation: 0`, e as três autorrotações
      não-retrato desligadas
- [x] `androidRenderOutsideSafeArea` → `0` — o jogo não tem código lendo `Screen.safeArea`,
      então é melhor o Android reservar a faixa do notch. Reverter para `1` no dia em que
      a UI tratar a safe area sozinha
- [x] Confirmar no aparelho: menu legível, sem rotação, botões respondendo

> `AndroidTargetSdkVersion` saiu desta fase: tentei **36** e o build quebrou porque a SDK
> Platform 36 não está instalada. Voltou para `0` (Automatic) para destravar o teste, e o item
> vive agora na **Fase 4**, junto com o resto do que trava o release.

## Fase 2 — Input de toque ✅
*Fechada em 05/08/2026, no aparelho.* A prova saiu junto com o teste da nave: ela trocou de
faixa, então o `TouchInput` está lendo o toque. Não existe mais validador separado — nem
precisou.

- [x] Escrever a camada de leitura de toque — `Assets/Scripts/Input/TouchInput.cs`
      *(Input System novo, com fallback de mouse no Editor; roda com o `activeInputHandler: 2`
      atual, sem precisar mexer em Player Settings)*
- [x] ~~Validador visual `TouchTester.cs` + `TouchTestSetup.cs`~~ — **apagados em 05/08/2026,
      porque quebravam o jogo.** O objeto `TouchTest` trazia um segundo `TouchInput`, e como a
      classe é singleton um dos dois se autodestruía; quando o perdedor era o do `TouchTest`, o
      `TouchTester` ficava com a referência morta e disparava `MissingReferenceException` a cada
      frame, derrubando o `Update` da cena. O overlay ainda por cima cobria os 40% de cima da
      tela, bem em cima do botão do menu de pausa.
      Voltam com `git checkout c9d8519 -- Assets/Scripts/Input/TouchTester.cs* Assets/Editor/Tools/TouchTestSetup.cs*`,
      mas só valem a pena depois de resolver a duplicação de `TouchInput`
- [x] **Validado no aparelho** (05/08/2026) — Build And Run, a nave respondeu ao arraste
- [x] Decidir o gesto de jogo — **arrastar para a esquerda/direita** troca de faixa (02/08/2026).
      Implementado em `ShipLaneController`: o arraste é medido em **polegadas**, não em pixels,
      para o gesto ter o mesmo tamanho em qualquer densidade de tela. **Uma faixa por arraste**
      (05/08/2026): pouco importa a distância percorrida, o gesto vale uma faixa e o próximo
      comando só sai depois de soltar o dedo
- [x] ~~Decidir se o EventSystem migra para `InputSystemUIInputModule`~~ — decidido pela
      realidade: **não era opcional**. A suposição de que "hoje o legado funciona" estava errada;
      ver Fase 0. Migração feita e confirmada no aparelho

## Fase 3 — Decisões que travam o resto ✅
Herdadas da Parte 4 do escopo. Decididas em 02/08/2026.

- [x] **Qual é "o jogo"?** → **Corrida espacial com tiro.** Não é o shoot-'em-up clássico de
      ficar parado atirando no que desce: é uma corrida de nave, em faixas, que *também* tem
      tiro. O conceito completo ainda está sendo pensado, então o jogo será construído **por
      partes**, e cada parte só começa quando a anterior estiver rodando no aparelho. A ordem
      está na Fase 6
- [x] **Novo `applicationId`** → **`br.com.raffael.corridanoespaco`**, já aplicado em
      `ProjectSettings.asset:172`. O `PreflightCheck` compara com o antigo, então ele para de
      acusar sozinho

> **Consequência para a ficha da loja:** `docs/play-console/ficha-da-loja.md` descreve só a
> corrida ("um toque muda de faixa", "sem tiro"). Quando a parte de tiro entrar, as descrições
> pt-BR e en-US precisam ser reescritas. Não é urgente — nada é publicado antes da Fase 4.

## Fase 4 — Destravar o build de release 🟡
Fase 3 decidida, então esta está liberada. Fecha o assunto "publicar" de uma vez.
**O item da SDK 36 tem prazo: 31/08/2026.**

- [ ] Instalar a **Android SDK Platform 36** e fixar o Target API Level em 36 — sem isso a Play
      não aceita upload a partir de 31/08/2026, e hoje o valor está em Automatic
- [ ] Gerar keystore novo, guardar **fora** do repositório, senha e alias num gerenciador
- [x] Tirar o caminho absoluto do keystore do `ProjectSettings.asset` — `AndroidKeystoreName` e
      `AndroidKeyaliasName` agora vazios; o projeto não está mais amarrado a uma máquina
- [x] Script de build — `Assets/Editor/Tools/BuildAndroid.cs`. Menu **Tools → Corrida no Espaço →
      Build** (APK de teste / AAB de release) e alvos `-executeMethod BuildAndroid.Release` e
      `.Development` para linha de comando. As credenciais vêm das variáveis `CNE_KEYSTORE_PATH`,
      `CNE_KEYSTORE_PASS`, `CNE_KEY_ALIAS`, `CNE_KEY_ALIAS_PASS` e são limpas do Editor ao fim do
      build, para o caminho da chave nunca voltar a vazar para o `ProjectSettings.asset`
- [x] Conferência automática antes do build — `Assets/Editor/Tools/PreflightCheck.cs`, menu
      **Tools → Corrida no Espaço → Conferir configuração**: cenas e ordem da build list,
      orientação, target SDK, keystore vazado no projeto e `applicationId` antigo
- [x] Aplicar o novo `applicationId` — `br.com.raffael.corridanoespaco` em
      `ProjectSettings.asset:172` (02/08/2026, com a Unity fechada)
- [ ] Ativar Play App Signing (padrão) — permite reset se a chave de upload sumir de novo
- [ ] **Marco:** gerar um `.aab` de release assinado, mesmo com o jogo incompleto
- [x] ~~Adicionar `/.utmp/` ao `.gitignore`~~ — já estava lá (`.gitignore:14`); item era engano meu

## Fase 5 — Play Console, em paralelo 🟡
Não depende de código. **O relógio mais lento do projeto** — começar cedo.

- [x] Preparar o teste fechado no papel — `docs/play-console/teste-fechado.md`: regra, planilha
      para 16 e-mails (convidar mais que 12, porque gente some), texto de convite e checklist
- [ ] Montar a lista de **12 testadores** para o teste fechado (opt-in contínuo por 14 dias)
      — **é você quem junta os e-mails; nada mais no projeto trava isto**
- [x] Escrever a política de privacidade — `docs/politica-de-privacidade.md` e `docs/privacy-policy.md`
- [ ] Publicar a política numa URL pública (GitHub Pages do portfólio) e guardar o link
- [x] Desligar o Unity Analytics legado antes de publicar — feito em 02/08/2026:
      `UnityConnectSettings.asset` com `m_Enabled: 0` no topo e no bloco `UnityAnalyticsSettings`
      (`m_InitializeOnStartup: 0` junto), e o módulo `com.unity.modules.unityanalytics` fora do
      `manifest.json`. A configuração agora concorda com o texto da política
- [x] Escrever a ficha da loja PT e EN — `docs/play-console/ficha-da-loja.md`
      *(textos escritos para o conceito das 3 faixas; se a Fase 3 decidir outra coisa, reescrever)*
- [ ] Produzir os gráficos da ficha: ícone 512×512, feature graphic 1024×500, screenshots — **arte, é sua**
- [ ] Quando a conta sair da verificação: criar o app e testar se o pacote antigo é aceito
- [ ] Criar o projeto novo no Play Games Services
- [x] Rascunhar o Data Safety coerente com a política — `docs/play-console/data-safety.md`
- [ ] Preencher o Data Safety no Console
- [x] Rascunhar IARC e público-alvo — `docs/play-console/iarc-e-publico-alvo.md`
      *(recomendação: declarar 13+ e ficar fora da Política para Famílias)*
- [ ] Responder o questionário IARC no Console

> Regras da Play mudam com frequência — reconfirmar cada item no Console, não tratar como fato.

## Fase 6 — O jogo em si 🟡
O grosso do trabalho. Construído **por partes**: cada parte tem de estar rodando no aparelho
antes de a próxima começar. O conceito das partes 3 em diante ainda está aberto — o Raffael
decide cada uma na hora, vendo a anterior funcionar.

### Parte 1 — Faixas e troca de faixa ✅
A base de tudo: se a nave não anda direito entre as faixas, nada em cima disso presta.
*Aprovada no aparelho em 05/08/2026 — troca de faixa, pausa e saída para o menu, os três.*

- [x] `LaneTrack.cs` — geometria das 3 faixas. Fonte única de "onde fica a faixa N", para nave,
      obstáculos e cenário não carregarem cada um a sua própria constante. Desenha gizmo na
      Scene view, então dá para ver o enquadramento sem entrar em Play
- [x] `ShipLaneController.cs` — arrasta para a esquerda/direita e a nave migra de faixa.
      Deslocamento suave com inclinação proporcional ao que falta andar (a nave endireita
      sozinha ao chegar), e as faixas das pontas travam sem acumular arraste "contra a parede"
- [x] **Limite de uma faixa por arraste** (05/08/2026) — o gesto se esgota na primeira troca e
      só volta a valer no toque seguinte. Ignora também o arraste que começa em cima da UI e o
      que acontece com o jogo pausado (`Time.timeScale` em zero)
- [x] `Assets/Scripts/UI/PauseMenu.cs` — botão de hambúrguer meio transparente no canto superior
      direito, com "Continuar" e "Sair para o menu". A pausa é `Time.timeScale = 0`, que é o que
      o `ShipLaneController` lê para ignorar arraste: nenhum script de jogabilidade conhece a UI
- [x] **UI montada na `Game.unity` em 05/08/2026** pelo `PauseMenuSetup.cs`, que criou junto o
      `EventSystem` que faltava na cena. O script foi apagado depois de rodar — **e só volta do
      git se tiver sido commitado antes de 05/08/2026**; se não tiver, mudar cor, tamanho ou
      posição do menu passa a ser na mão pelo Inspector, sobre a UI que já está na cena
- [x] **Menu de pausa conferido no aparelho** (05/08/2026) — o hambúrguer pausa e o "Sair"
      volta para a `Menu.unity`
- [x] `GameSceneSetup.cs` montou a `Game.unity` inteira (câmera, corredor, divisas, nave, camada
      de toque) e gerou a arte marcador de lugar em `Assets/Art/Placeholder/`. Rodou em
      02/08/2026 sem erro — o único erro no Console era o do Play Games, sem relação, resolvido
      removendo o plugin (ver "Limpeza")
- [x] **`GameSceneSetup.cs` apagado em 05/08/2026** — a cena está montada e commitada, e a arte
      marcador de lugar está no `Assets/Art/Placeholder/`, então o gerador já entregou o que
      tinha para entregar. Para remontar do zero ou reposicionar as divisas depois de mudar a
      largura da faixa: `git checkout c9d8519 -- Assets/Editor/Tools/GameSceneSetup.cs*`.
      A refatoração do modelo híbrido (abaixo) passa essa responsabilidade para o `LaneTrack`
      em runtime, que é o motivo de ele não fazer mais falta
- [x] **Testado no aparelho** (05/08/2026) — uma faixa por gesto, sem faixa pulada
- [x] Ajustar o tato — **não precisou**. `swipeInchesPerLane` (0.18") e `laneChangeSpeed`
      (12 u/s) passaram no primeiro teste; seguem no Inspector se o tato mudar quando o fundo
      estiver em movimento
- [ ] **Refatorar para o modelo híbrido** — *adiado de propósito em 05/08/2026.* Hoje as faixas
      são só matemática no `LaneTrack` e as divisas foram desenhadas uma vez. Mudar `laneCount`
      no meio da fase moveria a nave sem redesenhar nada. O combinado é o `LaneTrack` passar a
      **gerar as faixas como objetos em runtime** e avisar quem depende quando o número muda —
      o que também dá onde pendurar coisa por faixa (spawn, faixa que fecha, faixa que acelera).
      **Só vale a pena quando existir um mecanismo que precise disso**; enquanto o corredor for
      fixo em 3 faixas, é arquitetura para problema que ninguém tem
      - [ ] **Decisão pendente do Raffael, e só quando a hora chegar:** ao ganhar uma faixa no
            meio da fase, o corredor cresce **para os dois lados mantendo o centro** (todas as
            faixas se deslocam, a nave junto) ou **só para um lado** (as faixas existentes ficam
            paradas)?

### Parte 2 — A nave se move para frente ✅
Sem cenário passando, não existe sensação de corrida. Liberada em 05/08/2026 com a Parte 1
aprovada no aparelho; **código escrito no mesmo dia, falta montar e testar**.

- [x] **Velocidade como número de verdade** — `Assets/Scripts/Gameplay/RaceSpeed.cs`: velocidade
      atual em unidades de mundo por segundo, com teto e evento `Changed`. Fonte única, do mesmo
      jeito que o `LaneTrack` é para as faixas. **A fase começa com a nave parada (0) e acelerando
      até a velocidade de cruzeiro**, decidido pelo Raffael em 05/08/2026
- [x] **Números da nave provisórios, com troca preparada** — cruzeiro 8 u/s, aceleração 4 u/s²
      (2 s do zero ao cruzeiro), teto 16 u/s. Estão num bloco marcado `PROVISÓRIO` no topo do
      `RaceSpeed`, e o dia em que a **ficha da nave** existir (velocidade, vida, o que mais vier)
      ela chama `ApplyShipStats(cruzeiro, aceleração, teto)` e pronto — **é o único ponto de
      troca**, nada mais no jogo lê estes números direto
- [x] **Cenário passando** — `Assets/Scripts/Gameplay/ScrollingBackground.cs`, rolando na
      velocidade lida do `RaceSpeed`. Dois ladrilhos que se revezam: quando um sai por baixo,
      volta para cima do outro. Nada é instanciado durante a corrida. O campo `speedFactor` já
      existe para o dia da paralaxe — a camada de trás anda mais devagar
- [x] Arte marcador de lugar do fundo — `Assets/Art/Placeholder/starfield.png`, campo de estrelas
      gerado com semente fixa (mesma imagem toda vez). Trocar o PNG por arte de verdade não
      encosta em código
- [x] **HUD de velocidade** — `Assets/Scripts/UI/SpeedHud.cs`, TextMesh Pro **no rodapé,
      centralizado** (pedido do Raffael, 05/08/2026). Só remonta a string quando o número muda,
      então em cruzeiro não gera lixo por frame
- [x] Editor script de montagem — `Assets/Editor/Tools/RaceSetup.cs`, menu **Tools → Corrida no
      Espaço → Montagem → Montar corrida (fundo + HUD)**, com "Desmontar corrida" para desfazer
- [x] **Rodado em 06/08/2026** — fundo rolando, velocidade subindo do zero ao cruzeiro e HUD
      no rodapé. Aprovado pelo Raffael de primeira, sem ajuste de número
- [x] **Confirmado no aparelho** (06/08/2026) — Editor e celular, sem ajuste de número
- [ ] Apagar o `RaceSetup.cs` depois de commitado, conforme o combinado do `CLAUDE.md`

> **Sem paralaxe de propósito** (decidido em 05/08/2026): primeiro tudo funcionando, o enfeite
> depois. O `speedFactor` do `ScrollingBackground` é o gancho para quando essa hora chegar.

### Parte 3 — O jogo inteiro ✅
Decidida pelo Raffael em 06/08/2026: **tudo de uma vez**, em vez de uma mecânica por parte.
Escrita, montada e **aprovada no mesmo dia, no Editor e no aparelho**. Com ela o projeto deixou
de ser uma demonstração e virou um jogo: tem começo, meio, derrota, vitória e recorde.

**A ficha da nave**
- [x] `Assets/Scripts/Gameplay/ShipStats.cs` — velocidade de cruzeiro, dano, velocidade de ataque
      e vida. É a ficha que o Raffael pediu antes de tudo, e **é dela que o resto lê**: a arma
      pega dano e cadência, a corrida pega a velocidade de cruzeiro (`RaceSpeed.ApplyShipStats`),
      o HUD pega a vida. Atributo novo entra aqui e mais nada muda
- [x] `Health.cs` — vida genérica, usada **pela nave e pelos obstáculos**. Quem causa dano não
      precisa saber no que está batendo

**Combate**
- [x] `Obstacle.cs` — desce na velocidade da corrida, tem vida (dá para destruir a tiro) e dá
      dano na nave se bater. Destruir **acelera** a corrida; bater **freia** e machuca
- [x] `ObstacleSpawner.cs` — solta um obstáculo por vez em faixa sorteada, com intervalo irregular
- [x] `ShipWeapon.cs` + `Projectile.cs` — **tiro automático**, na cadência da ficha, munição
      infinita. O dia em que virar comando do jogador, a condição entra no `Update` da arma e
      nada mais muda
- [x] **Sem Physics2D, de propósito** — o acerto é distância entre dois pontos, como o resto do
      jogo. Colisor mal configurado falha calado; distância, não

**Fim de corrida**
- [x] `RaceDirector.cs` — cronômetro, vitória e derrota. **Derrota:** vida da nave em 0.
      **Vitória:** velocidade alcança a de dobra (15 u/s)
- [x] Painéis de vitória (tempo, campo de nome, salvar, tabela) e de derrota, montados na cena
- [x] `HealthHud.cs` — vida no canto inferior esquerdo; o `SpeedHud` passou a mostrar a meta de
      dobra ao lado da velocidade atual

**Pontuação e recordes**
- [x] `Services/ScoreBoard.cs` — a pontuação é **o tempo até entrar em dobra**, então menor é
      melhor. Top 10 em PlayerPrefs, com nome. É por aqui que um leaderboard online passaria
      na Fase 7
- [x] `UI/RecordsBoard.cs` — escreve a tabela num TextMesh Pro só; o mesmo componente serve ao
      painel do menu (10 linhas), ao pódio da transição (3) e ao fim de corrida (5)

**Menu**
- [x] **Terceiro botão, "Recordes"**, entre Jogar e Sair, com o painel da tabela
- [x] **Transição antes da partida** — "Jogar" mostra o pódio (3 primeiros, nome e tempo) por
      3 segundos, ou até tocar na tela, e só então carrega a fase
- [x] **Botões maiores** — de 300x65 para 440x110, com o texto crescendo junto

**Montagem e teste**
- [x] Rodadas as montagens de combate e de menu, nesta ordem
- [x] **Testado no Editor e no aparelho** (06/08/2026)
- [x] **Equilíbrio aprovado de primeira** — "rápido o suficiente para ser difícil, mas não
      impossível". Vida 100, dano 25, 3 tiros/s, obstáculo com 50 de vida e 20 de dano,
      +1,5 de velocidade por obstáculo destruído, -2 por batida, dobra em 15.
      **Mexer nestes números é mexer na dificuldade do jogo inteiro** — anotados aqui porque a
      partir de agora qualquer mecânica nova é comparada com este ponto de partida que funciona
- [ ] Apagar as ferramentas de montagem já rodadas (`RaceSetup`, `BattleSetup`, `MenuSetup`),
      depois de commitadas

> A ficha da loja (`docs/play-console/ficha-da-loja.md`) descreve só a corrida, "sem tiro".
> **Agora tem tiro** — as descrições pt-BR e en-US precisam ser reescritas antes de publicar.

### Parte 4 — Variedade e desafio 🟡
O jogo funciona, mas toda partida é igual à anterior: um tipo de obstáculo, um ritmo, uma nave.
O objetivo desta parte é dar ao jogador motivo para jogar de novo.

**A ficha completa** *(proposta do Raffael em 06/08/2026, ajustada por ele no mesmo dia)*
- [x] **Aceleração** saiu do `RaceSpeed` e entrou no `ShipStats`. Continua no `RaceSpeed` como
      valor de partida, para a cena funcionar sem nave; a ficha impõe o dela em
      `ApplyShipStats(cruzeiro, aceleração)`. Manda em quão rápido a nave se recupera de uma freada
- [x] **Defesa em %** — `ShipStats.DamageAfterDefense()` reduz e **arredonda para cima**, com
      mínimo de 1. Decisão do Raffael: *"se fosse 1 de dano e a redução fosse 99%, ainda toma 1"*.
      Um golpe que acerta nunca é de graça, então defesa alta não vira imunidade e o número não
      precisa de teto artificial
- [x] Bater na nave passou a ser `ShipStats.TakeHit()`, e não mais direto no `Health`: é o único
      caminho por onde a defesa desconta
- [x] ~~**Velocidade de dobra** como atributo da nave~~ — **descartada pelo Raffael**: ela fica
      sendo **regra da fase**, que é quem decide o quanto se exige para completar a dobra. Some
      junto a inversão esquisita de "menor é melhor" num atributo de nave
- [x] **Validação da fase invencível** — `RaceDirector` acusa no Console se a dobra exigir mais
      que o teto de velocidade do `RaceSpeed`. Sem ela, o jogador corre atrás de uma meta que a
      física do jogo não alcança e nada avisa
- [x] **Dobra com tempo de carga** (decidido pelo Raffael em 06/08/2026) — chegar à velocidade
      não vence mais na hora: a nave precisa **segurar** a velocidade pelo tempo que a fase pedir
      (5 s hoje). Abaixo da velocidade a carga **escoa** pela metade do ritmo, em vez de zerar:
      uma batida no fim custa caro sem apagar a corrida inteira.
      `warpChargeSeconds` e `warpDecayRate` ficam no `RaceDirector`
      - Serve de **ajuste de dificuldade principal da fase**: mexer no tempo de dobra é mexer em
        quanto o jogador tem de aguentar já correndo depressa demais para desviar com folga
- [x] `UI/WarpChargeHud.cs` — contagem regressiva acima do velocímetro, aparecendo só quando há
      carga. Muda de cor quando a carga está escoando

**Bloco A — obstáculos e fichas de fase** *(escrito em 06/08/2026, falta rodar e testar)*
- [x] `Levels/ObstacleStats.cs` — **ficha do obstáculo**, no mesmo espírito da ficha da nave, mas
      como ScriptableObject: o dado é igual para todos os obstáculos daquele tipo. Vida, dano,
      quantas faixas ocupa, bônus de velocidade ao morrer, estilhaços. Pedido do Raffael para
      obstáculo novo não passar por código
- [x] `Levels/LevelDefinition.cs` — **ficha da fase**: velocidade e tempo de dobra, ritmo de
      spawn (com rampa do início ao fim da corrida) e a **agenda de obstáculos** — que tipo entra
      e a partir de que segundo, com a estreia **sorteada dentro de uma janela** para duas
      partidas da mesma fase não ficarem idênticas
- [x] `Levels/LevelCatalog.cs` — as fases, a fase sem fim e as três dificuldades num arquivo só,
      em `Resources/`. A dificuldade **não troca obstáculo** (isso é papel da fase): ela soma na
      velocidade de dobra e multiplica ritmo, vida e dano
- [x] `Levels/LevelSelection.cs` — a escolha atravessa a troca de cena. Abrir a `Game.unity`
      direto no Editor cai na Fase 1, então dá para testar sem navegar pelo menu
- [x] **Três tipos de obstáculo**, como o Raffael desenhou:
      **Detrito** (o atual, desde o segundo 0 em toda fase), **Barcaça** (mais vida, **ocupa 2
      faixas**, entra entre o segundo 5 e 10 a partir da fase 2) e **Casulo** (ao ser destruído
      solta **estilhaços que descem pela própria faixa**, a partir da fase 3)
- [x] `Shrapnel.cs` — os estilhaços descem mais rápido que a corrida. **É o primeiro elemento que
      obriga a trocar de faixa**: até aqui dava para vencer parado no meio, segurando o gatilho
- [x] **Regra da fuga garantida** — o spawner olha a **leva inteira** que está descendo e nunca
      fecha todas as faixas; se não houver posição que deixe saída, ele pula a batida. Sem isso,
      a Barcaça de 2 faixas mais um Detrito na faixa restante matariam por sorteio, e não por erro
- [ ] Rodar **Criar fases e fichas de obstáculo** e depois **Montar combate**
- [ ] Testar por etapas no Editor e no aparelho
- [ ] Equilibrar: vida e dano dos três tipos, janelas de estreia e os fatores de dificuldade

**Bloco B — fases, progressão e leaderboard** *(a fazer)*
- [ ] **Ganho passivo de velocidade** *(pedido do Raffael em 06/08/2026)* — hoje só destruir
      obstáculo acelera a corrida, o que obriga a atirar. A nave passa a ganhar velocidade
      **sozinha, com o tempo, proporcional ao atributo de aceleração da ficha** — de leve, para
      quem preferir desviar a destruir também chegar lá, só que devagar.
      Onde entra: `RaceSpeed` empurrando o `Target` um pouquinho por segundo
      - [ ] **Decisão pendente:** esse ganho sozinho chega até a velocidade de dobra, ou para
            um pouco antes (digamos, 90% dela)?
            **Chegando:** desviar vira estratégia completa, e uma partida paciente vence sem um
            tiro — mais liberdade, e o tiro vira atalho em vez de obrigação.
            **Parando antes:** desviar leva você quase lá, mas fechar a corrida exige alguns
            abates — o tiro continua tendo papel, e a dobra continua sendo conquistada.
            *Recomendação: parar antes.* O jogo se chama corrida **com tiro**, e um teto que
            some perto do fim mantém as duas coisas valendo sem tirar a alternativa de quem
            joga desviando
      - [ ] Cuidado ao equilibrar: o ganho passivo também **desfaz sozinho a punição da batida**.
            Se ficar generoso demais, bater deixa de doer
      - [ ] Na fase sem fim não há dobra, então lá o ganho passivo vira só rampa: a corrida fica
            perigosa com o tempo mesmo para quem não atira em nada
- [ ] Seleção de fase e dificuldade no menu — 3 fases × 3 dificuldades, mais a sem fim
- [ ] **Progressão**: vencer a fase N destrava a N+1; fechar as três destrava a dificuldade
      seguinte; fechar o Difícil destrava a fase sem fim
- [ ] **Fase sem fim** — sem dobra, endurece sozinha, acaba quando a nave cai
- [ ] **Leaderboard só da fase sem fim.** Cuidado registrado: a pontuação **inverte de sentido**.
      Hoje é *tempo até a dobra* e menor é melhor; lá é *tempo sobrevivido* e **maior é melhor**.
      O `ScoreBoard` ordena crescente e precisa mudar junto
- [ ] As fases normais deixam de ter placar: o painel de vitória delas troca o campo de nome por
      "fase concluída" e o que foi destravado

**Depois**
- [ ] Barreira que impede naves fracas de avançar *(ideia do Raffael, adiada por ele)*
- [ ] **Itens** — reparo, escudo temporário, tiro rápido
- [ ] **Escolha de nave** — duas ou três fichas diferentes. É onde aceleração e defesa passam a
      valer de verdade: hoje afinam uma nave só

**Depois disso**
- [ ] Decidir se o tiro vira comando do jogador, com munição, ou continua automático
- [ ] Áudio: motor, tiro, impacto, vitória, derrota
- [ ] Arte de verdade no lugar dos marcadores de `Assets/Art/Placeholder/`
- [ ] Pool de objetos para tiro e obstáculo, se o `Instantiate`/`Destroy` pesar no aparelho

## Fase 7 — Fechar login e conquistas ⬜
Depende do projeto GPGS novo (Fase 5). **O plugin não está mais no projeto** — foi removido na
limpeza de 02/08/2026 junto com a configuração da conta antiga. Esta fase começa por instalar
o plugin atual, e não por consertar o velho.

- [ ] **Instalar o plugin do Play Games atual** (o removido era 0.11.01, anterior à Unity 6.3 —
      pegar a versão que declare suporte a Unity 6). Conferir logo depois se o External
      Dependency Manager resolve as dependências Android sem erro; se falhar de novo, o log
      verboso do Console diz se é rede, JDK/Gradle ou incompatibilidade de versão
- [ ] Recriar as conquistas no novo projeto GPGS e regerar `GPGSIds.cs`
      *(as antigas se chamavam "Conquista?" e "Mais uma tentativa?")*
- [ ] Devolver o `PlayGamesAuth.cs` de `docs/fase-7/` para `Assets/Scripts/Services/`
- [x] Reescrever o login — `PlayGamesAuth.cs` (hoje guardado em `docs/fase-7/`) substitui o antigo
      `Google-Login.cs`, que foi apagado. Login silencioso opcional, login manual em botão,
      falha vira warning e não quebra o jogo, token de servidor vai por callback e não fica
      guardado. Traz também `UnlockAchievement`/`ReportAchievement`
      *(o arquivo velho se chamava `Google-Login.cs` com a classe `GooglePlayGamesExampleScript`:
      com hífen no nome e classe diferente do arquivo, a Unity nunca deixaria anexar num
      GameObject — era código morto por construção)*
- [ ] Colocar o `PlayGamesAuth` numa cena (raiz da `Menu.unity`) — só depois do projeto GPGS novo
      e por editor script, nunca editando a cena na mão
- [ ] Ligar um botão de login manual na UI, para quando o silencioso falhar
- [ ] Chamar `UnlockAchievement` de fato no jogo (passo 4.4 do escopo, nunca fechado)
- [ ] Cadastrar a SHA-1 de debug no Console para conseguir testar login sem build de release

## Fase 8 — Acabamento ⬜

- [ ] Ícone próprio (hoje usa o padrão do Unity)
- [ ] Splash própria (hoje "Made with Unity")
- [ ] Leaderboard do Play Games (opcional)

## Fase 9 — Monetização ⬜
Depois do lançamento. Anúncios antes de compras.

- [ ] Anúncios (integração)
- [ ] Compras no app (design de economia — projeto próprio, do tamanho do jogo base)

---

## Ordem recomendada

*Reordenado em 05/08/2026, com as Fases 0, 1 e 3 fechadas.*

*Build And Run de 05/08/2026 aprovado: troca de faixa, pausa e saída para o menu. Fases 0, 1, 2 e
3 fechadas, e a Parte 1 da Fase 6 junto.*

**Próximo passo, nesta ordem:**

1. **Rodar e testar o Bloco A da Parte 4** — ver "Retomar aqui", no topo do arquivo.
2. **Bloco B** — ganho passivo de velocidade, menu de fases, progressão, fase sem fim e o
   leaderboard invertido.
3. **Fase 4, a SDK Platform 36** — **prazo 31/08/2026**, e é instalação pelo Android SDK Manager,
   não código. Quanto mais perto do fim do mês, mais chance de dar errado na pressa.
4. **Fase 5, os 12 testadores** — o relógio mais lento do projeto e ainda não começou a correr.
5. Reescrever a ficha da loja: agora o jogo tem tiro, e o texto atual diz que não tem.

> O modelo híbrido de faixas segue adiado de propósito (ver Parte 1) — é arquitetura para um
> problema que o jogo ainda não tem.

**Em paralelo, sem depender de código:**
- Fase 5, os 12 testadores — o relógio mais lento do projeto, e ainda não começou a correr
- Fase 4, a SDK Platform 36 — **prazo 31/08/2026**, e é instalação pelo Android SDK Manager

**Na sequência:** Fase 6 parte por parte, cada uma testada no aparelho antes da próxima.

**Quando o jogo tiver forma:** Fase 4 até o marco do `.aab` assinado, e reescrever a ficha da
loja com o tiro incluído.

**Por último:** Fases 7, 8 e 9.

**Caminho crítico real:** Fase 6, parte por parte. Todo o resto ou é barato, ou roda em
paralelo, ou não depende de você.
