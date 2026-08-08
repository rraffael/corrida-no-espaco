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

- [x] ~~Criar o app~~ — feito em 08/08/2026. Nome "Corrida no Espaço", **idioma padrão en-US**,
      Jogo, Gratuito, pacote `br.com.raffael.corridanoespaco`
      - ⚠️ **Lembrete do idioma padrão:** ficou en-US, então **pt-BR precisa ser adicionado como
        idioma extra**, senão brasileiro vê a página em inglês. Não trava o teste interno, mas não
        pode ser esquecido no lançamento
- [x] ~~Ligar o Play App Signing~~ — **não existe o que ligar.** Para app criado novo é
      obrigatório e automático: entra em vigor sozinho quando o primeiro `.aab` sobe
- [x] ~~Publicar a política de privacidade~~ — no ar em
      `https://rraffael.github.io/Portfolio/corrida-no-espaco/privacidade/`
- [x] ~~Preencher a seção de conteúdo do app~~ — **as dez declarações fechadas em 08/08/2026**,
      sem pendência. Gabarito em `declaracoes-do-app.md`
- [x] ~~Preencher a página do app~~ — textos reescritos e arte provisória gerada em 08/08/2026.
      Tudo em `pagina-do-app.md`
- [x] ~~Subir o `.aab`, adicionar-se como testador e instalar~~ — **feito em 08/08/2026.**

> ## ✅ Etapa 1 concluída em 08/08/2026
>
> **O jogo está na Play Store, em teste interno, instalado pelo celular do Raffael.** Era o
> objetivo: parar de instalar por cabo. Daqui para a frente, versão nova chega pela loja.

### Como subir uma atualização

O caminho inteiro, toda vez. São seis passos e nenhum deles é o Play App Signing ou os
formulários — aquilo tudo já está feito e não se repete.

1. **Fazer e testar a mudança** no Editor, como sempre.
2. **Subir o `AndroidBundleVersionCode` em 1.** *Project Settings → Player → Other Settings.*
   **A Play recusa dois envios com o mesmo código** — é o erro mais comum aqui. O `bundleVersion`
   (`0.1.0`) é o número que o usuário vê, e sobe quando você achar que a mudança merece.
3. **Tools → Corrida no Espaço → Conferir configuração.** Ele agora barra pacote errado, cena
   fora de ordem, orientação e keystore vazado. **Rodar sempre** — foi pular isto que deixou o
   primeiro `.aab` sair com o pacote errado.
4. **Tools → Corrida no Espaço → Build → AAB de release.** Se reclamar de variável de ambiente,
   feche a Unity **e o Unity Hub**, e abra de novo: o Editor herda o ambiente do Hub, e o Hub só
   lê na inicialização.
5. **Play Console → Testes → Teste interno → Criar nova versão**, subir o `.aab` de `Builds/`,
   escrever as notas da versão e lançar.
6. **O celular atualiza sozinho pela Play**, em minutos. Sem análise demorada — aquilo foi só na
   primeira vez.

**O que NÃO se repete:** declarações de conteúdo, política de privacidade, arte da página,
categoria, lista de testadores. Só mudam se você quiser mudar.

> O Console mostra uma lista do que ainda falta preencher antes de deixar publicar. **Ele é a
> autoridade, não este arquivo** — as exigências mudam com frequência.

---

## Etapa 2 — Teste fechado (quando for lançar)

Não fazer agora. Quando a hora chegar, o passo a passo, a regra dos 12/14 dias, o texto de convite
e a tabela de acompanhamento estão em **`teste-fechado.md`**.

### Ligar os símbolos de depuração antes de haver testadores

O Console avisa, a cada envio, que faltam **dois arquivos** e que os relatórios de falha ficam
ilegíveis sem eles:

| Aviso | O que é | Onde se liga |
|---|---|---|
| Arquivo de desofuscação | O `mapping.txt` do R8/ProGuard, que devolve os nomes reais das classes. **Só existe se o *Minify* estiver ligado** | *Player Settings → Publishing Settings → Minify* |
| Símbolos nativos | O jogo roda em IL2CPP, então quase tudo é biblioteca nativa. Sem símbolos, um crash chega como endereço de memória | *Player Settings → Publishing Settings*, a opção que gera o `symbols.zip` |

**Ignorados de propósito no teste interno** (08/08/2026): o único aparelho é o do Raffael, e para
ele existe `.\tools\logcat.ps1`, que dá a pilha completa em tempo real — mais do que o Console
mostraria.

**Viram obrigatórios quando houver testadores**, porque aí alguém diz "fechou sozinho" e não há
como pedir o celular emprestado. O `symbols.zip` sai junto do build e sobe ao lado do `.aab`.

### O que ainda vai faltar, e que a Etapa 1 não resolve

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
