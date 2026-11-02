# Design das naves

Decisões de design do Raffael sobre naves, progressão e monetização. **Registradas em 01/11/2026 e
ainda não implementadas.** O estado de cada tarefa fica no `ROADMAP.md`; as regras que já valem no
jogo, no `docs/ESCOPO.html`, que só muda quando a decisão entrar no jogo.

## 1. Sem ranks visíveis

- O jogador vê só a lista de naves, sem rank nem tier.
- Lançamento com **7 a 8 naves**, priorizando **diversidade de estilo de jogo** em vez de força.
- Ranks visíveis podem virar uma atualização futura, quando houver 15 a 20 naves.

*Substitui a parte "o jogador vê o rank" da decisão de 26/09/2026 sobre os ranks S → E.*

## 2. Power score interno (regra obrigatória)

- Cada nave tem um **power score**, calculado dos atributos base e das métricas dos testes automatizados.
- **Serve para:**
  - **Balanceamento:** nave acima da curva leva nerf ou fica mais difícil de obter; abaixo, leva buff ou
    vira nave inicial.
  - **Aquisição:** define o custo ou a dificuldade de obter a nave.
- **Os testes avaliam cada nave isoladamente**, contra um conjunto fixo de cenários de referência, para o
  custo crescer de forma linear com o número de naves. **Alterou uma nave, roda só os testes dela.**
- Se um dia houver ranks visíveis, eles serão **faixas de power score**. Mudar as faixas não exige rodar
  testes de novo.

## 3. Progressão por nave com escolha de poder

- Cada nave evolui e, em certo ponto, o jogador **escolhe entre dois poderes alternativos**.
- **As diferenças devem ser sutis**, porque cada partida dura uns 30 segundos.
- **Exemplo:** a nave cuja IA joga sozinha (Raffa) tem hoje a ativação automática ao entrar na dobra, se
  houver carga. Ela precisa de um poder alternativo.
- **Trocar de poder depois da escolha custa um recurso do jogo**, obtenível jogando ou pagando.

## 4. Monetização (princípio; o detalhe vem depois)

- **Nada de pay-to-win.** Tudo que é mecânica (naves, upgrades e poderes) deve ser obtenível jogando.
- **Pagar só acelera** (pay-to-progress).

*Substitui a ideia de 01/11/2026 de "dar grande vantagem a quem pagar". A Predadora pode continuar num
pacote, inclusive já no nível 40, desde que também possa ser obtida jogando.*

## Decidido em 02/11/2026

- **A escolha de poder fica no nível 60**, entre duas passivas.
- **O recurso da troca é a Sucata**, até a economia da Parte 8.
- **Os atributos seguem iguais até ler a base.** O ajuste dos valores de base vem junto do power score e
  dos ranks internos, por estarem ligados.
- **A Raffa é a primeira a ganhar alternativa**, para testar a ideia antes das outras naves.

### Poder alternativo da Raffa: AI-ssistant (AI-ssistente em português)

Depois que a Super IA termina, a **primeira batida seguinte é evitada**: a nave desvia sozinha, como se a
IA tivesse deixado um assistente a bordo. A outra opção do nível 60 é a Dobra assistida.

1. **Sem prazo:** fica armado até a próxima batida.
2. **Nunca falha:** desvia se houver faixa livre; se não houver, atravessa, sem dano e sem perder velocidade.
3. **Vale para estilhaço também.**
4. **Não acumula:** no máximo um por vez.
5. **Cancela o estilhaço do Casulo** quando evita a batida nele.

### Regras de fallback, decididas em 02/11

- **Recarga de poder:** não nasce para nave sem poder ativo. O sorteio não muda; aquela vez fica vazia, e os
  outros modificadores mantêm a mesma taxa. A regra fica no próprio modificador ("sirvo para esta nave?").
- **Intangibilidade na dobra:** durante a carga, a perda de velocidade não derruba abaixo da velocidade de dobra.
- **Toque duplo sem carga ou em recarga:** treme só o quadradinho do poder, nunca a tela inteira.
- **Frenesi com "Desabilitar Armas":** fica como está; perder a sequência é um revés intencional.
- **Rastro:** fica como está; puxar para uma batida é a função dele.

## Plano em etapas

*Aprovado pelo Raffael em 02/11, depois da consulta aos agentes.*

1. Referência por nave no simulador, com cenários fixos de teste.
2. Power score **provisório**, que ainda não vira preço nem nerf.
3. **Testar bases diferentes para as naves.**
4. Estrutura da escolha de poder.
5. Tela de escolha e troca.
6. Poderes alternativos, medidos, começando pela Raffa. **Depois desta etapa, rodada com os testadores.**
7. **Reforma das fases.**
8. Power score **final**, que diz quanto cada nave vale em relação às outras.
9. Recurso (Parte 8): quanto se ganha por partida, e os preços em Sucata.
10. Monetização (Fase 9), no lugar previsto no plano de lançamento.

**Sem testar em duplicidade:** cada rodada seguinte, de testadores ou de simulador, cobre só o que mudou em
relação à base. A base completa só roda de novo quando a mudança atingir todas as naves.

**Ponto de checagem, ao ler a base:** as fases vêm antes da etapa 3 se as Fases 2 e 3 não separarem as
naves nos níveis 15 a 40, se a ordem das naves mudar muito de fase para fase, ou se o Difícil nível 60
também saturar, porque aí uma passiva sutil não apareceria.

**Antes de a moeda valer algo:** fases e larguras fechadas; progresso guardado por fase e dificuldade, para
fase nova não pagar de novo o que já foi vencido; retorno decrescente ou pagamento pela primeira vitória;
persona parada medida; conquistas de teste removidas; placar por nave decidido.

## Pendências de decisão

1. **Quais outras naves ganham alternativa**, e quais são as 2 ou 3 naves que faltam.
2. **A fórmula do power score**, depois de ler a base de 6.000 corridas.

## Limites conhecidos

- **"Roda só os testes dela" vale quando só a nave muda.** Se mudar fase, obstáculo, modificador, persona
  ou o simulador, as referências de todas as naves ficam desatualizadas.
- **As personas não foram calibradas com gente.** O score compara naves entre si; não diz quanto um jogador
  de verdade vence. Ele deve ser recalibrado com os dados do teste fechado.
- **Há ruído:** diferença pequena de score não deve virar nerf, nem preço diferente.
