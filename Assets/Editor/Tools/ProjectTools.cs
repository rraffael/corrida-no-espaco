using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Catálogo das ferramentas do projeto e diário de quando cada uma rodou.
///
/// Os caminhos de menu moram aqui, e não espalhados pelos atributos
/// <c>[MenuItem]</c>, para renomear um item ser mexer num lugar só. As
/// ferramentas de montagem avisam aqui quando terminam, então o
/// <see cref="ProjectToolsWindow"/> sabe o que já está feito sem ninguém
/// precisar anotar nada à mão.
/// </summary>
[InitializeOnLoad]
static class ProjectTools
{
    public const string MenuRoot = "Tools/Corrida no Espaço/";

    public const string PanelItem = MenuRoot + "Painel de ferramentas";

    /// <summary>
    /// O item único de montagem. Fica na raiz do menu, e não em <c>Montagem/</c>,
    /// porque é o que o Raffael roda — os itens de dentro do submenu existem só
    /// para remontar uma coisa isolada quando faz falta.
    /// </summary>
    public const string SetupAllItem = MenuRoot + "Montar";

    public const string PreflightItem = MenuRoot + "Conferir configuração";
    public const string AndroidSdkItem = MenuRoot + "Conferir SDK do Android";
    public const string StoreArtItem = MenuRoot + "Gerar arte da loja";
    public const string BuildApkItem = MenuRoot + "Build/APK de teste";
    public const string BuildAabItem = MenuRoot + "Build/AAB de release";

    public const string RaceItem = MenuRoot + "Montagem/Montar corrida (fundo + HUD)";
    public const string RaceUndoItem = MenuRoot + "Montagem/Desmontar corrida";
    public const string LevelsItem = MenuRoot + "Montagem/Criar fases e fichas de obstáculo";
    public const string ShipsItem = MenuRoot + "Montagem/Criar ficha da nave";
    public const string BattleItem = MenuRoot + "Montagem/Montar combate (nave, obstáculos, fim)";
    public const string BattleUndoItem = MenuRoot + "Montagem/Desmontar combate";
    public const string MenuSceneItem = MenuRoot + "Montagem/Montar menu (recordes + transição)";
    public const string MenuSceneUndoItem = MenuRoot + "Montagem/Desmontar menu";
    public const string LevelSelectItem = MenuRoot + "Montagem/Montar seleção de fase";
    public const string LevelSelectUndoItem = MenuRoot + "Montagem/Desmontar seleção de fase";

    public const string RaceId = "corrida-fundo-hud";
    public const string LevelsId = "fases-e-obstaculos";
    public const string ShipsId = "ficha-da-nave";
    public const string BattleId = "combate";
    public const string MenuSceneId = "menu-recordes";
    public const string LevelSelectId = "selecao-de-fase";

    /// <summary>
    /// O diário vive em <c>UserSettings/</c>, que o .gitignore já ignora: é
    /// estado de máquina ("eu rodei isto aqui"), não fato do projeto — para
    /// isso existe o ROADMAP.md. Assim ele também não suja commit nenhum.
    /// </summary>
    const string LogPath = "UserSettings/corrida-ferramentas.json";

    public sealed class Tool
    {
        /// <summary>Chave no diário. Vazio: ferramenta de rotina, que não tem "já feito".</summary>
        public string Id;

        public string Title;
        public string Summary;
        public string MenuPath;

        /// <summary>Item que desfaz esta montagem, quando existe.</summary>
        public string UndoMenuPath;

        public bool Tracked => !string.IsNullOrEmpty(Id);
    }

