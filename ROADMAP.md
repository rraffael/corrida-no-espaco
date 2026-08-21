# ROADMAP — Corrida no Espaço

Acompanhamento da implementação completa e funcional do projeto.
Marque cada item conforme for concluído: `[ ]` → `[x]`.

**Legenda de status das fases:** ⬜ Não iniciada · 🟡 Em andamento · ✅ Concluída

> **Este arquivo é o estado; `docs/ESCOPO.html` é o jogo.** O documento do jogo (reescrito em
> 21/08/2026) descreve os conceitos, as regras e o futuro — é o que se lê para *entender* o jogo.
> Aqui fica o acompanhamento tarefa por tarefa, com data e caixinha marcada. **Onde os dois
> divergirem, este arquivo é o atual**, porque é o que se mexe todo dia.
>
> *O escopo anterior, de 28/07/2026, era um diagnóstico do projeto de antes do commit
> `Rebuilding in unity 6.3` — descrevia o template Space Shooter, que não existe mais. Foi
> substituído, não arquivado; o Raffael guardou cópia do original fora do repositório.*

---

## 🚀 Onde estamos — 21/08/2026

**A mecânica crua está pronta, testada no aparelho e aprovada.** O tato da rodada de 07/08 foi
julgado no celular: o arranque bravo, a batida de ~3,5 s, o raspão do estilhaço e a dobra de
2,5 s — os quatro bons, sem nada a equilibrar. E a promessa da página do app **se confirmou**:
dá para vencer só desviando, então a frase fica no texto publicado.

Com isso fecha o ciclo aberto em 05/08. O jogo é uma corrida completa — variedade de obstáculo,
três fases, três dificuldades, progressão, placar e um custo de erro que o dedo sente —, está na
Play Store em teste interno, e **atualizar custa minutos**: sobe o `.aab`, o celular baixa sozinho.

### A virada de 21/08: parar de lapidar o cru, construir o meta-jogo

*Decisão do Raffael, e é o eixo de todo o resto do projeto.*

A jogabilidade crua ainda tem pormenores lapidáveis, mas lapidar mais **agora** é otimizar no
escuro, por dois motivos: ele já esgotou o que consegue julgar sozinho no polegar, e **a camada
seguinte reescreve o equilíbrio de qualquer jeito** — nave com mais defesa muda o quanto a batida
dói, escudo temporário muda a Fase 3 inteira. Polimento fino feito hoje seria refeito depois.

O alvo passa a ser o **MVP completo**, na definição do Raffael: poderes em partida, variedade de
naves com diferenciação real, evolução, recursos e economia, nível do jogador, recompensas e
conquistas, e a UI refeita. **Só quando isso estiver tão bom e tão testado quanto a mecânica
crua** é que entram arte, áudio e acabamento; depois conta e monetização; e só então o teste
fechado.

Três condições que o Raffael fixou no mesmo dia, e que mudam decisões:

- **Não há pressa de lançar.** É o que permite fazer por etapas sem sobrecarga, e é o que tira do
  caminho crítico qualquer tentativa de adiantar prazo da Play.
- **Já há gente no teste interno dando retorno**, e vai continuar havendo durante o
  desenvolvimento. A resposta *verdadeira* sobre equilíbrio, porém, ele espera do **teste
  fechado** — o interno serve para descobrir feature ruim, não para calibrar economia.
- **Documentação da Play se atualiza por etapa**, quando a feature entra — não por antecipação.
  Ver "Regra de documentação", na Fase 5.

### Retomar por aqui

- [ ] **`ShipStats` vira ficha em disco** — é o primeiro passo, e é pequeno. Hoje ele é
      `MonoBehaviour` (`Assets/Scripts/Gameplay/ShipStats.cs:13`), ou seja: a ficha da nave vive
      pendurada num objeto de cena, enquanto obstáculo e fase já são `ScriptableObject` em
      `Assets/Levels/`. Sem essa conversão, "cinco naves" vira cinco prefabs com números copiados
      na mão e evolução vira código. **Depois dela, nave nova é um `.asset`** — ver Parte 6
- [ ] **Parte 5 — poderes em partida.** Primeiro bloco de feature, porque é pura jogabilidade:
      ciclo curto e julgado no polegar, exatamente como foi a batida
- [ ] **Parte 6 — naves, diferenciação e evolução.** É o eixo do qual o resto pende
- [ ] **Parte 7 — recursos, nível, recompensas e conquistas.** Por último dos três, porque **o
      valor da moeda é definido pelo que ela compra**: os gastos têm de existir antes do dinheiro
- [ ] **Parte 8 — UI refeita.** No fim do bloco, não no começo: cada parte acima cria tela nova
      (loja, oficina, recompensa), e refazer a UI antes é refazê-la duas vezes
- [ ] ⚠️ **Fora da fila, e não depende de nada: ligar os símbolos de depuração.** Passou a
      importar em 21/08, quando entrou gente no teste interno — ver Fase 5. Entra no próximo
      `.aab` que subir

> **Princípio de construção que atravessa as quatro partes: fábrica, não estoque.** Fase nova hoje
> não é código — é um `.asset` em `Assets/Levels/`. Se poder, nave e recompensa nascerem com a
> mesma estrutura (ficha em disco + catálogo, zero código por item novo), um ano de conteúdo custa
> **horas de autoria**, e não meses de programação. É o que torna viável ter roadmap de futuro sem
> precisar ter o futuro construído antes de lançar.

> **Como subir uma atualização:** seis passos, em `docs/play-console/caminho-ate-a-play-store.md`.
> O que mais derruba envio é esquecer de subir o `AndroidBundleVersionCode` — a Play recusa dois
> arquivos com o mesmo número.

---

## Estado atual

*Revisto em 21/08/2026.*

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
  obstáculos com vida e dano, derrota por vida em zero, vitória por dobra, tabela de recordes com
  nome, e o menu com Recordes e a transição do pódio. Aprovado no aparelho, com o equilíbrio
  acertando de primeira. *(A pontuação era o tempo; virou distância na fase sem fim — ver Parte 4.)*
- **Unity Analytics desligado** — `UnityConnectSettings.asset` com tudo em `0`, e o módulo
  `com.unity.modules.unityanalytics` fora do `manifest.json`. A configuração agora concorda
  com o que a política de privacidade afirma.
- **O jogo está na Play Store** (08/08/2026) — teste interno, instalado no celular pela loja.
  App criado, dez declarações de conteúdo preenchidas, política de privacidade publicada em
  `https://rraffael.github.io/Portfolio/corrida-no-espaco/privacidade/`, página do app escrita e
  arte provisória gerada. **Distribuir versão nova deixou de custar cabo e Build And Run.**
- **O jogo tem variedade e progressão** (07/08/2026, aprovado no Editor) — três tipos de obstáculo
  com ficha própria, três fases com ficha própria, três dificuldades, tela de seleção de fase com
  cadeado, progressão em corrente única (Fácil 1→2→3, e assim por diante) e a **fase sem fim** como
  o modo avulso onde se compete. O leaderboard é de **distância**, maior é melhor, e só a fase sem
  fim pontua.
- **Bater dói de um jeito que se vê** (07/08/2026) — a batida derruba a velocidade para 25%, trava
  a nave por meio segundo e devolve o resto em dois trechos, o último arrastado: ~3,5 s de corrida.
  O estilhaço dá um raspão de menos de 1 s, que é outra coisa de propósito. E a aceleração cresce
  quanto mais devagar a nave estiver, até 300% com ela parada — o arranque ficou bravo.
- **A dobra é regra de fase, e a exigência é da dificuldade** (07/08/2026) — 2,5 s segurando a
  velocidade em todas as fases, e a velocidade pedida sobe só com a dificuldade: 16 · 17,5 · 19.
  Fase difícil se faz com obstáculo e ritmo, não pedindo mais velocidade.
