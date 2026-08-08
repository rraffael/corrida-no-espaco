# Declarações do app no Play Console

As dez declarações de *Política e programas → Conteúdo do app*, com a resposta de hoje e **o que
faria cada uma mudar**. Preenchido em 08/08/2026, para o app em teste interno.

**Este arquivo tem duas funções.** A primeira é servir de gabarito na hora de preencher. A segunda,
e a mais importante a longo prazo: **antes de desenvolver qualquer feature nova, passar os olhos na
seção "Gatilhos de revisão"** no fim. Mecânica nova costuma mexer em declaração, e declaração
errada é motivo de rejeição — a Play compara o que você declarou com o que o binário faz.

> Reúne o que antes estava espalhado em `data-safety.md` e `iarc-e-publico-alvo.md`, apagados em
> 08/08/2026. Três arquivos para conferir era o oposto do que se quer aqui.

> As regras da Play mudam com frequência. Este arquivo é memória do que foi respondido e por quê —
> **não é fonte da verdade sobre as regras atuais.** Reconfirme no Console.

---

## As dez declarações

| # | Declaração | Resposta (08/08/2026) |
|---|---|---|
| 1 | Política de Privacidade | URL pública da política — ver `politica-de-privacidade.md` |
| 2 | Anúncios | **Não contém anúncios** |
| 3 | Detalhes do login | **Sem restrição de acesso** — o app não tem login, tudo é jogável de cara |
| 4 | Classificações de conteúdo | Questionário IARC, abaixo. Categoria **Jogos** |
| 5 | Público-alvo e conteúdo | **13-15, 16-17, 18+**. Atrai crianças: **Não** |
| 6 | Segurança dos dados | **Não coleta nem compartilha nenhum dado** |
| 7 | ID de publicidade | **Não usa** |
| 8 | Apps governamentais | **Não** |
| 9 | Recursos financeiros | **Não** |
| 10 | Apps de saúde | **Nenhum recurso de saúde** |

**A 7 foi verificada no bundle, não chutada.** O `.aab` de 08/08/2026 declara **uma** permissão:
`android.permission.INTERNET`. Nenhum `AD_ID`. O `INTERNET` a Unity põe por padrão em todo build
Android e não conflita com "não coleta dados" — permissão declarada não é dado coletado.

Para reconferir depois de um build novo, descompacte o `.aab` (é um zip) e olhe as permissões em
`base/manifest/AndroidManifest.xml`.

---

## Questionário IARC — perguntas e respostas

Todas **Não**, em 08/08/2026. As perguntas estão como o Console as apresenta, porque o texto exato
é o que importa na hora de decidir se uma feature nova muda a resposta.

| Pergunta | Resposta | Por que hoje é essa |
|---|---|---|
| **Violência, sangue ou imagens violentas** — inferências, referências ou representações, incluindo violência contra o personagem do jogador | **Não** | A nave bate em detrito e explode. Sem sangue, sem alvo humano, sem representação de violência |
| **Medo** — sons ou imagens assustadoras, aterradoras ou perturbadoras | **Não** | Corrida espacial com arte colorida e marcador de lugar |
| **Sexualidade, insinuação ou jogos de namoro** | **Não** | Não há personagens |
| **Jogos de azar** — jogos de azar reais, simulação de casino/bingo ou referências | **Não** | Nada de aposta, sorteio pago ou economia |
| **Idioma** — linguagem potencialmente ofensiva | **Não** | Os textos são de menu e HUD |
| **Substâncias controladas** — drogas, álcool, tabaco | **Não** | |
| **Humor ofensivo** — funções corporais com fim humorístico | **Não** | |
| **Compras digitais, recompensas convertíveis em dinheiro ou NFTs** | **Não** | Sem compras, sem moeda, sem ativo transferível |
| **Diversos — interação entre utilizadores** (voz, texto, partilha de imagem ou áudio) | **Não** | O placar é local, no `PlayerPrefs`. Ninguém vê o recorde de ninguém |
| **Diversos — partilha da localização precisa com outros utilizadores** | **Não** | O app não lê localização |
| **Diversos — suástica ou símbolos nazis** | **Não** | |
| **Diversos — denegrir a identidade nacional da Coreia do Sul** | **Não** | |
| **Diversos — defesa de atos terroristas** | **Não** | |
| **Diversos — descrições realistas de crimes ou técnicas criminosas** | **Não** | |

**Resultado esperado:** livre para todas as idades (L / 3+ / Everyone).

O questionário pede um e-mail de contato. A classificação sai automática e é aplicada sozinha.

---

## Público-alvo: por que 13+, e o que custa mudar

