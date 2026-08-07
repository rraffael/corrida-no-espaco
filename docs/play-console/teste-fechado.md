# Teste fechado — 12 testadores

> **Adiado de propósito em 07/08/2026.** O alvo de agora é o **teste interno**, que põe o app na
> Play sem exigir os 12 testadores — ver `caminho-ate-a-play-store.md`. Este documento vale para
> quando o jogo estiver bom de lançar; até lá, **não junte os e-mails**. Pedir a 16 pessoas que
> instalem e mantenham instalado por 14 dias é uma ficha social que se joga uma vez, e gastá-la
> numa versão que ainda vai mudar muito desperdiça a única chance de ter os 12 firmes no dia.

Este é **o relógio mais lento do projeto**. Quando a hora chegar, ele é o caminho crítico: o prazo
só começa a correr depois que o teste está no ar.

---

## A regra, em resumo

Contas de desenvolvedor **pessoais** criadas a partir de novembro de 2023 precisam, antes de
liberar produção, rodar um teste fechado com:

- no mínimo **12 testadores** que aceitaram o convite (opt-in);
- pelo menos **14 dias contínuos** de teste, com esses 12 ativos o período todo;
- e só então solicitar acesso à produção.

**Os dois detalhes que costumam custar semanas:**

1. **Contínuo.** Se o número cair de 12 no meio do caminho, o contador reinicia. Convide mais do
   que 12 — a recomendação prática é **16 a 20**, contando com quem some.
2. **Opt-in de verdade.** Não basta a pessoa dizer que testa: ela precisa abrir o link do teste,
   aceitar participar e instalar pela Play com a **mesma conta Google** da lista.

> Regra da Play, sujeita a mudança. Confirme os números no Console antes de contar prazo.

---

## Lista de testadores

Preencher com o **e-mail da conta Google** de cada pessoa — o mesmo com que ela usa a Play Store
no celular. E-mail alternativo não funciona.

| # | Nome | E-mail da conta Google | Convidado | Aceitou | Instalou |
|---|---|---|---|---|---|
| 1 |  |  | ⬜ | ⬜ | ⬜ |
| 2 |  |  | ⬜ | ⬜ | ⬜ |
| 3 |  |  | ⬜ | ⬜ | ⬜ |
| 4 |  |  | ⬜ | ⬜ | ⬜ |
| 5 |  |  | ⬜ | ⬜ | ⬜ |
| 6 |  |  | ⬜ | ⬜ | ⬜ |
| 7 |  |  | ⬜ | ⬜ | ⬜ |
| 8 |  |  | ⬜ | ⬜ | ⬜ |
| 9 |  |  | ⬜ | ⬜ | ⬜ |
| 10 |  |  | ⬜ | ⬜ | ⬜ |
| 11 |  |  | ⬜ | ⬜ | ⬜ |
| 12 |  |  | ⬜ | ⬜ | ⬜ |
| 13 |  |  | ⬜ | ⬜ | ⬜ |
| 14 |  |  | ⬜ | ⬜ | ⬜ |
| 15 |  |  | ⬜ | ⬜ | ⬜ |
| 16 |  |  | ⬜ | ⬜ | ⬜ |

**Onde procurar 16 pessoas:** família e amigos com Android, colegas de trabalho, grupo do
condomínio, comunidade de dev (troca de teste é comum: você testa o jogo de alguém, ele testa o
seu). Não precisa ser gente que goste do jogo — precisa ser gente que instale e deixe instalado.

## Mensagem de convite

```
Oi! Tô publicando um joguinho meu na Play Store e preciso de 12 pessoas testando por 14 dias
pra Google liberar a publicação. É rápido:

1. Me manda o e-mail da conta Google que você usa na Play Store do celular
2. Eu te mando um link
3. Você abre o link, clica em aceitar e instala o jogo
4. Só não desinstala nos próximos 14 dias 🙏

Não precisa jogar todo dia — se jogar e me falar o que achou, melhor ainda. É Android só.
```

## Checklist de execução

- [ ] Juntar 16 e-mails de conta Google
- [ ] Criar o grupo de testadores no Console (*Testes → Testes fechados*)
- [ ] Subir um `.aab` assinado na trilha de teste fechado (Fase 4)
- [ ] Enviar o link de opt-in e confirmar quem aceitou
- [ ] Conferir a contagem de participantes no Console — só o Console vale, não a sua lista
- [ ] Marcar no calendário a data em que os 14 dias fecham
- [ ] Ao fim, solicitar acesso à produção

## Ordem de dependência

O teste fechado precisa de um `.aab` assinado, que precisa do keystore novo e do
`applicationId` definitivo (Fases 3 e 4) — tudo isso fica pronto na Etapa 1 do
`caminho-ate-a-play-store.md`, então quando esta fase começar só faltará gente.

**Juntar os 16 e-mails não depende de nada tecnicamente** — e mesmo assim está adiado de
propósito. É o único item do projeto que se atrasa *de graça*: convidar cedo demais gasta a
disposição das pessoas numa versão que ainda vai mudar, e não adianta um dia sequer, porque o
contador só corre com o `.aab` no ar. Ver o aviso no topo.