- **A mecânica crua está aprovada no aparelho** (21/08/2026) — arranque, batida, raspão e dobra
  julgados no celular, os quatro bons, **nada a equilibrar**. E as três fases se vencem **só
  desviando**, o que confirma a regra de projeto de 07/08 e a frase da página do app. É o marco
  que encerra a lapidação da jogabilidade crua e abre o meta-jogo — ver "Onde estamos", no topo.

**Faltando**
- **Todo o meta-jogo** — poderes em partida, variedade e evolução de naves, recursos e economia,
  nível do jogador e recompensas, UI refeita. É o trabalho a partir de 21/08; ver Partes 5 a 8.
- ~~O tato da rodada de 07/08 nunca foi julgado num aparelho~~ — **julgado e aprovado em
  21/08/2026.**
- ~~Um passo de Montar pendente~~ — **rodou em 08/08 e o conserto foi apagado.** Conferido no
  disco: `br.com.raffael.corridanoespaco`, target 36, `productName` "Corrida no Espaço",
  versão `0.1.0`. **Não há montagem pendente.**
- Áudio: nenhum `.wav`/`.mp3`/`.ogg` no projeto.
- Ícone e splash **dentro do app** — hoje são os padrões da Unity. *(A arte da página da loja já
  tem marcador de lugar; esta linha é sobre o que aparece no celular.)*
- Arte de verdade no lugar dos marcadores de lugar, dentro e fora do jogo.
- Play Games inteiro — plugin, login e conquistas voltam na Fase 7, do zero.
- Os 16 testadores e o teste fechado — **adiados de propósito** até o jogo estar bom de lançar.

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

> **Consequência para a página do app:** `docs/play-console/pagina-do-app.md` descreve só a
> corrida ("um toque muda de faixa", "sem tiro"). Quando a parte de tiro entrar, as descrições
> pt-BR e en-US precisam ser reescritas. Não é urgente — nada é publicado antes da Fase 4.

## Fase 4 — Destravar o build de release ✅
*Fechada em 08/08/2026, com o `.aab` assinado aceito pela Play e o jogo instalado pelo celular.
O prazo da SDK 36 (31/08) foi cumprido com três semanas de folga.*

- [x] ~~Instalar a **Android SDK Platform 36**~~ — **já estava instalada.** O item novo
      **Tools → Corrida no Espaço → Conferir SDK do Android** mostrou em 07/08/2026 que a
      `6000.3.20f1` traz **34, 35 e 36**. Ou seja: **o build que quebrou em 31/07 tinha outra
      causa**, e o item ficou um mês no ROADMAP assustando à toa. Lição registrada — medir antes
      de acreditar no diagnóstico velho
- [x] **Target API Level fixado em 36** — o `ReleaseSettingsFix` pôs 36, o nome no celular e a
      versão `0.1.0`, e o resultado foi **conferido no disco em 08/08**. O prazo da Play
      (31/08/2026) estava cumprido três semanas antes. *(Este item ficou marcado como aberto até
      21/08 por descuido de escrita — a Fase 4 já estava fechada desde 08/08.)*
- [x] **Keystore novo gerado** (08/08/2026), fora do repositório, pelo *Keystore Manager* da
      própria Unity — alias `upload`. Foi refeito no mesmo dia com senha mais simples, decisão do
      Raffael: como ela mora em variável de ambiente sem criptografia, não fazia sentido uma senha
      forte ali. **Custou nada porque nada tinha sido enviado ainda** — depois do primeiro upload,
      trocar a chave custa um reset no Console
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
- [x] ~~Ativar Play App Signing~~ — **não existe o que ativar.** Para app criado novo é obrigatório
      e automático: entra em vigor sozinho no primeiro `.aab`. A consequência é a que importava —
      a chave de upload agora **pode ser resetada** se sumir, que era o buraco da chave anterior
- [x] **Marco batido em 08/08/2026:** `.aab` de release assinado —
      `Builds/Corrida no Espaço-0.1.0-1-release.aab`, 25,2 MB. Target API 36, versão 0.1.0,
      version code 1. Era o item que a Fase 4 perseguia desde o começo do projeto
      - **Efeito colateral inofensivo no `ProjectSettings.asset`:** o `AndroidKeystoreName` deixou
        de ser vazio e virou `'{inproject}: '`, que é como a Unity serializa "sem keystore
        próprio". **Não é vazamento** — não tem caminho nem nome de arquivo, e não há `.keystore`
        dentro do projeto. Vai aparecer no diff do próximo commit; pode passar
- [x] ~~Adicionar `/.utmp/` ao `.gitignore`~~ — já estava lá (`.gitignore:14`); item era engano meu

## Fase 5 — Play Console, em paralelo 🟡
Não depende de código. **Deixou de ser o relógio mais lento do projeto** em 08/08/2026: o app está
no ar, as declarações estão preenchidas e atualizar leva minutos. O que sobra aqui não é mais
espera — é o que só se resolve perto do lançamento.

- [x] Preparar o teste fechado no papel — `docs/play-console/teste-fechado.md`: regra, planilha
      para 16 e-mails (convidar mais que 12, porque gente some), texto de convite e checklist
- [x] **Conta de desenvolvedor verificada** (07/08/2026) — era o que travava criar o app no
      Console. Com isso a Fase 5 deixou de depender de espera e virou o caminho crítico
- [x] **Caminho até a Play Store escrito** — `docs/play-console/caminho-ate-a-play-store.md`,
      dividido em duas etapas: **teste interno agora** (o app na loja, para ele testar baixando
      pela Play) e **teste fechado depois** (a regra dos 12/14 dias, que destrava produção)
- [x] **Etapa 1 — teste interno no ar** (08/08/2026), e **com testadores de verdade desde então**:
      o Raffael pôs gente conhecida na faixa interna e já colhe retorno sobre as features. Isso
      **não gasta** a ficha social do teste fechado — teste interno não conta para os 14 dias
- [ ] **Análise de dados — entre o teste interno e o teste fechado** *(decidido pelo Raffael em
      21/08/2026)*. Entra depois de o meta-jogo existir e antes de o teste fechado começar, que é
      quando os dados precisam estar fluindo para a resposta verdadeira sobre equilíbrio aparecer.
      **Mexe na política de privacidade e na Segurança dos dados** — as duas se atualizam junto,
      no dia em que a telemetria entrar, e não antes
- [ ] Montar a lista de **12 testadores** para o teste fechado (opt-in contínuo por 14 dias)
      — **adiado de propósito em 07/08/2026, reafirmado em 21/08.** Junte **16** quando a hora
      chegar (se a contagem cair de 12, o contador reinicia), mas não agora: é uma ficha social
      que se joga uma vez, e gastá-la numa versão que ainda vai mudar muito é desperdício.
      A hora é **depois do MVP completo, com arte, áudio e monetização prontos** — é lá que o
      Raffael quer a leitura verdadeira do equilíbrio
- [x] Escrever a política de privacidade — `docs/politica-de-privacidade.md` e `docs/privacy-policy.md`
- [x] **Página web da política pronta** — `docs/privacidade/index.html`, autocontida (sem CSS nem
      fonte externa), com **pt-BR e en-US na mesma página**, então uma URL só serve aos dois
      idiomas. É só publicar onde quiser
      - **Os dois textos foram corrigidos em 08/08/2026:** descreviam o login do Play Games, que
        saiu do projeto em 02/08. Publicar assim contradiria a declaração de Segurança dos dados
        ("não coleta nada") — e é exatamente esse tipo de divergência que a Play compara
