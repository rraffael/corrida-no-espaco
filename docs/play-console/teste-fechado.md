# Teste fechado — 12 testadores

Este é **o relógio mais lento do projeto**. Comece a montar a lista mesmo com o jogo incompleto:
o prazo só começa a correr depois que o teste está no ar.

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
`applicationId` definitivo (Fases 3 e 4). Mas **juntar os 16 e-mails não depende de nada** — é
o que dá para adiantar hoje, e é justamente o que trava o cronograma se ficar para depois.
