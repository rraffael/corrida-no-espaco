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
- **Verificar se a Unity está fechada** antes de mexer em `.unity` ou `ProjectSettings/`:
  `Get-Process Unity -ErrorAction SilentlyContinue`. Com o Editor aberto, ele sobrescreve.
- **Ao mover ou apagar arquivo, levar o `.meta` junto.** Sem isso os GUIDs quebram.
- **Nome de arquivo de MonoBehaviour tem que bater com o nome da classe**, senão não dá para
  anexar em GameObject nenhum. Já aconteceu neste projeto (`Google-Login.cs`).

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

- Validar compilação: `Unity.exe -quit -batchmode -projectPath . -executeMethod ...`
  *(pedir o caminho do `Unity.exe` — fica fora da pasta do projeto)*
- Build: menu **Tools → Corrida no Espaço → Build**, ou `-executeMethod BuildAndroid.Release`
  com as variáveis `CNE_KEYSTORE_*` definidas.
- Log do aparelho: `.\tools\logcat.ps1`

## Não fazer

- Rodar `git`, `npm` ou `npx` por conta própria — sugerir o comando para ele rodar.
- Ler ou escrever fora desta pasta sem pedir autorização.
- Commitar keystore, senha ou qualquer segredo. O keystore mora fora do repositório e chega no
  build por variável de ambiente.