- [x] **Publicada e aceita pelo Console** (08/08/2026). Mora no portfólio, em
      `Portfolio/public/corrida-no-espaco/privacidade/index.html`, e vai ao ar sozinha no push
      para `master` pelo workflow do GitHub Pages.
      URL: **`https://rraffael.github.io/Portfolio/corrida-no-espaco/privacidade/`**
      - **E-mail de contato do app: `suporte.raffael@gmail.com`** (trocado em 08/08/2026, em toda
        a documentação e na página publicada). O `raffaelcr.ti@gmail.com` segue sendo só a conta
        Google dona do Console e do repositório — identidade de conta, não contato do app
      - Por que dentro de `public/`: o Next copia essa pasta literalmente para o `out/`, sem
        passar pelo build. HTML solto ali funciona como está, e não vira componente
      - Por que sob `corrida-no-espaco/`: a política é **do jogo**, não do portfólio. Num site
        pessoal, uma rota `/privacidade/` seria lida como a política do próprio site — e o dia em
        que houver um segundo app, cada um tem a sua
- [x] Desligar o Unity Analytics legado antes de publicar — feito em 02/08/2026:
      `UnityConnectSettings.asset` com `m_Enabled: 0` no topo e no bloco `UnityAnalyticsSettings`
      (`m_InitializeOnStartup: 0` junto), e o módulo `com.unity.modules.unityanalytics` fora do
      `manifest.json`. A configuração agora concorda com o texto da política
- [x] **Página do app reescrita para o jogo que existe** (08/08/2026) —
      `docs/play-console/pagina-do-app.md`, pt-BR e en-US: nome, descrição curta com contagem de
      caracteres, descrição completa e a tabela de arte com o que é obrigatório e o que dá para
      pular. *(Renomeado de `ficha-da-loja.md` em 07/08 — "ficha" não dizia nada.)*
      - O texto anterior era de 02/08 e prometia um jogo abandonado: sem barra de vida, um toque
        para trocar de faixa, impulsos para coletar, e nenhuma menção a tiro
      - **Decidido não traduzir o nome:** "Space Race" é genérico demais — termo histórico, muitos
        produtos homônimos, afundaria na busca. "Corrida no Espaço" nos dois idiomas
      - ✅ **A frase que dependia do teste está confirmada** (21/08/2026): *"dá para vencer só
        desviando"* se sustenta nas três fases, no aparelho. **Fica no texto publicado.**
- [x] **Ícone e gráfico de destaque provisórios gerados** (08/08/2026) — menu **Gerar arte da
      loja** (`Assets/Editor/Tools/StoreArt.cs`) escreve `docs/play-console/arte/icone-512.png`
      (82 KB) e `destaque-1024x500.png` (108 KB), opacos e no tamanho exato.
      **Fora de `Assets/` de propósito:** lá dentro a Unity os importaria como textura e eles
      entrariam no `.aab`. **A `StoreArt.cs` sai do projeto no dia da arte de verdade**
- [ ] **Arte de verdade da loja** — ícone, gráfico de destaque e screenshots caprichadas.
      **Migrou para a Fase 8**, junto com a arte do jogo: são o mesmo trabalho, e fazer a arte da
      loja antes da arte do jogo é fotografar marcador de lugar
- [ ] ⚠️ **Ligar os símbolos de depuração — a hora chegou.** O Console avisa a cada envio que
      faltam o arquivo de desofuscação e os símbolos nativos. Ficaram ignorados de propósito
      enquanto o único aparelho era o do Raffael, porque `.\tools\logcat.ps1` dá mais detalhe que
      o Console. **Isso mudou: já há outras pessoas no teste interno** (21/08/2026), e quando
      alguém disser "fechou sozinho" não vai dar para pegar o celular da pessoa. Sobe junto do
      próximo `.aab`. Ver `caminho-ate-a-play-store.md`, Etapa 2
- [x] ~~Quando a conta sair da verificação:~~ **app criado no Console** (08/08/2026), com o
      `applicationId` novo `br.com.raffael.corridanoespaco`, aceito sem briga
- [ ] Criar o projeto novo no Play Games Services — **só na Fase 7**, junto com login e conta
- [x] **As dez declarações de conteúdo do app documentadas** —
      `docs/play-console/declaracoes-do-app.md` (08/08/2026), com a resposta de cada uma, o
      questionário IARC pergunta por pergunta e — o que mais importa a longo prazo — uma seção
      **"Gatilhos de revisão"**: que feature futura muda qual declaração. Anúncios, compras, gacha,
      Play Games, leaderboard online, arte e áudio já estão mapeados.
      *Reúne os antigos `data-safety.md` e `iarc-e-publico-alvo.md`, apagados — três arquivos para
      conferir era o oposto do que se queria*
      - **A declaração de ID de publicidade foi verificada no bundle**, não chutada: o `.aab`
        declara só `android.permission.INTERNET`, sem `AD_ID`
- [x] **As dez declarações preenchidas no Console** (08/08/2026) — incluindo o IARC, a Segurança
      dos dados e a URL da política. A seção *Conteúdo do app* ficou **sem pendência**. O gabarito
      do que foi respondido está em `docs/play-console/declaracoes-do-app.md`

### Regra de documentação — atualizar por etapa, nunca por antecipação

*Decidida pelo Raffael em 21/08/2026.* O jogo vai mexer em várias declarações da Play até o
lançamento — conta de jogador, monetização, análise de dados, anúncios. **Nada disso se declara
antes de existir.** A regra é uma só:

> **Entrou a feature → atualiza no mesmo dia o que ela mexe.** Entrou conta? política de
> privacidade e Segurança dos dados. Entrou monetização? declaração de compras, público-alvo e
> página do app. Entrou telemetria? política e Segurança dos dados de novo.

**Por que não adiantar:** o futuro é incerto, feature planejada sai de escopo e feature não
planejada entra na frente. Declarar hoje o que talvez exista em três meses cria divergência entre
o que o Console afirma e o que o `.aab` faz — e é exatamente esse tipo de divergência que a Play
compara. Já aconteceu neste projeto: a política descrevia o login do Play Games depois de ele ter
saído do código, e foi preciso corrigir antes de publicar.

**O mapa de quem mexe em quê já existe:** `docs/play-console/declaracoes-do-app.md`, seção
*"Gatilhos de revisão"* — anúncios, compras, gacha, Play Games, leaderboard online, arte e áudio
já estão mapeados feature por feature. É o arquivo a abrir toda vez que uma dessas entrar.

> Regras da Play mudam com frequência — reconfirmar cada item no Console, não tratar como fato.

## Fase 6 — O jogo em si 🟡
O grosso do trabalho. Construído **por partes**: cada parte tem de estar rodando no aparelho
antes de a próxima começar. O Raffael decide o conceito de cada uma na hora, vendo a anterior
funcionar.

**Partes 1 a 4 fechadas e aprovadas no aparelho** — é a mecânica crua, e ela está pronta.
**Partes 5 a 8 são o meta-jogo**, aberto em 21/08/2026: poderes, naves, recursos e UI. Fechar a
Parte 8 é fechar o **MVP**.

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
- [x] ~~Apagar o `RaceSetup.cs` depois de commitado~~ — **cancelado em 06/08/2026**: o
      `Montar.cs` chama `RaceSetup.Setup()` direto, então ele deixou de ser ferramenta de uso
      único e virou peça de rotina. Ver a nota no fim da Parte 4

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
- [x] `Services/ScoreBoard.cs` — top 10 em PlayerPrefs, com nome. É por aqui que um leaderboard
      online passaria na Fase 7.
      *~~A pontuação é o tempo até entrar em dobra, menor é melhor~~ — **mudou na Parte 4**: a
      pontuação virou a distância percorrida na fase sem fim, maior é melhor, e as fases numeradas
      deixaram de pontuar.*
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
- [x] ~~Apagar as ferramentas de montagem já rodadas (`RaceSetup`, `BattleSetup`, `MenuSetup`)~~
      — **cancelado em 06/08/2026**, ver a nota no fim da Parte 4

> A página do app (`docs/play-console/pagina-do-app.md`) descreve só a corrida, "sem tiro".
> **Agora tem tiro** — as descrições pt-BR e en-US precisam ser reescritas antes de publicar.