    public static readonly Tool[] All =
    {
        new Tool
        {
            Title = "Montar",
            Summary = "Roda, na ordem certa, tudo que a última mudança de código precisa que seja " +
                      "remontado. É o único item de montagem que precisa ser rodado — o Claude " +
                      "mantém a lista dentro do Montar.cs. Os itens do submenu Montagem continuam " +
                      "existindo para remontar uma coisa isolada.",
            MenuPath = SetupAllItem,
        },
        new Tool
        {
            Id = RaceId,
            Title = "Montar corrida (fundo + HUD)",
            Summary = "Põe na Game.unity o campo de estrelas que rola, o RaceSpeed que manda na " +
                      "velocidade e o painel de velocidade no rodapé.",
            MenuPath = RaceItem,
            UndoMenuPath = RaceUndoItem,
        },
        new Tool
        {
            Id = LevelsId,
            Title = "Criar fases e fichas de obstáculo",
            Summary = "Gera os três tipos de obstáculo, as três fases, a fase sem fim e o " +
                      "catálogo em Resources. Rodar de novo não sobrescreve o que você ajustou.",
            MenuPath = LevelsItem,
        },
        new Tool
        {
            Id = ShipsId,
            Title = "Criar ficha da nave",
            Summary = "Gera a ficha da nave inicial em Assets/Ships/ — os números da nave viraram " +
                      "arquivo de dados, e nave nova é arquivo novo. Rodar de novo não sobrescreve " +
                      "o que você ajustou. O 'Montar combate' liga a ficha na nave da cena.",
            MenuPath = ShipsItem,
        },
        new Tool
        {
            Id = BattleId,
            Title = "Montar combate (nave, obstáculos, fim)",
            Summary = "Nave com vida e tiro automático, obstáculos descendo pelas faixas, HUD de " +
                      "vida e os painéis de vitória e derrota. Depende do 'Montar corrida' e do " +
                      "'Criar ficha da nave'.",
            MenuPath = BattleItem,
            UndoMenuPath = BattleUndoItem,
        },
        new Tool
        {
            Id = MenuSceneId,
            Title = "Montar menu (recordes + transição)",
            Summary = "Botões maiores, botão Recordes entre Jogar e Sair, painel da tabela e a " +
                      "transição com o pódio antes da partida.",
            MenuPath = MenuSceneItem,
            UndoMenuPath = MenuSceneUndoItem,
        },
        new Tool
        {
            Id = LevelSelectId,
            Title = "Montar seleção de fase",
            Summary = "A tela que o \"Jogar\" abre: lista das fases do catálogo, dificuldade num " +
                      "seletor e cadeado no que ainda não foi destravado. Depende do 'Montar menu' " +
                      "e do 'Criar fases e fichas de obstáculo'.",
            MenuPath = LevelSelectItem,
            UndoMenuPath = LevelSelectUndoItem,
        },
        new Tool
        {
            Title = "Conferir configuração",
            Summary = "Confere cenas na build list, orientação, target SDK, keystore vazado e applicationId. " +
                      "Rodar sempre antes de um build.",
            MenuPath = PreflightItem,
        },
        new Tool
        {
            Title = "Gerar arte da loja",
            Summary = "Ícone 512×512 e gráfico de destaque 1024×500 em docs/play-console/arte/, " +
                      "opacos e no tamanho exato que a Play exige. Marcador de lugar para " +
                      "destravar o teste interno — sai fora no dia da arte de verdade.",
            MenuPath = StoreArtItem,
        },
        new Tool
        {
            Title = "Conferir SDK do Android",
            Summary = "Diz qual SDK o Editor usa e quais Platforms estão instaladas nele — o Unity " +
                      "Hub não instala nível de API, só o conjunto de ferramentas. Rodar antes de " +
                      "mexer no Target API Level, que é o que quebrou o build de 31/07/2026.",
            MenuPath = AndroidSdkItem,
        },
        new Tool
        {
            Title = "Build — APK de teste",
            Summary = "APK de desenvolvimento para instalar no aparelho.",
            MenuPath = BuildApkItem,
        },
        new Tool
        {
            Title = "Build — AAB de release",
            Summary = "Bundle assinado para a Play. Precisa das variáveis CNE_KEYSTORE_*.",
            MenuPath = BuildAabItem,
        },
    };

    static Log cache;

    static ProjectTools()
    {
        // O menu só existe depois que o Editor termina de carregar; marcar antes
        // disso não pega.
        EditorApplication.delayCall += SyncMenuChecks;
    }

    public static DateTime? LastRun(string id)
    {
        var entry = Load().Find(id);
        if (entry == null || !DateTime.TryParse(entry.when, out var when))
            return null;

        return when;
    }

    /// <summary>Chamado pela própria ferramenta quando ela termina com sucesso.</summary>
    public static void MarkRun(string id)
    {
        var log = Load();
        var entry = log.Find(id);

        if (entry == null)
        {
            entry = new Entry { id = id };
            log.entries.Add(entry);
        }

        entry.when = DateTime.Now.ToString("o");
        Save(log);
        SyncMenuChecks();
    }

    /// <summary>Apaga o registro. Os "Desmontar" chamam: desfez, então não está mais feito.</summary>
    public static void Forget(string id)
    {
        var log = Load();
        var entry = log.Find(id);
        if (entry == null)
            return;

        log.entries.Remove(entry);
        Save(log);
        SyncMenuChecks();
    }

    public static void Run(string menuPath)
    {
        // Adiado: a ferramenta pode abrir cena e caixa de diálogo, e nada disso
        // pode acontecer no meio do OnGUI de quem apertou o botão.
        EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem(menuPath);
    }

    /// <summary>
    /// Põe o ✓ no item de menu do que já rodou, para dar para ver sem abrir o
    /// painel. Qualificado como UnityEditor.Menu porque o projeto tem uma classe
    /// <c>Menu</c> própria (o script da Menu.unity), que ganharia do using.
    /// </summary>
    public static void SyncMenuChecks()
    {
        foreach (var tool in All)
        {
            if (tool.Tracked)
                UnityEditor.Menu.SetChecked(tool.MenuPath, LastRun(tool.Id) != null);
        }
    }

    static Log Load()
    {
        if (cache != null)
            return cache;

        cache = new Log();

        if (File.Exists(LogPath))
        {
            try
            {
                var parsed = JsonUtility.FromJson<Log>(File.ReadAllText(LogPath));
                if (parsed?.entries != null)
                    cache = parsed;
            }
            catch (Exception e)
            {
                // Diário corrompido não pode derrubar o Editor: no pior caso o
                // painel volta a mostrar tudo como não feito.
                Debug.LogWarning($"[Ferramentas] Não deu para ler {LogPath} ({e.Message}). Começando um diário novo.");
            }
        }

        return cache;
    }

    static void Save(Log log)
    {
        cache = log;

        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        File.WriteAllText(LogPath, JsonUtility.ToJson(log, true));
    }

    [Serializable]
    class Entry
    {
        public string id;
        public string when;
    }

    [Serializable]
    class Log
    {
        public List<Entry> entries = new List<Entry>();

        public Entry Find(string id) => entries.Find(e => e.id == id);
    }
}
