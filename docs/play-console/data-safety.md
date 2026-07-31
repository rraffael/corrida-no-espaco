# Formulário de Segurança dos Dados (Data Safety)

Respostas propostas, coerentes com `docs/politica-de-privacidade.md`. Preencher em
*Política → Conteúdo do app → Segurança dos dados*.

> Regra de ouro: o formulário e a política precisam contar **a mesma história**. Divergência
> entre os dois é motivo comum de rejeição — e é a Play que compara, não um humano de boa vontade.

---

## Seção 1 — Coleta e compartilhamento

| Pergunta | Resposta | Por quê |
|---|---|---|
| Seu app coleta ou compartilha algum dos tipos de dados obrigatórios? | **Não** | O jogo não tem servidor. Nada sai do aparelho por iniciativa do jogo. |
| Todos os dados são criptografados em trânsito? | **Não se aplica** | Sem transmissão pelo app. Se a pergunta aparecer obrigatória, responder **Sim** — o que o Play Games transmite vai por HTTPS. |
| Você fornece uma forma de o usuário pedir exclusão dos dados? | **Sim** | Desinstalar ou limpar dados do app apaga tudo o que é local; os dados de conta são excluídos pelo Google. |

**A pegadinha:** o Data Safety cobre também o que os **SDKs de terceiros** coletam, não só o seu
código. Aqui o SDK relevante é o Google Play Games Services.

## Seção 2 — Google Play Games Services

O login é **opcional** e feito pelo próprio Google. A leitura conservadora — e a que combina com
a política — é declarar o que o SDK trata quando o jogador entra:

| Tipo de dado | Coletado | Compartilhado | Obrigatório? | Finalidade |
|---|---|---|---|---|
| IDs do usuário (player ID, gamertag) | Sim | Não | **Opcional** | Funcionalidade do app (conquistas) |

Justificativa a registrar: *"Usado apenas para identificar o jogador no Google Play Games
Services e registrar conquistas. O desenvolvedor não recebe nem armazena esses dados."*

- [ ] Confirmar no Console se o guia de Data Safety do Play Games ainda pede essa declaração —
      o Google já mudou a orientação sobre serviços próprios dele mais de uma vez

## Seção 3 — O que **não** declarar

Não marcar nenhum destes, porque o jogo genuinamente não faz:

- Localização, contatos, agenda, SMS, fotos, arquivos, áudio
- Informações pessoais (nome, e-mail, documento)
- Informações financeiras
- Histórico de navegação ou de busca
- Identificadores de publicidade — **enquanto não houver anúncios**

## Seção 4 — Pendências antes de enviar

- [ ] Publicar a política de privacidade e colar a URL no formulário
- [ ] Desligar o Unity Analytics legado (`UnityConnectSettings.asset:23`) — hoje `m_Enabled: 1`.
      Sem `cloudProjectId` ele não envia nada, mas é melhor a configuração concordar com a
      declaração "não coleta"
- [ ] Conferir se algum plugin novo entrou no projeto desde a última revisão
- [ ] **Revisar tudo isto de novo na Fase 9** — no dia em que entrar anúncio, este formulário
      muda por inteiro: passa a haver ID de publicidade, compartilhamento com terceiro e
      finalidade de publicidade