### Parte 4 — Variedade e desafio ✅
O jogo funciona, mas toda partida é igual à anterior: um tipo de obstáculo, um ritmo, uma nave.
O objetivo desta parte é dar ao jogador motivo para jogar de novo.

> **Fechada em 21/08/2026**, com os Blocos A e B aprovados no Editor em 07/08 e a rodada inteira
> **aprovada no aparelho** em 21/08 — tato bom, nada a equilibrar. O backlog "Depois" que ficava
> no fim desta seção **virou as Partes 5 a 8**, que é o trabalho a partir de agora.

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
- [x] ~~**Validação da fase invencível**~~ — **removida em 06/08/2026, junto com o teto de
      velocidade.** Ela existia para acusar dobra acima do teto do `RaceSpeed`; sem teto, não há
      mais meta inalcançável: o ganho passivo sozinho chega a qualquer velocidade de dobra, é só
      questão de tempo. Volta com `git checkout <commit anterior> -- Assets/Scripts/Gameplay/RaceDirector.cs`
      se o teto um dia voltar
- [x] **Dobra com tempo de carga** (decidido pelo Raffael em 06/08/2026) — chegar à velocidade
      não vence mais na hora: a nave precisa **segurar** a velocidade pelo tempo que a fase pedir
      (5 s hoje). Abaixo da velocidade a carga **escoa** pela metade do ritmo, em vez de zerar:
      uma batida no fim custa caro sem apagar a corrida inteira.
      `warpChargeSeconds` e `warpDecayRate` ficam no `RaceDirector`
      - Serve de **ajuste de dificuldade principal da fase**: mexer no tempo de dobra é mexer em
        quanto o jogador tem de aguentar já correndo depressa demais para desviar com folga
- [x] `UI/WarpChargeHud.cs` — contagem regressiva acima do velocímetro, aparecendo só quando há
      carga. Muda de cor quando a carga está escoando

**Bloco A — obstáculos e fichas de fase** *(escrito em 06/08/2026, aprovado no Editor em 07/08)*
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
- [x] **Rodadas em 06/08/2026 às 02:15** — `fases-e-obstaculos` e, em seguida, `combate`.
      O registro está em `UserSettings/corrida-ferramentas.json`, fora do git
- [x] **Testado no Editor em 07/08/2026** — os três tipos aparecem na fase certa, a Barcaça tranca
      duas faixas, o Casulo solta estilhaço e a regra da fuga garantida segura a leva
- [x] **Repetido no aparelho em 21/08/2026** — os três tipos, a Barcaça de duas faixas, o Casulo
      com estilhaço e a regra da fuga garantida, todos se comportando como no Editor
- [x] ~~Equilibrar: vida e dano dos três tipos, janelas de estreia e os fatores de dificuldade~~ —
      **nada incomodou nem no Editor (07/08) nem no aparelho (21/08).** Os números ficam como
      estão até o meta-jogo mexer com eles, que é quando serão reavaliados de qualquer forma

**Bloco B — fases, progressão e leaderboard** *(escrito em 06/08/2026, aprovado no Editor em 07/08)*

> **A divisão que o Bloco B assentou**, decidida pelo Raffael em 06/08/2026: as fases numeradas
> **ensinam o jogo** — obstáculo novo, desafio novo, e o prêmio é a próxima fase; a **fase sem fim
> é onde se compete**, e é a única com placar. Foi isto que tirou o campo de nome do painel de
> vitória e mudou a pontuação de tempo para distância.

- [x] **Ganho passivo de velocidade** *(pedido do Raffael em 06/08/2026)* — hoje só destruir
      obstáculo acelera a corrida, o que obriga a atirar. A nave passa a ganhar velocidade
      **sozinha, com o tempo, proporcional ao atributo de aceleração da ficha** — de leve, para
      quem preferir desviar a destruir também chegar lá, só que devagar.
      `RaceSpeed.AdvanceTarget` empurra o `Target`; o fator é `passiveGainFactor` (0,04 da
      aceleração por segundo, ou 0,16 u/s com a ficha atual — perto de 45s do cruzeiro à dobra)
      - [x] **Dois regimes de velocidade** *(ajuste do Raffael em 06/08/2026)* — o ganho lento vale
            **só acima da velocidade de cruzeiro**. Abaixo dela, a nave se recupera na **aceleração
            cheia da ficha**: sair de 1 e voltar aos 8 do cruzeiro leva menos de 2s, e não 45.
            O motivo: **o caminho principal até o cruzeiro é a aceleração da nave, não destruir
            obstáculo**. Acima do cruzeiro é que o tiro vira atalho — um bônus, não o foco.
            *(O Raffael cogitou remover o tiro no futuro; este desenho já deixa o jogo de pé sem ele.)*
            - ~~Consequência a vigiar: abaixo do cruzeiro a freada da batida se desfaz em menos de
              um segundo~~ — **sem efeito desde 07/08/2026**: bater deixou de ser uma freada e virou
              uma parada com retomada em dois trechos, que custa o mesmo em qualquer velocidade
      - [x] `ResumeCruise()` apagado — ficou sem uso e passou a mentir: ele saltava para o cruzeiro
            de uma vez, que é justamente o contrário da recuperação por aceleração
- [x] **Aceleração que cresce quanto mais devagar a nave estiver** *(pedido do Raffael em
      07/08/2026)* — abaixo do cruzeiro a aceleração da ficha é multiplicada por um bônus que vai
      de **+200% com a nave parada** (300% do total) a **0% no cruzeiro**, proporcional ao caminho
      que falta andar. Acima do cruzeiro não há bônus nenhum: lá quem manda é o ganho passivo, e
      turbinar a aceleração só faria a nave alcançar mais depressa um alvo que sobe devagar de
      propósito.
      `RaceSpeed.AccelerationNow` é a conta, e o botão é o `lowSpeedAccelerationBonus` (2)
      - **Vale nos dois lugares**, e tem de valer: o `AdvanceTarget` sobe o alvo com o bônus e o
        `Update` sobe a velocidade com o bônus. Abaixo do cruzeiro os dois andam colados, então
        deixar o alvo na aceleração limpa esconderia o bônus justamente no caso que ele resolve
      - Efeito: com aceleração 4 e cruzeiro 8, o zero-ao-cruzeiro caiu de **2s para ~1,1s**. O
        arranque da fase ficou bravo, e a volta depois de uma freada também
- [x] **A batida virou um tranco, e não um desconto** *(desenho do Raffael em 07/08/2026)* — era
      um empurrão de −2 u/s no alvo, que o jogador não sentia: o número baixava e voltava.
      Agora `RaceSpeed.Crash()` faz três coisas, nesta ordem:
      1. **A velocidade despenca na hora** para `crashSpeedFraction` (25%) do que era — nada de
         desacelerar bonitinho, é um tranco. **Fração e não valor fixo** *(ajuste do Raffael em
         07/08/2026)*: com o número fixo de 1 u/s a nave parecia **parar de vez**, o que era
         drástico demais e matava a sensação de corrida. Em 25% o tranco pesa igual em qualquer
         velocidade e o cenário continua andando
      2. **`recoveryDelaySeconds` (0,5 s) sem reagir** — o instante de nave morta
      3. **A volta em dois trechos:** corre até `fastRecoveryFraction` (80%) do que tinha, e se
         **arrasta nos 20% finais** por `finalStretchSeconds` (2 s). O trecho rápido é rápido de
         graça — a nave está lá embaixo, então o bônus de baixa velocidade está no talo
      - **A troca de fundo:** bater passou a custar **tempo**, não velocidade. Custo que o jogador
        vê acontecendo, em vez de ler num número duas unidades menor. Uma batida a 14 u/s tira
        perto de **3,5 segundos** da corrida — e quem manda nesse número é o trecho arrastado, não
        a profundidade da queda
      - O `speedPenaltyOnCrash` das fichas **continua valendo por cima**, como custo permanente: é
        ele que decide o destino da retomada (`velocidade anterior − punição`). **Zerar o campo nas
        três fichas de `Assets/Levels/Obstaculos/` deixa a batida custando só os segundos** — é a
        ideia original do Raffael em estado puro, e é um valor no Inspector, não código
      - O piso do destino é o **cruzeiro**: abaixo dele a nave voltaria para lá de qualquer jeito, e
        mirar mais baixo criaria um degrau lento no meio da subida
      - `Nudge` voltou a ser só o empurrão do abate. Bater é `Crash`, que é outra coisa —
        `Obstacle.CheckCrash` chama o novo
      - **Quem manda no custo é o trecho arrastado**, e não a profundidade da queda. Mexer no
        `crashSpeedFraction` muda o susto; mexer no `finalStretchSeconds` muda o preço
