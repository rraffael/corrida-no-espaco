# Caminho até a Play Store

Escrito em 07/08/2026, quando a conta de desenvolvedor saiu da verificação.

**São duas viagens, e é importante não confundir:**

| | Etapa 1 — **Teste interno** | Etapa 2 — **Teste fechado** |
|---|---|---|
| Para quê | ter o app na Play para **você** testar, baixando e atualizando pela loja | destravar o acesso à produção |
| Quando | **agora** | quando o jogo estiver bom de lançar |
| Testadores | até 100, por e-mail — pode ser só você | **12 em opt-in contínuo por 14 dias** |
| Atualização no ar | minutos | passa por análise, bem mais lento |
| Conta para a regra dos 12/14 | **não** | é ela que conta |

**Decisão de 07/08/2026:** fazer só a Etapa 1 por enquanto. O objetivo não é lançar — é parar de
instalar por cabo e passar a receber atualização pela Play como qualquer app.

> **Por que não adiantar a Etapa 2:** pedir a 16 pessoas que instalem e **mantenham instalado por
> 14 dias** é uma ficha social que se joga uma vez. Gastá-la numa versão que ainda vai mudar
> muito desperdiça a única chance de ter os 12 firmes quando o lançamento for de verdade. E o
> contador só serve para destravar produção, que não é o objetivo agora.

**O que a Etapa 1 deixa pronto para a Etapa 2:** keystore, Play App Signing, app criado,
`applicationId` registrado e os formulários de conteúdo. Nada disso se refaz depois.

---

## ⚠️ Decisão irreversível, antes de tudo

Depois que o app for publicado em **qualquer** trilha, o `applicationId` não muda mais. Trocar
depois significa app novo, do zero, sem histórico.

O atual é **`br.com.raffael.corridanoespaco`**, decidido em 02/08/2026. Se houver qualquer dúvida
sobre ele, é agora — depois de criar o app, acabou.

---

## Etapa 1 — Teste interno

### 1.1 Na sua máquina

- [x] ~~Instalar a Android SDK Platform 36~~ — **já estava instalada.** Conferido em 07/08/2026
      pelo menu **Tools → Corrida no Espaço → Conferir SDK do Android**: a `6000.3.20f1` traz
      **34, 35 e 36**. O build que quebrou em 31/07 tinha outra causa, e o medo de mexer no target
      era infundado. *(Rode esse item de novo a cada atualização da Unity — o conjunto de
      Platforms muda com a versão do Editor.)*
- [ ] **Gerar o keystore novo.** O antigo se perdeu; este assina tudo daqui para a frente,
      inclusive o lançamento.

      **Pela Unity, sem terminal** *(caminho recomendado)*: *Project Settings → Player →
      Publishing Settings → Keystore Manager → Keystore… → Create New → Anywhere…*. Escolha a
      pasta, a senha, e crie um alias chamado `upload`.
      **Depois de criar, desmarque *Custom Keystore*** — senão o caminho do arquivo fica gravado
      no `ProjectSettings.asset` e vaza para o git. (O `BuildAndroid` limpa isso no próximo build
      de release, mas não conte com isso: desmarque na hora.)

      **Pelo terminal**, se preferir — o `keytool` vem com o JDK da Unity, em
      `Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin`:
      ```
      keytool -genkeypair -v -keystore corrida-no-espaco.keystore -alias upload \
        -keyalg RSA -keysize 2048 -validity 10000
      ```

      Valendo para os dois caminhos:
      - **Fora do repositório**, numa pasta que não seja a do projeto
      - **Senha e alias num gerenciador de senhas**, não em arquivo de texto
      - **Uma cópia em outro lugar** (nuvem, pendrive). Perder este custa um reset de chave no
        Console — foi o que aconteceu com o anterior
- [x] ~~Três ajustes no `ProjectSettings`~~ — feitos em 07/08/2026 e conferidos no disco:
      **Target API Level 36**, nome no celular **"Corrida no Espaço"** (era `corrida-no-espaco`) e
      versão **`0.1.0`** (era `1`)
- [ ] **O `AndroidBundleVersionCode` sobe a cada upload** — a Play recusa dois envios com o mesmo
      código. Fica em 1 para o primeiro. Esse é campo de todo build, não de conserto: mexa nele em
      *Project Settings → Player → Other Settings*, na própria janela da Unity