| Opção | O que implica |
|---|---|
| **13 anos ou mais** *(o que foi declarado)* | Fica **fora** do programa Famílias. Menos formulários, sem exigência extra de conteúdo, sem revisão adicional |
| Incluir menores de 13 | Entra na **Política para Famílias**: política de privacidade específica, SDKs restritos, anúncios certificados para crianças, revisão mais rigorosa. Limita a monetização da Fase 9 |

A classificação IARC dá "livre", mas isso **não** obriga a declarar público infantil — são coisas
separadas, e a segunda é escolha do desenvolvedor.

**Cuidado registrado:** a Play olha a arte e o texto da página do app para contestar essa
declaração. Se a arte de verdade ficar infantilizada, a declaração de 13+ pode ser questionada.

---

## Segurança dos dados: por que "não coleta nada"

O `.aab` de hoje **não tem SDK de terceiro nenhum**:

- **Play Games removido** em 02/08/2026, volta na Fase 7
- **Unity Analytics desligado** em 02/08/2026 — `UnityConnectSettings.asset` com tudo em `0` e o
  módulo fora do `manifest.json`
- **Sem anúncios**, sem rede, sem servidor

O nome que o jogador digita no recorde fica no **`PlayerPrefs`, dentro do aparelho**. Guardar
localmente não é coleta — coleta é o dado sair do dispositivo.

> Regra de ouro: o formulário e a política de privacidade têm de contar **a mesma história**.
> Divergência entre os dois é motivo comum de rejeição, e quem compara é automação, não um humano
> de boa vontade.

---

## Gatilhos de revisão

**Consultar esta seção antes de começar qualquer feature da lista.** A pergunta não é "isso é
grave?", é "isso muda alguma resposta lá em cima?".

| Feature | O que muda | Onde está no ROADMAP |
|---|---|---|
| **Anúncios** | Declaração 2 vira **Sim** ("Contém anúncios" aparece na loja). Declaração 7 (ID de publicidade) vira **Sim**. IARC: a pergunta de anúncios muda. **Segurança dos dados muda por inteiro** — passa a haver ID de publicidade, compartilhamento com terceiro e finalidade de publicidade | Fase 9 |
| **Compras no app** | IARC: **"Compras digitais"** vira **Sim**. Declaração 9 (recursos financeiros) merece releitura | Fase 9 |
| **Gacha / caixa surpresa** | Certamente IARC **"Compras digitais"** → Sim, e a Play exige **divulgar as probabilidades**. Se vira **"jogos de azar"** é julgamento: mecânica de sorteio pago sem saque em dinheiro normalmente **não** entra como azar, mas se for apresentada com estética de cassino, roleta ou aposta, entra. **Responder de novo com honestidade na época, não copiar daqui** | Fase 9 |
| **Play Games (login, conquistas)** | **A política de privacidade também muda** — a seção sobre Play Games foi *removida* dela em 08/08/2026, porque o plugin não está no app; ela volta junto. E a **Segurança dos dados** passa a declarar IDs de jogador: *coletado, não compartilhado, opcional, finalidade "funcionalidade do app"*, justificativa *"usado apenas para identificar o jogador no Google Play Games Services e registrar conquistas; o desenvolvedor não recebe nem armazena esses dados"*. Se o login virar obrigatório para alguma parte, a declaração 3 muda também | Fase 7 |
| **Leaderboard online** | **É a mudança mais fácil de deixar passar.** Hoje o nome no recorde é local; num placar online ele **sai do aparelho** e passa a ser dado coletado — e, se outros jogadores veem, **compartilhado**. Declaração 6 deixa de ser "não coleta" | Fase 7/8 |
| **Chat, comentários ou qualquer troca entre jogadores** | IARC **"interação entre utilizadores"** vira Sim. Costuma elevar a classificação etária | Não planejado |
| **Arte de verdade** | Se entrar sangue, corpo humano atingido ou explosão realista, IARC **violência** merece releitura. Se o clima ficar sombrio ou com susto, **medo** também. Também vale reler a declaração de **público-alvo**: arte infantilizada é o que a Play usa para contestar o 13+ | Fase 8 |
| **Áudio** | Mesma lógica do medo: som de tensão ou susto pode mudar a resposta | Fase 6, "Depois disso" |
| **Qualquer SDK novo** | **Segurança dos dados** cobre o que os SDKs de terceiros coletam, não só o seu código. SDK novo = reler a declaração 6 inteira | Sempre |

**A regra que resume tudo:** o formulário descreve **o binário que está no ar**, não o que se
pretende construir. Declarar cedo demais é tão errado quanto declarar tarde.

---

## Registro

| Quando | O quê |
|---|---|
| 08/08/2026 | Primeira vez preenchido, para o teste interno. Todas as dez, com o `.aab` `0.1.0-1` |