- [x] **Estilhaço dá um raspão** *(decidido pelo Raffael em 07/08/2026, no mesmo dia)* —
      `RaceSpeed.Graze()`: a nave perde 15% da velocidade e 0,15 s de reação, e **retoma no ritmo
      normal, sem o trecho arrastado**. Custa menos de um segundo, contra os ~3,5 da batida.
      - **Não mexe no alvo de propósito.** O raspão cobra só o tempo perdido; a velocidade
        conquistada continua esperando. Um Casulo solta vários estilhaços, e cada um levar
        velocidade embora somaria uma punição de batida em prestações
      - Um raspão durante a retomada de uma batida **não encurta** a pausa da batida — a pausa
        maior é que manda
      - Botões: `grazeSpeedLoss` (0,15) e `grazeDelaySeconds` (0,15), no `RaceSpeed`
- [x] **A dobra recalibrada** *(pedido do Raffael em 07/08/2026)* — carga mais curta, velocidade
      mais alta, e a exigência saiu da fase:
      - **`warpChargeSeconds` = 2,5 s em todas as fases** (era 5, 6 e 7). Segurar a velocidade por
        7 segundos era o pedaço mais longo da corrida
      - **`warpSpeed` = 15 em todas as fases.** A Fase 3 pedia 16, e essa diferença por fase
        acabou: **fase difícil se faz com obstáculo e ritmo, não pedindo mais velocidade**
      - **A escada de exigência é da dificuldade**, e só dela: Fácil **+1**, Normal **+2,5**,
        Difícil **+4** (eram 0, +1,5 e +3). Dá 16 · 17,5 · 19, igual nas três fases. Até o Fácil
        soma, porque a carga caiu pela metade e a exigência tinha de subir para todo mundo
      - O `LevelDefinition.warpSpeed` ganhou tooltip dizendo para manter igual entre as fases —
        é o tipo de regra que se perde se ficar só aqui
      - **Precisou de conserto pontual.** O `LevelSetup` promete não sobrescrever asset existente,
        e as fichas são de 06/08 — mexer só nos padrões dele não chegaria nos assets do Raffael.
        Ver "Consertos pontuais", abaixo
      - A **fase sem fim não foi tocada**: lá não há dobra, e os 999 dela são o jeito de dizer isso
- [x] **Regra de projeto: toda fase tem de ser vencível sem atirar** *(Raffael, 07/08/2026)* —
      valia para a Fase 1 e passou a valer para **todas**, inclusive as que ainda não existem.
      Amarra o equilíbrio de qualquer fase nova: se o ganho passivo sozinho não fecha a dobra no
      tempo da fase, a fase está errada — não o jogador
- [x] **Ganho por abate virou atributo da nave** *(pedido do Raffael em 06/08/2026)* —
      `ShipStats.KillSpeedGain` = aceleração × `killGainFactor` (0,16), ou **0,64 u/s por abate**
      na nave inicial. A ficha do obstáculo deixou de guardar velocidade: `speedBonusOnKill` virou
      `killWeight`, o **peso relativo** do obstáculo (Detrito 1,5 · Barcaça 2,5 · Casulo 2,0), e
      quem converte peso em velocidade é a nave.
      - A renomeação leva `[FormerlySerializedAs]`, então os três assets em
        `Assets/Levels/Obstaculos/` **mantêm os números** que já tinham
      - Proporcional à **aceleração**, e não à velocidade de cruzeiro: o ganho passivo também é,
        então um abate vale sempre os mesmos ~4 segundos de paciência em qualquer nave. Amarrar ao
        cruzeiro andaria para trás — nave mais rápida tem *menos* caminho até a dobra da fase, e o
        abate dela deveria valer menos, não mais
      - Efeito no equilíbrio: do cruzeiro (8) até a dobra (15) são 7 u/s, ou **11 Detritos** contra
        45s de paciência. Antes eram 5 abates. Destruir compensa e continua sendo o caminho rápido,
        mas deixou de ser o único que fecha a fase em tempo razoável
      - [x] **Decidido pelo Raffael em 06/08/2026: vai ATÉ a dobra.** Recomendei parar um pouco
            antes e ele escolheu o contrário — desviar vira estratégia completa e uma partida
            paciente vence sem um tiro. O tiro passa a ser atalho, não obrigação.
            Quem impõe o teto é o `RaceDirector.ApplyPassiveCeiling`, que é o único lugar que
            sabe qual é a dobra da fase
      - [x] ~~Cuidado ao equilibrar: o ganho passivo desfaz sozinho a punição da batida~~ —
            **resolvido em 07/08/2026 pelo desenho novo de batida.** O grosso do custo passou a ser
            a parada e a retomada arrastada, que o ganho passivo não encurta. O que ele ainda
            desfaz é só o `speedPenaltyOnCrash` que sobra por cima
      - [x] Na fase sem fim não há dobra, então lá o ganho passivo **não para nunca**: a corrida
            fica perigosa com o tempo mesmo para quem não atira em nada
- [x] **Teto de velocidade removido** *(pedido do Raffael em 06/08/2026)* — o `maxSpeed` saiu do
      `RaceSpeed`. Nada mais limita a velocidade: destruir obstáculo empurra a corrida acima da
      dobra à vontade, e a fase sem fim acelera até a nave cair. O único piso é o zero, para uma
      sequência de batidas não empurrar a corrida para trás.
      **`PassiveCeiling` não é teto de velocidade** — limita só o ganho passivo, e é a dobra da fase
      - [ ] **Consequência a vigiar no teste:** o acerto é distância medida por frame
            (`Obstacle.CheckCrash`), então velocidade alta o bastante faz o obstáculo **atravessar
            a nave sem bater**. A conta: passa a falhar quando a velocidade supera
            `2 × crashDistance ÷ deltaTime` — perto de **75 u/s a 60 fps**, ou **37 u/s a 30 fps**.
            Só a fase sem fim chega lá, e demora. Quando incomodar, o conserto é testar o trecho
            percorrido no frame em vez do ponto final
- [x] **Seleção de fase e dificuldade no menu** — o "Jogar" abre uma tela com **todas as fases do
      catálogo**, a dificuldade num seletor suspenso e um **cadeado** no que ainda não abriu.
      A lista é montada em runtime a partir do `LevelCatalog`: fase nova no catálogo aparece no
      menu sem rodar ferramenta nenhuma de novo. Escolhida a fase, a transição do pódio continua
      no meio do caminho, como antes
      - `UI/LevelSelectMenu.cs` desenha, `UI/LevelSelectRow.cs` é a linha, e a montagem é o
        **Montagem → Montar seleção de fase**. O cadeado é PNG gerado em código
        (`PlaceholderArt.Padlock`), branco, para a cor sair do `Image`
- [x] **Progressão em corrente única**, como o Raffael desenhou em 06/08/2026:
      Fácil 1→2→3, Normal 1→2→3, Difícil 1→2→3. A **fase sem fim ficou fora da corrente e aberta
      desde o começo** (decidido no mesmo dia): ela é o modo avulso, e quem só quer sobreviver não
      precisa fechar nove fases antes de tentar.
      `Services/LevelProgress.cs` guarda **um int** — quantos degraus caíram —, e o degrau é
      calculado a partir do catálogo: entrar uma quarta fase não invalida o que o jogador já tem.
      Quem registra a vitória é o `RaceDirector`, que também conta no painel o que foi destravado