### 1.2 O primeiro `.aab`

- [ ] **Definir as quatro variáveis de ambiente e só então abrir a Unity** — o Editor lê o
      ambiente na inicialização, então definir com ela aberta não pega. **Feche a Unity antes.**

      Sem terminal: tecla Windows → *"variáveis de ambiente"* → *Editar as variáveis de ambiente
      da sua conta*. Em *Variáveis de usuário*, **Novo…** quatro vezes:

      | Nome | Valor |
      |---|---|
      | `CNE_KEYSTORE_PATH` | caminho completo do `.keystore` |
      | `CNE_KEYSTORE_PASS` | senha do keystore |
      | `CNE_KEY_ALIAS` | `upload` |
      | `CNE_KEY_ALIAS_PASS` | senha do alias |

      Assim elas persistem e você não redefine a cada build. **O preço honesto:** senha em
      variável de usuário é legível por qualquer programa rodando na sua conta. Para um projeto
      pessoal nesta máquina é uma troca razoável; se incomodar, defina só na sessão do terminal
      antes de abrir a Unity, e aí some ao fechar
- [ ] **Tools → Corrida no Espaço → Conferir configuração**
- [ ] **Tools → Corrida no Espaço → Build → AAB de release.** Sai em `Builds/`, que o
      `.gitignore` já ignora
- [ ] Conferir que o `ProjectSettings.asset` continua **sem** o caminho do keystore depois do
      build. O `BuildAndroid` limpa sozinho, mas confere uma vez

> **Etapas 1.1 e 1.2 fechadas em 08/08/2026.** O `.aab` assinado está em
> `Builds/Corrida no Espaço-0.1.0-1-release.aab`. Daqui para a frente é tudo Play Console.

### 1.3 No Play Console

- [ ] **Criar o app.** Nome, idioma padrão pt-BR, *App*, *Gratuito*
- [ ] **Ligar o Play App Signing** — é o que permite resetar a chave de upload se ela sumir de
      novo. Foi exatamente o problema que custou caro antes
- [ ] **Publicar a política de privacidade numa URL pública** e colar o link. Texto pronto em
      `docs/politica-de-privacidade.md` (e `privacy-policy.md`); falta hospedar — GitHub Pages do
      portfólio resolve
- [ ] **Preencher a seção de conteúdo do app** — são **dez declarações**, e as respostas estão
      todas em `declaracoes-do-app.md`, junto com o questionário IARC pergunta por pergunta.
      Nove são rápidas; a única que trava é a política de privacidade, acima
- [ ] **Preencher o mínimo da página do app.** Para teste interno só você vê, então **texto e arte
      marcador de lugar bastam** — a reescrita de verdade fica para o lançamento.
      Ver `pagina-do-app.md`, que hoje está desatualizado de propósito
- [ ] **Subir o `.aab` em *Testes → Teste interno*** e adicionar seu e-mail à lista
- [ ] Abrir o link de opt-in no celular, aceitar e instalar pela Play

> O Console mostra uma lista do que ainda falta preencher antes de deixar publicar. **Ele é a
> autoridade, não este arquivo** — as exigências mudam com frequência.

### Daqui para a frente

Cada versão nova é: subir o `AndroidBundleVersionCode`, gerar o `.aab`, subir no teste interno.
A atualização chega no seu celular pela Play em minutos.

---

## Etapa 2 — Teste fechado (quando for lançar)

Não fazer agora. Quando a hora chegar, o passo a passo, a regra dos 12/14 dias, o texto de convite
e a tabela de acompanhamento estão em **`teste-fechado.md`**.

O que ainda vai faltar naquele momento, e que a Etapa 1 não resolve:

- Os **16 e-mails** de conta Google (dezesseis, não doze: se a contagem cair de 12, o contador
  reinicia)
- A **página do app reescrita** — o texto atual promete um jogo que não existe
- **Arte de verdade:** ícone 512×512, gráfico de destaque 1024×500, screenshots
- Ícone e splash próprios no app (Fase 8)

---

## Armadilhas

- **Definir as variáveis com a Unity já aberta não funciona.** Feche, defina, abra.
- **`bundleVersionCode` repetido é recusado.** Suba um a cada upload, sempre.
- **O `applicationId` é para sempre.** Ver o aviso no topo.
- **Regras da Play mudam.** Reconfirme tudo no Console; nada aqui é fato eterno.
