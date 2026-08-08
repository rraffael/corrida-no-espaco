# Página do app na Play Store — Corrida no Espaço

O que o usuário vê na página do app: nome, descrições, ícone e screenshots. Pronto para colar no
Play Console (*Crescer → Presença na loja → Detalhes do app*).

> *O Console chama isto de "ficha da loja" (**store listing**). O arquivo se chamava
> `ficha-da-loja.md` e foi renomeado em 07/08/2026, porque "ficha" não dizia nada.*

**Reescrito em 08/08/2026**, para o jogo que existe. A versão anterior era de 02/08 e descrevia um
conceito abandonado: prometia que não havia barra de vida, que um toque muda de faixa, que existiam
impulsos para coletar, e não mencionava tiro.

> **Regra de redação, pedida pelo Raffael em 08/08/2026: não citar o número de faixas.** Ele
> pretende mecânicas que aumentem e reduzam a quantidade de faixas no meio da fase (ver Fase 6,
> Parte 1, o modelo híbrido). Texto de loja que promete "três faixas" vira mentira no dia em que
> uma fase abrir a quarta — e alterar a página depois não desfaz a expectativa de quem já instalou.
>
> **Pelo mesmo motivo, o número de fases também saiu.** Diz "fases numeradas", não "três fases":
> a progressão foi desenhada para aceitar fase nova sem invalidar o que o jogador já tem, então
> travar o número no texto seria criar trabalho para depois. Só ficou o número das dificuldades,
> que não está previsto mudar.

---

## Nome do app (máx. 30 caracteres)

**Corrida no Espaço** — nos dois idiomas. *(17 caracteres.)*

A Play permite traduzir o nome, mas a recomendação é **não traduzir**. "Space Race" é genérico
demais: é termo histórico, existe em vários produtos, e o jogo afundaria na busca. "Corrida no
Espaço" é distintivo, já é o nome do app no Console e o nome que aparece no celular. Nome é marca —
vale mais ser achável do que compreensível.

---

## Descrição curta (máx. 80 caracteres)

É o texto que aparece na busca, e **o que mais converte**. A regra: começar por **verbo**, dizer o
que a pessoa *faz*. Não gastar os 80 caracteres com "este é um jogo de…".

**pt-BR** *(67 caracteres)*
```
Desvie dos detritos, acelere e segure a dobra. Até onde você chega?
```

**en-US** *(69 caracteres)*
```
Dodge debris, push your speed and hold the warp. How far can you get?
```

---

## Descrição completa (máx. 4000 caracteres)

### pt-BR

```
Uma nave. Um corredor cheio de coisa no caminho. E uma decisão, tomada muitas vezes: para onde
desviar, e quando.

Arraste para mover a nave, desvie do que vem, e empurre a velocidade até abrir a dobra — depois
segure ela por tempo suficiente para atravessar.

A nave atira sozinha. Destruir um obstáculo empurra a velocidade para cima, então o tiro é o
caminho rápido — mas nunca é o único. Dá para vencer só desviando. Mais devagar, mas dá.

Bater custa caro. A nave despenca para um quarto da velocidade, fica um instante sem reagir e
depois volta se arrastando no trecho final. Você sente o tranco.

O QUE VEM PELA FRENTE
• Detrito — o perigo básico, desde o primeiro segundo
• Barcaça — larga o bastante para fechar quase toda a passagem
• Casulo — destrua um e ele explode em estilhaços que descem em cima de você

DOIS JEITOS DE JOGAR
• Fases numeradas, cada uma apresentando um perigo novo. Vença uma para abrir a próxima, em três
  dificuldades
• Sem fim — sem dobra, sem linha de chegada. Acelera até te derrubar, e a distância entra na
  tabela de recordes

FEITO PARA O CELULAR
• Retrato, uma mão só, um gesto
• Partidas curtas
• Funciona offline
• Sem anúncios e sem compras dentro do app
```

### en-US

```
One ship. A corridor full of things in the way. And one decision, made over and over: where to
dodge, and when.

Swipe to move, dodge what comes at you, and push your speed high enough to open a warp — then
hold it long enough to get through.

Your ship fires on its own. Destroying an obstacle pushes your speed up, so shooting is the fast
route — but it is never the only one. You can get through by dodging alone. Slower, but it works.

Crashing costs you. The ship drops to a quarter of its speed, sits dead for a moment, then claws
its way back, and the last stretch is the slow one. You feel it.

WHAT YOU FACE
• Debris — the basic hazard, from the first second
• Barges — wide enough to seal off most of the way through
• Cocoons — destroy one and it bursts into shrapnel raining straight down on you

TWO WAYS TO PLAY
• Numbered stages, each introducing a new hazard. Beat one to unlock the next, across three
  difficulties
• Endless — no warp, no finish line. It speeds up until it kills you, and your distance goes on
  the board

BUILT FOR A PHONE
• Portrait, one hand, one gesture
• Short runs
• Works offline
• No ads, no in-app purchases
```

