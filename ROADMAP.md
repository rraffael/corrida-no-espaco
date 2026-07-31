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

*Levantado em 31/07/2026, lendo o projeto.*

**Funcional**
- Cena `Menu.unity` com fiação correta: `Menu.cs` no Canvas, BotaoJogar → `OnPlayButton`,
  BotaoSair → `OnQuitButton`, GraphicRaycaster e EventSystem presentes, câmera com tag `MainCamera`.
- Ambas as cenas na lista de build (`Menu`, `Game`).
- Build Android instalando e abrindo no aparelho.
- Conta Unity nova, licença PE Personal ativa. Projeto **não** tem vínculo com organização
  Unity (`cloudProjectId` e `organizationId` vazios) — a conta antiga excluída não afeta nada.
- Camada de toque nova em `Assets/Scripts/Input/` — escrita, ainda **não validada no aparelho**.

**Stubs / incompleto**
- `Google-Login.cs` — exemplo cru copiado da documentação. Classe ainda chamada
  `GooglePlayGamesExampleScript`, `Token` público, autentica no `Awake()` sem UI e sem
  tratar falha. **Não está em nenhuma cena**, então hoje nem executa.
- `GPGSIds.cs` — 2 conquistas geradas, mas nenhuma linha do jogo chama `ReportProgress`.
  As conquistas existem no Console e são inalcançáveis.
- CanvasScaler do menu em *Constant Pixel Size* — a Reference Resolution 800x600
  configurada logo abaixo é ignorada nesse modo.

**Faltando**
- **`Game.unity` está vazia** — 206 linhas, só uma Main Camera. Sem nave, sem cenário, sem lógica.
- Gameplay inteiro (passo 6 do escopo original, nunca feito).
- Áudio: nenhum `.wav`/`.mp3`/`.ogg` no projeto.
- Ícone e splash próprios.
- Keystore de upload (o antigo está perdido; `ProjectSettings.asset:277` ainda aponta para
  `C:/Users/raffa/Desktop/Projeto/key/user.keystore`, que não existe).

---

## Fase 0 — Diagnóstico 🟡
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
- [ ] **Rodar o item de menu acima e rebuildar** — o log tem de ficar limpo e os botões do menu
      têm de responder ao toque
- [ ] Confirmar se o Console do Unity acusa erro de compilação nos scripts novos

## Fase 1 — Tornar o jogo testável no celular 🟡
São três ajustes pequenos que hoje fazem o app *parecer* quebrado sem estar.
*Aplicados em 31/07/2026 por edição direta dos arquivos, com a Unity fechada. Falta abrir a
Unity para confirmar que os valores apareceram no Inspector e rodar no aparelho.*

- [x] `Menu.unity` — CanvasScaler: *UI Scale Mode* → **Scale With Screen Size**,
      Reference Resolution **1080x1920**, Match **0.5**
      *(estava em Constant Pixel Size: botões de 300x65 px físicos ficavam minúsculos num 1080x2400)*
- [x] Travar orientação em **Portrait** — `defaultScreenOrientation: 0`, e as três autorrotações
      não-retrato desligadas
- [x] `androidRenderOutsideSafeArea` → `0` — o jogo não tem código lendo `Screen.safeArea`,
      então é melhor o Android reservar a faixa do notch. Reverter para `1` no dia em que
      a UI tratar a safe area sozinha
- [ ] `AndroidTargetSdkVersion` explícito — tentei **36** e o build quebrou: a SDK Platform 36
      não está instalada nesta máquina. Voltou para `0` (Automatic) para destravar o teste.
      **Pendência da Fase 4:** instalar a Platform 36 pelo Android SDK Manager e fixar o valor —
      a partir de 31/08/2026 a Play exige target 36 para aceitar upload
- [ ] Confirmar no aparelho: abrir a Unity, Build And Run, ver menu legível e sem rotação

## Fase 2 — Input de toque 🟡
Base já escrita nesta sessão; falta provar no aparelho e ligar na jogabilidade.

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
- [ ] Decidir o gesto de jogo: *tap* nas laterais vs *swipe* para trocar de faixa
- [x] ~~Decidir se o EventSystem migra para `InputSystemUIInputModule`~~ — decidido pela
      realidade: **não era opcional**. A suposição de que "hoje o legado funciona" estava errada;
      ver Fase 0. Migração escrita, falta rodar