- [x] **Fase sem fim fechada** — sem dobra, endurece sozinha pelo ganho passivo sem teto, e acaba
      quando a nave cai. **Cair lá não é derrota**: o painel de fim é o "Corrida encerrada", com a
      distância e a tabela. Chamar aquilo de derrota puniria o jogador pela única coisa que a fase
      permite que aconteça
- [x] **Leaderboard só da fase sem fim, em distância percorrida** *(decidido pelo Raffael em
      06/08/2026)*. O cuidado que estava registrado aqui era real e foi resolvido: a pontuação
      **inverteu de sentido**. Era *tempo até a dobra*, crescente, menor é melhor; virou
      *distância*, **decrescente, maior é melhor**.
      - A distância é a **velocidade integrada a cada frame**, e não tempo × velocidade final:
        assim acelerar cedo vale mais do que acelerar no último segundo
      - **Chave nova no PlayerPrefs** (`corrida.recordes-distancia`). Reaproveitar a antiga
        misturaria segundos com distância na mesma lista, e um tempo de 8s viraria "0,8 km" na
        tabela. Quem tinha recorde antigo começa a tabela nova vazia
      - `ScoreBoard.DisplayScale` (10) transforma unidade de mundo em "km" no painel, do mesmo
        jeito que o `displayScale` do `SpeedHud` faz com a velocidade
      - Na fase sem fim, a linha do HUD que mostrava a contagem da dobra passa a mostrar a
        **distância subindo** — lá ela ficaria vazia a corrida inteira, e é a pontuação que o
        jogador precisa ver para decidir se arrisca mais
- [x] **As fases de progressão perderam o placar** — o painel delas virou "Fase concluída!", com o
      tempo e **o que foi destravado**, sem campo de nome e sem tabela. É o que separa os dois
      tipos de fase: as numeradas ensinam o jogo e apresentam obstáculo novo, e o prêmio é a
      próxima fase; a sem fim é onde se compete

> **As ferramentas de montagem pararam de ser descartáveis** (06/08/2026). O combinado do
> `CLAUDE.md` era apagar a ferramenta depois de rodar, para o menu não acumular item morto. Com o
> `Montar.cs` chamando `LevelSetup`, `RaceSetup`, `BattleSetup`, `MenuSetup` e `LevelSelectSetup`
> direto, elas viraram **dependência de código** de uma ferramenta de rotina: apagar qualquer uma
> quebra a compilação do `Montar`. E o problema que a regra resolvia sumiu junto — o Raffael roda
> um item só, então o submenu não pesa mais na decisão dele.
>
> A regra continua valendo para **conserto pontual** (`Correções/`), que é o caso em que ela
> nasceu: script que arruma uma coisa uma vez e não tem por que sobreviver.

### Consertos pontuais
*Registro histórico das Partes 1 a 4. As partes novas do meta-jogo começam logo abaixo, na Parte 5.*

| Ferramenta | O que fez | Estado |
|---|---|---|
| `Correções/WarpTuningFix.cs` | Pôs 2,5 s de carga e dobra 15 nas três fichas de fase, e a escada de dobra por dificuldade (+1 · +2,5 · +4) no catálogo. Existiu porque o `LevelSetup` não sobrescreve asset criado antes. | **Rodou e foi apagado** (07/08/2026) |
| `Correções/ReleaseSettingsFix.cs` | Target API Level 36, nome no celular "Corrida no Espaço" (era `corrida-no-espaco`) e versão `0.1.0` (era `1`). O version code ficou em 1, intocado. **Tinha um defeito — ver abaixo.** | **Rodou e foi apagado** (07/08/2026) |
| `Correções/ApplicationIdFix.cs` | Devolveu o pacote a `br.com.raffael.corridanoespaco`, desfazendo o estrago do anterior. | **Rodou e foi apagado** (08/08/2026) |

### O bug do `applicationId` — 08/08/2026

O `ReleaseSettingsFix` mudou o `productName` e **não reafirmou o pacote**. A Unity deriva o
`applicationIdentifier` de `com.<companyName>.<productName>` enquanto ele não for imposto de novo,
então trocar o nome do produto **regerou o pacote**: `br.com.raffael.corridanoespaco` virou
`com.Raffael.CorridanoEspao`. O `.aab` saiu com o pacote errado e a Play recusou o upload — foi a
recusa que revelou o problema, não nenhuma checagem nossa.

**Duas defesas entraram para o erro não voltar calado:**

- `Assets/Editor/Tools/ProjectIdentity.cs` — o pacote definitivo virou uma constante, num lugar só
- O `PreflightCheck` passou a **exigir esse valor exato**, e divergência agora é **erro**, não
  aviso. Antes ele só reclamava se o pacote fosse o antigo (`com.Raffael.corridanoespaco`); um
  terceiro valor passava calado, que foi exatamente o que aconteceu

**Regra que fica:** mexeu no `productName`, reafirme o `applicationIdentifier` na sequência — e
rode **Conferir configuração** antes de todo build de release. Era para isso que ele existia.

O resultado do `WarpTuningFix` foi **conferido nos assets** antes de ele sair: `Fase1`, `Fase2` e
`Fase3` com `warpSpeed: 15` e `warpChargeSeconds: 2.5`, `FaseInfinita` intocada (999 / 0), e o
catálogo com os bônus 1 · 2,5 · 4. Saíram junto o `.meta`, a pasta `Correções/` (que ficou vazia),
o passo no `Montar.cs` e as entradas no `ProjectTools`.

Para trazê-lo de volta, se um dia fizer falta:
`git checkout <commit de 07/08/2026> -- "Assets/Editor/Tools/Correções/"`. **Na prática ele não
faz falta:** os números dele agora são os padrões do `LevelSetup`, então um projeto gerado do zero
já nasce certo — ele só existia para alcançar os assets criados em 06/08.

### Parte 5 — Poderes em partida ⬜
*Aberta em 21/08/2026. Primeiro bloco do meta-jogo.*

**Por que esta é a primeira:** é pura jogabilidade. Ciclo curto, sem depender de nave nova nem de
economia, e o Raffael julga no polegar — exatamente como julgou a batida. Além disso ela mexe no
mesmo tecido que a mecânica crua, então é onde eventuais pormenores do cru vão aparecer de novo,
já no contexto novo.

- [ ] **Passo zero: `PowerUpDefinition` como `ScriptableObject`**, no espírito do
      `ObstacleStats` — o que o poder faz, quanto dura, com que frequência aparece, arte. Mais um
      catálogo em `Resources/`, como o `LevelCatalog`. **Poder novo tem de ser um `.asset`**, nunca
      código: é a mesma aposta que fez fase nova sair de graça
- [ ] **Quais poderes** *(decisão do Raffael)*. Os três que ele já tinha listado no backlog antigo:
      **reparo**, **escudo temporário** e **tiro rápido**
- [ ] **Poderes ruins (power-down)** *(ideia do Raffael em 21/08/2026)* — o outro lado da moeda.
      Decidir se caem como o bom e o jogador precisa **desviar do item**, se são efeito de
      obstáculo, ou os dois
- [ ] **Como o poder chega** — cai na faixa e se pega passando por cima? Solta de obstáculo
      destruído? Aparece por tempo? Muda o desenho da corrida: item que cai numa faixa é mais uma
      razão para trocar de faixa, que é o que o estilhaço já faz
- [ ] **Cuidado herdado, e é o mesmo de sempre:** a **regra da fuga garantida** olha a leva inteira
      para nunca fechar todas as faixas. Item que ocupa faixa entra nessa conta, ou volta a matar
      por sorteio