> ⚠️ **Uma frase depende de teste:** *"dá para vencer só desviando"* / *"you can get through by
> dodging alone"*. É regra de projeto (ver ROADMAP, Bloco B) e vale por desenho, mas **não foi
> verificada nas três fases no aparelho**. Se o teste mostrar que alguma fase não fecha sem tiro,
> **tirar a frase antes de publicar** — promessa de loja que não se cumpre vira avaliação de uma
> estrela.

---

## Recursos gráficos

| Item | Requisito | Obrigatório? | Estado |
|---|---|---|---|
| Ícone do app | PNG/JPEG, **512×512**, até 1 MB, sem transparência | Sim | ✅ `arte/icone-512.png` |
| Gráfico de destaque | PNG/JPEG, **1024×500**, até 15 MB | Sim | ✅ `arte/destaque-1024x500.png` |
| Screenshots de celular | **2 a 8**, PNG/JPEG, até 8 MB cada, proporção 16:9 ou 9:16, lados entre 320 e 3840 px | **Sim** | ⬜ do aparelho |
| Screenshots de tablet 7" | até 8, mesmas regras | **Não** | ⬜ pular |
| Screenshots de tablet 10" | até 8, lados entre 1080 e 7680 px | **Não** | ⬜ pular |
| Vídeo promocional | link do YouTube | Não | ⬜ |

**Ícone e gráfico de destaque saem do menu Tools → Corrida no Espaço → Gerar arte da loja**
(`Assets/Editor/Tools/StoreArt.cs`). Vão para `docs/play-console/arte/`, e **não** para `Assets/` —
lá dentro a Unity as importaria como textura e elas entrariam no `.aab`, engordando o build com
imagem que o jogo nunca desenha. São opacas e no tamanho exato, que é o que a Play cobra.
São marcador de lugar: **a ferramenta sai do projeto no dia da arte de verdade.**

**Pule os dois tamanhos de tablet.** São opcionais, o jogo é retrato de uma mão, e tablet não é o
público. A única consequência é a Play poder marcar o app como "não otimizado para tablets" — o que
é verdade, e não impede nada.

**Screenshots:** o jogo é retrato, então 1080×1920 sai direto do aparelho e já está na proporção
certa. As **duas primeiras** são as que aparecem na busca — use a corrida em movimento, com
obstáculo na tela e o HUD visível. Screenshot de menu é desperdício de vitrine.

---

## Notas da versão

O Console pede um texto por idioma a cada envio, entre etiquetas. Em teste interno ninguém lê,
mas o campo é obrigatório.

```
<pt-BR>
Primeira versão em teste. Corrida, tiro automático, três tipos de obstáculo, fases com
progressão e o modo sem fim com tabela de recordes.
</pt-BR>
<en-US>
First test build. The run, automatic fire, three hazard types, stage progression and the
endless mode with its score table.
</en-US>
```

**Daqui para a frente**, cada envio precisa de nota nova — e o `AndroidBundleVersionCode` tem de
subir junto, senão a Play recusa o arquivo.

---

## Dados de contato e classificação

- [x] **Categoria: Jogos → Arcade** *(decidido em 08/08/2026)*. Alternativa aceitável: Casual.
      **Corrida foi descartada apesar do nome do jogo:** ali estão Asphalt e Real Racing, e quem
      entra na categoria quer *dirigir* — pista, adversário, curva. Aqui a mecânica é desviar em
      três faixas com placar, que é o que define Arcade. Ação também não: o tiro é automático e
      acessório. **Categoria se troca depois quantas vezes quiser**, ao contrário do nome do pacote
- [ ] Tags (até 5), em ordem de utilidade — a lista da Play é fechada, então pegar as mais
      próximas: **corrida sem fim** (*endless runner*), **arcade**, **nave/shoot 'em up**,
      **ficção científica**, **casual**.
      *Critério: tag boa descreve mecânica que alguém digitaria na busca, não tema. "Espaço" rende
      menos que "corrida sem fim", porque ninguém procura jogo por cenário*
- [x] E-mail de contato: **`suporte.raffael@gmail.com`** — **visível publicamente** na página
- [x] Site: `https://rraffael.github.io/Portfolio/`
- [x] **Telefone: deixar em branco.** É opcional e fica público. Telefone pessoal exposto numa loja
      é convite para spam, e ninguém liga para o suporte de um jogo mobile
- [x] URL da política de privacidade:
      `https://rraffael.github.io/Portfolio/corrida-no-espaco/privacidade/`
- [x] **Marketing externo: ativo.** É promoção gratuita, fica inerte enquanto o app não for
      público, e a mudança demora **até 60 dias** para valer — então o estado que vale no
      lançamento tem de estar configurado bem antes
- [ ] **Idiomas: en-US é o padrão** (escolhido ao criar o app) — **falta adicionar pt-BR** como
      idioma extra, senão brasileiro vê a página em inglês

> A Play muda regra de página e limite de caracteres com frequência. Confirme cada limite no
> Console na hora de colar; não trate esta tabela como fato.
