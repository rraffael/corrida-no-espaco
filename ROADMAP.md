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

## Fase 0 — Diagnóstico pendente 🟡
Bloqueia confiar em qualquer teste no aparelho. Sem saber o erro, não dá para separar bug real de ruído.

- [ ] Capturar o erro que fica repetindo no aparelho:
      `adb logcat -s Unity` (adb em `…/PlaybackEngines/AndroidPlayer/SDK/platform-tools/`)
- [ ] Identificar a origem e decidir se é código, configuração ou ruído de plugin
- [ ] Confirmar se o Console do Unity acusa erro de compilação nos scripts novos de `Assets/Scripts/Input/`

## Fase 1 — Tornar o jogo testável no celular ⬜
São três ajustes pequenos que hoje fazem o app *parecer* quebrado sem estar.

- [ ] `Menu.unity` — CanvasScaler: *UI Scale Mode* → **Scale With Screen Size**,
      Reference Resolution **1080x1920**, Match **0.5**
      *(hoje em Constant Pixel Size: botões de 300x65 px físicos ficam minúsculos num 1080x2400)*
- [ ] Travar orientação em **Portrait** — hoje `defaultScreenOrientation: 4` (auto-rotate, 4 orientações liberadas)
- [ ] Avaliar `androidRenderOutsideSafeArea: 1` — UI pode ficar sob o notch
- [ ] Fixar `AndroidTargetSdkVersion` explicitamente — hoje `0` (Automatic), não determinístico

## Fase 2 — Input de toque 🟡
Base já escrita nesta sessão; falta provar no aparelho e ligar na jogabilidade.

- [x] Escrever a camada de leitura de toque — `Assets/Scripts/Input/TouchInput.cs`
      *(Input System novo, com fallback de mouse no Editor; roda com o `activeInputHandler: 2`
      atual, sem precisar mexer em Player Settings)*
- [x] Escrever o validador visual — `Assets/Scripts/Input/TouchTester.cs`
- [ ] **Validar no aparelho:** GameObject vazio na `Game.unity` → Add Component `TouchTester`
      → Build And Run → conferir dedos ativos, posição, delta e taps
- [ ] Decidir o gesto de jogo: *tap* nas laterais vs *swipe* para trocar de faixa
- [ ] Decidir se o EventSystem do menu migra de `StandaloneInputModule` (legado) para
      `InputSystemUIInputModule` — **recomendação: só depois que o gameplay estiver de pé**,
      hoje o legado funciona e mexer é risco sem ganho

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

- [ ] Gerar keystore novo, guardar **fora** do repositório, senha e alias num gerenciador
- [ ] Tirar o caminho absoluto de `ProjectSettings.asset:277` — não amarrar o projeto a uma máquina
- [ ] Aplicar o novo `applicationId`
- [ ] Ativar Play App Signing (padrão) — permite reset se a chave de upload sumir de novo
- [ ] **Marco:** gerar um `.aab` de release assinado, mesmo com o jogo incompleto
- [ ] Adicionar `/.utmp/` ao `.gitignore`

## Fase 5 — Play Console, em paralelo ⬜
Não depende de código. **O relógio mais lento do projeto** — começar cedo.

- [ ] Montar a lista de **12 testadores** para o teste fechado (opt-in contínuo por 14 dias)
- [ ] Escrever e publicar a política de privacidade com URL pública (GitHub Pages do portfólio)
- [ ] Preparar ficha da loja: ícone 512×512, feature graphic, screenshots, descrições PT e EN
- [ ] Quando a conta sair da verificação: criar o app e testar se o pacote antigo é aceito
- [ ] Criar o projeto novo no Play Games Services
- [ ] Formulário de Data Safety, coerente com a política de privacidade
- [ ] Questionário IARC e declaração de público-alvo

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
- [ ] Reescrever `Google-Login.cs`: nome de classe decente, sem campo público de token,
      tratamento de falha, sem autenticar cego no `Awake()`
- [ ] Colocar o script numa cena — hoje ele não está em nenhuma
- [ ] Chamar `ReportProgress` de fato no jogo (passo 4.4 do escopo, nunca fechado)
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