- [ ] **Cuidado novo — o escudo mexe no que já foi aprovado.** A batida custa ~3,5 s, e foi assim
      que ficou boa. Escudo que anula batida anula o custo que dá peso à corrida inteira; decidir
      se ele **absorve** (vira raspão) ou **anula** é a decisão de equilíbrio desta parte
- [ ] Testar no aparelho antes de a Parte 6 começar — regra das partes, vale aqui como valeu antes

### Parte 6 — Naves: variedade, diferenciação e evolução ⬜
*Aberta em 21/08/2026. **É o eixo do qual o resto pende** — evolução, loja e recompensa todas se
penduram na resposta a "o que é uma nave".*

- [ ] **`ShipStats` vira `ScriptableObject`** — o passo que destrava tudo, e é pequeno. Hoje é
      `MonoBehaviour` (`Assets/Scripts/Gameplay/ShipStats.cs:13`): a ficha vive pendurada num
      objeto de cena, enquanto obstáculo e fase já são asset em disco. **Feito isso, nave nova é um
      `.asset`**, e evolução vira número, não código
      - Cuidado na conversão: quem lê a ficha hoje é `ApplyShipStats(cruzeiro, aceleração)`,
        `TakeHit()`, `DamageAfterDefense()` e `KillSpeedGain`. A parte *de cena* (a instância da
        nave viva) continua sendo componente; o que sai para o asset são os **números**
- [ ] **Catálogo de naves**, no mesmo molde do `LevelCatalog`, para a loja e a seleção lerem de um
      lugar só e nave nova aparecer sem rodar ferramenta
- [ ] **O que diferencia uma nave da outra** *(decisão do Raffael)*. Os atributos que já existem e
      já estão equilibrados: **aceleração** (manda no arranque, na retomada da batida e no ganho
      passivo), **velocidade de cruzeiro**, **defesa em %**, **vida** e **ganho por abate**
      (`killGainFactor`). A pergunta de projeto é se a diferença fica só nesses números ou se cada
      nave ganha **algo que só ela faz** — e essa é a diferença entre "nave melhor" e "nave outra"
      - **Amarra que continua valendo:** toda fase tem de ser vencível **sem atirar**, em qualquer
        nave. Nave nova que não fecha a dobra no tempo da fase sem um tiro está errada — ou a fase
        está
- [ ] **Evolução** *(decisão do Raffael)* — sobe atributo por atributo? Sobe a nave inteira em
      níveis? Tem teto? O que ela consome sai da Parte 7, então aqui se define **a forma**, e o
      preço fica para depois
- [ ] **Barreira que impede naves fracas de avançar** *(ideia antiga do Raffael, adiada por ele —
      é aqui que ela cabe)*. Só faz sentido quando existe nave forte e nave fraca
- [ ] Testar no aparelho antes de a Parte 7 começar

### Parte 7 — Recursos, nível, recompensas e conquistas ⬜
*Aberta em 21/08/2026. **Por último dos três blocos de mecânica**, e o motivo é simples: o valor da
moeda é definido pelo que ela compra. Os gastos têm de existir antes do dinheiro.*

- [ ] **Moeda comum e moeda premium** *(desenho do Raffael: "normalmente um free e um vip")* —
      definir o que cada uma compra e, principalmente, **o que a premium NÃO compra**. É a linha
      que separa "atalho" de "pagar para vencer", e ela é mais fácil de traçar agora do que depois
- [ ] **De onde vem a moeda comum** — corrida terminada, distância na fase sem fim, primeira
      vitória de cada fase, missão diária? Cada torneira dessas muda o ritmo do jogo todo
- [ ] **Nível do jogador e recompensas** — o que sobe o nível e o que o nível dá. Cuidado de
      projeto: se o nível dá poder, ele vira uma segunda evolução paralela à da nave e as duas
      brigam pelo mesmo espaço
- [ ] **Conquistas próprias, que pagam em recurso** *(pedido do Raffael em 21/08/2026)* — conquista
      do **jogo**, com regra e recompensa em moeda, nave ou o que a economia tiver. É mais uma
      torneira de recurso, e das boas: recompensa quem joga de um jeito específico, não quem joga
      muito
      - **Ficha em disco**, como todo o resto: `AchievementDefinition` + catálogo. Conquista nova
        é um `.asset` com condição e prêmio
      - **A conquista do jogo é a dona da verdade, a da Play é o espelho** — desenho decidido em
        21/08/2026. Completou no jogo → dispara a da Play. Isso tem uma consequência boa de
        sequência: **esta parte não espera a Fase 7.** As conquistas nascem funcionando e pagando
        offline, e o espelho da Play é um gancho que se liga depois, num lugar só
      - Ao criar cada conquista, já anotar o **ID correspondente na Play** para o dia em que o
        `GPGSIds.cs` for regerado. Sem isso, casar dezenas de conquistas depois vira trabalho de
        conferência manual
- [ ] **Persistência** — hoje tudo mora em `PlayerPrefs` (`Services/LevelProgress.cs`, recordes).
      Economia é outra coisa: mais dados, mais estruturado. Decidir aqui o formato **sabendo que
      ele vai viajar para a nuvem** (Fase 7) — um bloco serializável só, com versão, e não vinte
      chaves soltas de `PlayerPrefs`. Migrar formato depois de haver jogador com saldo é o tipo de
      dor que se evita agora, de graça
- [ ] **Equilíbrio da economia é o que o teste fechado vai responder** *(posição do Raffael em
      21/08/2026)*. O teste interno acha feature ruim; ritmo de grind e "vale a pena a segunda
      nave?" só aparecem com gente que não sabe onde estão os números. Esta parte nasce **sabendo
      que vai ser recalibrada** — então nada de número mágico espalhado por código: tudo em ficha
- [ ] **Gatilho de documentação:** economia com compra real **muda declarações da Play**. Segue a
      regra da Fase 5 — atualiza no dia em que entrar, e não antes

### Parte 8 — UI refeita ⬜
*Aberta em 21/08/2026. **No fim do bloco, e é de propósito:** cada parte acima cria tela nova
(loja, oficina, recompensa, seleção de nave). Refazer a UI antes é refazê-la duas vezes.*

Enquanto as Partes 5 a 7 correm, **tela feia não é bug** — só falta de função conta. As telas
nascem funcionais e sem acabamento; esta parte é a passada única que unifica tudo.

- [ ] **Refazer a tela de seleção de fase inteira** *(decidido pelo Raffael em 07/08/2026)* —
      layout, dropdown de dificuldade e o resto. A de hoje é montada em runtime a partir do
      `LevelCatalog` e serve para testar a progressão
- [ ] Telas novas que as Partes 5 a 7 criarem — loja, oficina/evolução, seleção de nave,
      recompensas, nível do jogador
- [ ] HUD revisto, agora que há poderes ativos e recursos para mostrar durante a corrida
- [ ] **Navegação inteira** — é aqui que se decide como se anda entre menu, loja, oficina e
      corrida sem o jogador se perder. Hoje são duas cenas e uma transição de pódio
- [ ] Testar no aparelho — fecha o MVP e abre a fase de arte, áudio e acabamento

### Backlog solto da Fase 6
*Coisas que não pertencem a nenhuma parte e não bloqueiam nada.*

- [ ] Decidir se o tiro vira comando do jogador, com munição, ou continua automático.
      *O ganho passivo já deixa o jogo de pé sem tiro nenhum, então isto é escolha, não conserto*
- [ ] Pool de objetos para tiro e obstáculo, se o `Instantiate`/`Destroy` pesar no aparelho.
      **Vira prioridade se a Parte 5 encher a tela de itens caindo**
- [ ] Áudio e arte de verdade — **saíram daqui**: são a fase seguinte ao MVP, junto com o
      acabamento. Ver "Ordem recomendada"

## Fase 7 — Conta do jogador, login e conquistas ⬜
Depende do projeto GPGS novo (Fase 5). **O plugin não está mais no projeto** — foi removido na
limpeza de 02/08/2026 junto com a configuração da conta antiga. Esta fase começa por instalar
o plugin atual, e não por consertar o velho.

