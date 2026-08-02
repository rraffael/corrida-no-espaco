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

## Estado atual

*Revisto em 02/08/2026, conferindo arquivo por arquivo.*

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
- Camada de toque em `Assets/Scripts/Input/` — escrita, ainda **não validada no aparelho**.
- **`Game.unity` montada** (02/08/2026): 822 linhas, com `Ship`, `Track`, `LaneDividers`
  (Borda 0, Divisa 1, Divisa 2, Borda 3) e `Input`. O `GameSceneSetup` rodou sem erro e gerou
  a arte marcador de lugar em `Assets/Art/Placeholder/`. **Falta testar no aparelho.**
- **Play Games removido do projeto** (02/08/2026) — ver "Limpeza" abaixo. Sem plugin, sem
  erro de resolução de dependência a cada recompilação.
- **Unity Analytics desligado** — `UnityConnectSettings.asset` com tudo em `0`, e o módulo
  `com.unity.modules.unityanalytics` fora do `manifest.json`. A configuração agora concorda
  com o que a política de privacidade afirma.

**Faltando**
- **Teste no aparelho da troca de faixa** — a cena está montada, mas ninguém rodou ainda.
- Gameplay: só a primeira etapa (troca de faixa) está escrita.
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
- `TouchTester.cs` / `TouchTestSetup.cs` — ferramenta de diagnóstico de toque. Não custa nada e
  serve se a nave se comportar mal no aparelho.

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
- [x] Escrever o conserto — `Assets/Editor/Tools/EventSystemUpgrade.cs`, menu
      **Tools → Corrida no Espaço → Migrar EventSystem para o Input System**
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

## Fase 2 — Input de toque 🟡
Camada escrita e já ligada na jogabilidade (Fase 6, Parte 1). Falta a prova no aparelho —
que agora sai junto com o teste da nave, sem precisar do `TouchTest`.

- [x] Escrever a camada de leitura de toque — `Assets/Scripts/Input/TouchInput.cs`
      *(Input System novo, com fallback de mouse no Editor; roda com o `activeInputHandler: 2`
      atual, sem precisar mexer em Player Settings)*
- [x] Escrever o validador visual — `Assets/Scripts/Input/TouchTester.cs`
- [x] Automatizar a montagem da cena de teste — `Assets/Editor/Tools/TouchTestSetup.cs`
      *(menu **Tools → Corrida no Espaço → Montar teste de toque**: abre a `Game.unity`, cria o
      objeto `TouchTest` com `TouchInput` + `TouchTester` e salva. O item "Desmontar" desfaz)*
- [ ] **Validar no aparelho:** rodar o item de menu acima → Build And Run → conferir dedos
      ativos, posição, delta e taps
- [ ] Desmontar o `TouchTest` antes de qualquer build de release
- [x] Decidir o gesto de jogo — **arrastar para a esquerda/direita** troca de faixa (02/08/2026).
      Implementado em `ShipLaneController`: o arraste é medido em **polegadas**, não em pixels,
      para o gesto ter o mesmo tamanho em qualquer densidade de tela; arraste longo e contínuo
      atravessa mais de uma faixa sem soltar o dedo
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

## Fase 5 — Play Console, em paralelo ⬜
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

### Parte 1 — Faixas e troca de faixa 🟡
A base de tudo: se a nave não anda direito entre as faixas, nada em cima disso presta.

- [x] `LaneTrack.cs` — geometria das 3 faixas. Fonte única de "onde fica a faixa N", para nave,
      obstáculos e cenário não carregarem cada um a sua própria constante. Desenha gizmo na
      Scene view, então dá para ver o enquadramento sem entrar em Play
- [x] `ShipLaneController.cs` — arrasta para a esquerda/direita e a nave migra de faixa.
      Deslocamento suave com inclinação proporcional ao que falta andar (a nave endireita
      sozinha ao chegar), e as faixas das pontas travam sem acumular arraste "contra a parede"
- [x] `GameSceneSetup.cs` — menu **Tools → Corrida no Espaço → Montar cena do jogo (3 faixas)**.
      Monta a `Game.unity` inteira: câmera, corredor, divisas, nave e a camada de toque.
      Gera a arte marcador de lugar em `Assets/Art/Placeholder/` — trocar por arte de verdade
      depois não encosta em código. O item "Desmontar cena do jogo" desfaz
