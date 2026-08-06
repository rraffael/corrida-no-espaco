# Corrida no Espaço — instruções do projeto

**Perfil desta pasta: Raffael (Projetos Pessoais).** Usar direto, sem perguntar no início da sessão.

## Antes de qualquer coisa

Ler `ROADMAP.md` na raiz — é a fonte de verdade do plano e do estado atual.
`docs/ESCOPO.html` é de 28/07/2026, anterior ao rebuild, e está desatualizado na parte técnica;
só a análise de publicação e política da Play continua boa.

## O que é

Jogo mobile Android feito na Unity `6000.3.20f1`. Divisão de trabalho:

- **Claude escreve o código** — C#, dados, editor tooling, scripts de build, documentos.
- **Raffael abre a Unity** — roda, testa no aparelho, faz arte e áudio, e **publica**. Upload na
  Play Store é sempre dele: ação externa irreversível e envolve segredos de assinatura.

## Regras de edição

- **Nunca editar cena (`.unity`) ou prefab na mão** para mudanças estruturais. Escrever um editor
  script em `Assets/Editor/Tools/` com item de menu e pedir para ele clicar. Exceção tolerável:
  trocar um valor escalar já existente, com a Unity fechada.
- **Nunca editar `.unity` ou `ProjectSettings/` na mão — o Editor vive aberto** e sobrescreve o
  arquivo. Mudança nesses arquivos é sempre por editor script.
- **Ao mover ou apagar arquivo, levar o `.meta` junto.** Sem isso os GUIDs quebram.
- **Nome de arquivo de MonoBehaviour tem que bater com o nome da classe**, senão não dá para
  anexar em GameObject nenhum. Já aconteceu neste projeto (`Google-Login.cs`).
- **Ferramenta nova de editor entra no catálogo.** O caminho do `[MenuItem]` é uma `const` em
  `Assets/Editor/Tools/ProjectTools.cs`, a ferramenta entra em `ProjectTools.All`, e a montagem
  chama `ProjectTools.MarkRun(id)` ao terminar (e `Forget(id)` no "Desmontar"). É o que faz o
  **Painel de ferramentas** saber sozinho o que já rodou. Montagem vai no submenu `Montagem/`;
  conserto pontual, em `Correções/`.
- **Toda mudança que precise de montagem entra no `Montar.cs`.** O Raffael roda **um item só**:
  **Tools → Corrida no Espaço → Montar**. Ao terminar uma mudança, reescrever a lista `Steps` em
  `Assets/Editor/Tools/Montar.cs` com o que aquela mudança exige remontar, na ordem
  assets → `Game.unity` → `Menu.unity`. Lista vazia é resposta válida: quer dizer que foi só
  código. Nunca pedir para ele rodar os itens do submenu `Montagem/` um a um.
- **Não checar se a Unity está aberta, e não rodar validação em batchmode.** O Editor dele fica
  aberto o tempo todo; o batchmode trava no lockfile e a checagem só gasta tempo. Escrever o
  código, dizer o que ele roda no Editor, e parar aí — o Console dele acusa erro de compilação.
- **Conserto pontual se apaga depois de rodar.** Script de `Correções/` que arruma uma coisa uma
  vez sai do projeto (com o `.meta`) depois de funcionar, e o ROADMAP registra o que ele fez e o
  `git checkout <commit> -- <caminho>` que o traz de volta. Consequência prática: **commitar antes
  de rodar**, senão não há de onde resgatar.
  *(Não vale mais para as montagens: o `Montar.cs` chama `LevelSetup`, `RaceSetup`, `BattleSetup`,
  `MenuSetup` e `LevelSelectSetup` direto, então apagar qualquer uma quebra a compilação.)*

## Estrutura

```
Assets/Scripts/<área>/     código de runtime, sem namespace (Input/, Services/, VFX/)
Assets/Editor/Tools/       ferramentas de editor e build
Assets/Scenes/             Menu.unity, Game.unity
docs/                      escopo, política de privacidade, textos do Play Console
tools/                     scripts de máquina (logcat)
```

Estilo: identificadores em inglês, comentários e menus em português. Comentário explica **por
quê**, não o que a linha já diz.

## Comandos

- Montar: menu **Tools → Corrida no Espaço → Montar** — o único item que ele roda. Ver as regras
  de edição acima.
- Ferramentas: menu **Tools → Corrida no Espaço → Painel de ferramentas** — lista o que falta
  rodar e o que já rodou, com a data. O diário fica em `UserSettings/`, fora do git.
- Build: menu **Tools → Corrida no Espaço → Build**, ou `-executeMethod BuildAndroid.Release`
  com as variáveis `CNE_KEYSTORE_*` definidas.
- Log do aparelho: `.\tools\logcat.ps1`

## Não fazer

- Rodar `git`, `npm` ou `npx` por conta própria — sugerir o comando para ele rodar.
- Ler ou escrever fora desta pasta sem pedir autorização.
- Commitar keystore, senha ou qualquer segredo. O keystore mora fora do repositório e chega no
  build por variável de ambiente.