**A conta do jogador é o Google Play, e o salvamento é na nuvem por padrão** *(decidido pelo
Raffael em 21/08/2026)*. Nada de conta própria, nada de senha: entra com a conta Google que já
está no aparelho, e o progresso — nível, moedas, naves, evolução, conquistas — sobe para a nuvem
sem o jogador pedir. Trocar de celular não pode custar o saldo.

**É o gatilho de documentação mais pesado do projeto:** mexe na política de privacidade e na
Segurança dos dados de uma vez só, porque passa a haver identificador de usuário **e dado de
jogador guardado fora do aparelho**. Vale a regra da Fase 5 — atualiza no dia em que a conta
entrar, não antes.

**A ordem em relação à economia importa:** a Parte 7 da Fase 6 decide o formato da persistência
já sabendo que ele vai viajar, e cria as conquistas do jogo já anotando o ID da Play de cada uma.
Esta fase é a que **liga o fio**, não a que inventa o sistema.

- [ ] **Instalar o plugin do Play Games atual** (o removido era 0.11.01, anterior à Unity 6.3 —
      pegar a versão que declare suporte a Unity 6). Conferir logo depois se o External
      Dependency Manager resolve as dependências Android sem erro; se falhar de novo, o log
      verboso do Console diz se é rede, JDK/Gradle ou incompatibilidade de versão
- [ ] **Salvamento em nuvem** (*Saved Games* do Play Games) — o bloco de progresso da Parte 7 sobe
      e desce sozinho. **Decidir a regra de conflito antes de codar:** dois aparelhos, dois saldos,
      quem ganha? O de maior progresso, o mais recente, ou pergunta ao jogador? É a decisão que
      dói se for tomada depois de existir jogador com saldo
      - **O jogo tem de funcionar sem nuvem.** Sem internet, sem login, sem Play Services: joga
        igual, salva local, sincroniza quando der. Login que trava a entrada é o jeito mais rápido
        de perder jogador na primeira tela
- [ ] **Espelhar as conquistas do jogo na Play** *(desenho do Raffael em 21/08/2026)* — cada
      conquista criada na Parte 7 ganha a gêmea no Console, e **completar no jogo dispara a da
      Play**. A do jogo é a dona da verdade e paga o prêmio; a da Play é vitrine e facilita
      acompanhar quem completou o quê
      - Recriar as conquistas no novo projeto GPGS e regerar `GPGSIds.cs`
        *(as antigas se chamavam "Conquista?" e "Mais uma tentativa?" — não sobreviveram e nem
        deviam: nasceram como teste de pipeline, não como conquista de jogo)*
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
- [ ] Ligar o `UnlockAchievement` no ponto único onde a conquista do jogo se completa — **um
      lugar só**, e não espalhado pelo código. É o que torna o espelho da Play barato
- [ ] Cadastrar a SHA-1 de debug no Console para conseguir testar login sem build de release

## Fase 8 — Arte, áudio e acabamento ⬜
**Começa quando o MVP fechar** (Parte 8 da Fase 6 aprovada no aparelho), e não antes. O motivo é o
mesmo que adiou a UI: arte feita para uma tela que ainda vai mudar é arte feita duas vezes.

- [ ] **Arte de verdade** no lugar dos marcadores de `Assets/Art/Placeholder/` — nave, obstáculos,
      poderes, fundo, UI
- [ ] **Áudio** — motor, tiro, impacto, poder pego, vitória, derrota. Hoje **não há um único
      arquivo de som no projeto**
- [ ] Ícone próprio (hoje usa o padrão do Unity)
- [ ] Splash própria (hoje "Made with Unity")
- [ ] **Arte da loja de verdade** — ícone, gráfico de destaque e screenshots caprichadas, no lugar
      das provisórias. **A `StoreArt.cs` sai do projeto neste dia** (ver Fase 5)
- [ ] Leaderboard do Play Games (opcional)

## Fase 9 — Monetização ⬜
Depois do lançamento. Anúncios antes de compras.

- [ ] Anúncios (integração)
- [ ] Compras no app (design de economia — projeto próprio, do tamanho do jogo base)

---

## Ordem recomendada

*Revisto em 21/08/2026, com o plano de lançamento que o Raffael desenhou no mesmo dia.*

**Fases 0 a 4 fechadas.** Da Fase 6, as **Partes 1 a 4 estão aprovadas no aparelho** — a mecânica
crua acabou. O que resta do jogo são as Partes 5 a 8, e depois arte, áudio e monetização.

### O plano de lançamento, em etapas

Escrito pelo Raffael em 21/08/2026. A regra que atravessa todas as etapas: **cada uma só começa
quando a anterior estiver tão boa e tão testada quanto a mecânica crua está hoje.**

| # | Etapa | O que fecha |
|---|---|---|
| 1 | **Mecânica crua** | ✅ **Feita.** Partes 1 a 4, aprovadas no aparelho em 21/08 |
| 2 | **Meta-jogo** | Partes 5 a 8: poderes, naves, recursos, conquistas, UI. **Fecha o MVP** |
| 3 | **Análise de dados** | Entre o teste interno e o fechado. Fase 5 |
| 4 | **Arte, áudio e acabamento** | Fase 8, com a Fase 7 (conta Google + nuvem) no caminho |
| 5 | **Monetização** | Fase 9 |
| 6 | **Teste fechado** | Os 16 e-mails, 12 firmes por 14 dias. **A leitura verdadeira do equilíbrio** |
| 7 | **Roadmap de futuro** | Conteúdo e eventos planejados para ~1 ano, antes de abrir |
| 8 | **Lançamento aberto** | Com divulgação, e o jogo inteiro de pé |

**Próximo passo, nesta ordem:**

1. **Converter o `ShipStats` em ficha de disco** — pequeno, é só código, e destrava a Parte 6.
2. **Parte 5 — poderes em partida.** Ciclo curto, julgado no polegar.
3. **Parte 6 — naves, diferenciação e evolução.**
4. **Parte 7 — recursos, nível e recompensas.**
5. **Parte 8 — UI refeita.** Fecha o MVP.

> **O teste fechado segue adiado de propósito, e agora com prazo definido: depois de tudo pronto.**
> Ele exige **12 testadores em opt-in contínuo por 14 dias** e destrava **produção**. Pedir a 16
> pessoas que instalem e mantenham instalado por duas semanas é uma ficha social que se joga uma
> vez. Teste interno **não conta** para esses 14 dias — e é justamente por isso que ele pode
> continuar rodando com gente de verdade o tempo todo, de graça, como já está rodando.

### As duas apostas que sustentam este plano

**1. Fábrica, não estoque.** O Raffael quer um ano de conteúdo previsto antes de abrir o jogo.
Isso não exige um ano de conteúdo *construído* — exige que cada item novo custe horas. Fase nova
já é um `.asset`; poder, nave e recompensa têm de nascer iguais. **É a diferença entre planejar
doze features e planejar doze linhas numa tabela.**

**2. Documentação por etapa.** Nada se declara na Play antes de existir. Cada feature que mexe em
declaração atualiza a sua no dia em que entra — o mapa de quem mexe em quê está em
`docs/play-console/declaracoes-do-app.md`, seção *"Gatilhos de revisão"*. Ver a "Regra de
documentação" na Fase 5.

> O modelo híbrido de faixas segue adiado de propósito (ver Parte 1) — mas deixou de ser puramente
> teórico: a página do app foi escrita **sem citar número de faixas**, justamente porque o Raffael
> pretende mecânicas que aumentem e reduzam a quantidade delas.

**Caminho crítico real: o jogo.** A publicação deixou de ser problema — o app está no ar, o
processo de atualizar está escrito e leva minutos, e não há pressa de lançar. O que decide o
projeto agora é o que se joga.