- [x] **Rodar o item de menu** — feito em 02/08/2026. A cena montou sem erro; o único erro no
      Console era o do Play Games, sem relação, e foi resolvido removendo o plugin (ver "Limpeza")
- [ ] **Testar no aparelho** — no Editor já dá (o `TouchInput` cai para o mouse), mas o que vale
      é o aparelho: o arraste tem de ter o peso certo, sem faixa pulada
- [ ] Ajustar o tato se precisar: `swipeInchesPerLane` (0.18" hoje) e `laneChangeSpeed` (12 u/s)
      estão expostos no Inspector
- [ ] **Refatorar para o modelo híbrido** — hoje as faixas são só matemática no `LaneTrack`, e
      as divisas são desenhadas uma vez pelo editor script. Mudar `laneCount` no meio da fase
      move a nave mas não redesenha nada, e a nave pode ficar com índice inválido. O combinado
      é: o `LaneTrack` continua dono da geometria, mas passa a **gerar as faixas como objetos em
      runtime** e a avisar quem depende quando o número muda — o que também dá onde pendurar
      coisa por faixa (spawn, faixa que fecha, faixa que acelera)
      - [ ] **Decisão pendente do Raffael:** ao ganhar uma faixa no meio da fase, o corredor
            cresce **para os dois lados mantendo o centro** (todas as faixas se deslocam, a nave
            junto) ou **só para um lado** (as faixas existentes ficam paradas)?

### Parte 2 — A nave se move para frente ⬜
Sem cenário passando, não existe sensação de corrida.

- [ ] Rolagem do fundo / cenário passando, dando a impressão de avanço
- [ ] Velocidade como número de verdade, que outras coisas possam ler e alterar

### Parte 3 em diante — a definir ⬜
Candidatos herdados do escopo, **nenhum decidido**:

- [ ] Obstáculos que aceleram ou freiam (velocidade = única barra de vida)
- [ ] Tiro — o que dá para acertar, e o que acertar muda na corrida
- [ ] Derrota com velocidade ≤ 0; vitória por dobra espacial com velocidade ≥ X
- [ ] Voltar ao menu ao morrer (`SceneManager.LoadScene`)
- [ ] Pontuação persistente
- [ ] Áudio: motor, impacto positivo, impacto negativo, vitória, derrota
- [ ] Arte de verdade no lugar dos marcadores de `Assets/Art/Placeholder/`

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

*Reordenado em 02/08/2026, com as Fases 0, 1 e 3 fechadas.*

**Próximo passo, nesta ordem:**

1. Abrir a Unity. O primeiro carregamento demora mais que o normal: ela reimporta o projeto e
   regenera o `packages-lock.json` com os pacotes removidos na limpeza. **Conferir o Console** —
   os três scripts novos (`LaneTrack`, `ShipLaneController`, `GameSceneSetup`) nunca passaram
   por um compilador.
2. **Build And Run** e sentir o arraste. É o que valida a Parte 1 da Fase 6 e, de quebra, a
   Fase 2 inteira — a nave só troca de faixa se o `TouchInput` estiver funcionando no aparelho.
3. Depois disso, a refatoração para o modelo híbrido de faixas (Fase 6, Parte 1), que depende
   da decisão sobre como o corredor cresce.

**Em paralelo, sem depender de código:**
- Fase 5, os 12 testadores — o relógio mais lento do projeto, e ainda não começou a correr
- Fase 4, a SDK Platform 36 — **prazo 31/08/2026**, e é instalação pelo Android SDK Manager

**Na sequência:** Fase 6 parte por parte, cada uma testada no aparelho antes da próxima.

**Quando o jogo tiver forma:** Fase 4 até o marco do `.aab` assinado, e reescrever a ficha da
loja com o tiro incluído.

**Por último:** Fases 7, 8 e 9.

**Caminho crítico real:** Fase 6, parte por parte. Todo o resto ou é barato, ou roda em
paralelo, ou não depende de você.