## Fase 3 — Decisões que travam o resto ⬜
Herdadas da Parte 4 do escopo. Nenhuma linha de gameplay ou build de release deve sair antes destas duas.

- [ ] **Qual é "o jogo"?** O conceito do documento (foguete trocando de faixa, velocidade como
      barra de vida) ou polir um shoot-'em-up? O template que servia de base **não existe mais no
      repositório**, então "polir o shooter" hoje significaria recomeçá-lo também
- [ ] **Novo `applicationId`.** O atual `com.Raffael.corridanoespaco` provavelmente está queimado
      na conta antiga. Não pode mudar depois de publicado. Sugestão do escopo:
      `br.com.raffael.corridanoespaco`

## Fase 4 — Destravar o build de release ⬜
Depende da Fase 3. Fecha o assunto "publicar" de uma vez.

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
- [ ] Aplicar o novo `applicationId`
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
- [ ] Desligar o Unity Analytics legado antes de publicar — `UnityConnectSettings.asset:23` está
      com `m_Enabled: 1` e `m_InitializeOnStartup: 1`. Sem `cloudProjectId` ele não envia nada,
      mas a política afirma que o jogo não coleta dados: melhor a configuração concordar com o texto
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

## Fase 6 — O jogo em si ⬜
O grosso do trabalho. Só começa com a Fase 3 decidida.

- [ ] Montar a `Game.unity` do zero — hoje só tem a Main Camera
- [ ] Foguete trocando entre 3 faixas
- [ ] Obstáculos que aceleram ou freiam (velocidade = única barra de vida)
- [ ] Derrota com velocidade ≤ 0; vitória por dobra espacial com velocidade ≥ X
- [ ] Ligar o gameplay na camada `TouchInput`
- [ ] Voltar ao menu ao morrer (`SceneManager.LoadScene`)
- [ ] Pontuação persistente
- [ ] Áudio: motor, impacto positivo, impacto negativo, vitória, derrota
- [ ] Reaproveitar do template só a **arte** (naves, planetas, VFX), não o gameplay

## Fase 7 — Fechar login e conquistas ⬜
Depende do projeto GPGS novo (Fase 5).

- [ ] Recriar as conquistas no novo projeto GPGS e regerar `GPGSIds.cs`
- [x] Reescrever o login — `Assets/Scripts/Services/PlayGamesAuth.cs` substitui o antigo
      `Google-Login.cs`, que foi apagado. Login silencioso opcional, login manual em botão,
      falha vira warning e não quebra o jogo, token de servidor vai por callback e não fica
      guardado. Traz também `UnlockAchievement`/`ReportAchievement`
      *(o arquivo velho se chamava `Google-Login.cs` com a classe `GooglePlayGamesExampleScript`:
      com hífen no nome e classe diferente do arquivo, a Unity nunca deixaria anexar num
      GameObject — era código morto por construção)*
- [ ] Colocar o `PlayGamesAuth` numa cena (raiz da `Menu.unity`) — só depois do projeto GPGS novo
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

**Agora, em paralelo:**
- Fase 0 (logcat) — destrava confiar nos testes
- Fase 5, primeiro item (12 testadores) — é o item mais longo e nem começou a correr
- Fase 3 (as duas decisões) — não custam trabalho, mas travam tudo abaixo

**Na sequência:** Fase 1 → Fase 2 (validar no aparelho). Juntas fazem o jogo virar algo
testável de verdade no celular, e são baratas.

**Depois:** Fase 4 (marco do `.aab` assinado). A partir daí publicar deixa de ser mistério.

**Então:** Fase 6, sem distração. É onde o projeto deixa de ser exercício de pipeline e vira jogo.

**Por último:** Fases 7, 8 e 9.

**Caminho crítico real:** Fase 3 → Fase 6. Todo o resto ou é barato, ou roda em paralelo,
ou não depende de você.
