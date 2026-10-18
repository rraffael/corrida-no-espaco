using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// A janela do **simulador de corridas** (Tools → Corrida no Espaço → Simulador
/// de corridas): escolhe personas, naves, níveis, fases, dificuldades e quantas
/// repetições, aperta Rodar, e o Editor entra no Play jogando sozinho.
///
/// O que roda a corrida é o <see cref="RaceSimulation"/>; aqui é o plano, a
/// barra de progresso e o relatório no fim. Ver também <see cref="PilotPersona"/>.
/// </summary>
public class RaceSimulatorWindow : EditorWindow
{
    const string SecondsPerRaceKey = "cne.simulador.segundos-por-corrida";

    /// <summary>O que está desmarcado, pelo nome do asset. Guardar o desmarcado faz coisa nova nascer marcada.</summary>
    [SerializeField] List<string> excluded = new List<string>();

    /// <summary>Os patamares marcados (1, 15, 40, 60 pelo molde de evolução).</summary>
    [SerializeField] List<int> tierLevels = new List<int> { 1 };

    /// <summary>Níveis fora dos patamares, por vírgula.</summary>
    [SerializeField] string extraLevelsText = string.Empty;
    [SerializeField] bool easy;
    [SerializeField] bool normal = true;
    [SerializeField] bool hard;
    [SerializeField] int repetitions = 5;
    [SerializeField] int firstSeed = 1;
    [SerializeField] float maxSeconds = 240f;
    [SerializeField] float maxEndlessSeconds = 300f;
    [SerializeField] bool sixtyFps;
    [SerializeField] bool hideRendering = true;

    Vector2 scroll;

    [MenuItem(ProjectTools.SimulatorItem, false, 153)]
    static void Open() => GetWindow<RaceSimulatorWindow>("Simulador de corridas");

    void OnEnable() => EditorApplication.update += RepaintWhileRunning;

    void OnDisable() => EditorApplication.update -= RepaintWhileRunning;

    void RepaintWhileRunning()
    {
        if (RaceSimulatorDriver.IsRunning)
            Repaint();
    }

    void OnGUI()
    {
        if (RaceSimulatorDriver.IsRunning)
        {
            DrawProgress();
            return;
        }

        var personas = Personas();
        var ships = Ships();
        var levels = Levels();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Joga corridas de verdade, em Play acelerado, com as personas no lugar do dedo. O relatório " +
            "vai para docs/simulacoes/. Nada é gravado no salvamento.\n\n" +
            "Para comparar: rode, mexa na nave/fase/obstáculo, rode o MESMO plano de novo. As sementes " +
            "se repetem, então a diferença é da mudança, e não da sorte.", MessageType.Info);

        if (personas.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma persona encontrada. Rode Tools → Corrida no Espaço → Montar.",
                                    MessageType.Warning);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent("Marcar o retrato completo",
                "Todas as personas, naves, patamares de nível, fases (menos a sem fim) e dificuldades. " +
                "Para comparar tudo com tudo numa rodada só — é a mais longa.")))
            MarkEverything(personas, ships, levels);

        Section("Personas", personas.Select(p => p.name));
        foreach (var persona in personas)
            Toggle(persona.name, $"{persona.skillRank}. {persona.displayName}");

        Section("Naves", ships.Select(s => s.name));
        foreach (var ship in ships)
            Toggle(ship.name, ship.displayName);

        Section("Níveis da nave");
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (int tier in Tiers())
            {
                bool on = tierLevels.Contains(tier);
                bool now = EditorGUILayout.ToggleLeft(tier.ToString(), on, GUILayout.Width(50));
                if (now && !on) tierLevels.Add(tier);
                if (!now && on) tierLevels.Remove(tier);
            }
        }
        extraLevelsText = EditorGUILayout.TextField(
            new GUIContent("Outros níveis", "Fora dos patamares, separados por vírgula. Ex.: 5, 30"), extraLevelsText);

        Section("Fases", levels.Select(l => l.name));
        foreach (var level in levels)
            Toggle(level.name, level.displayName);

        Section("Dificuldades");
        easy = EditorGUILayout.ToggleLeft("Fácil", easy);
        normal = EditorGUILayout.ToggleLeft("Normal", normal);
        hard = EditorGUILayout.ToggleLeft("Difícil", hard);

        Section("Rodada");
        repetitions = Mathf.Max(1, EditorGUILayout.IntField(
            new GUIContent("Repetições", "Corridas de cada combinação. A fase é sorteada; com poucas, o " +
                                         "resultado pode ser sorte."), repetitions));
        firstSeed = EditorGUILayout.IntField(
            new GUIContent("Primeira semente", "Mude para ver outras pistas. Igual entre duas rodadas = " +
                                               "mesmas largadas."), firstSeed);
        maxSeconds = Mathf.Max(30f, EditorGUILayout.FloatField("Teto das fases (s)", maxSeconds));
        maxEndlessSeconds = Mathf.Max(30f, EditorGUILayout.FloatField("Teto da sem fim (s)", maxEndlessSeconds));
        sixtyFps = EditorGUILayout.ToggleLeft("60 quadros por segundo (o celular roda a 30)", sixtyFps);
        hideRendering = EditorGUILayout.ToggleLeft("Não desenhar a tela (bem mais rápido)", hideRendering);

        EditorGUILayout.EndScrollView();

        var plan = BuildPlan(personas, ships, levels);
        int total = Count(plan);

        EditorGUILayout.Space();
        float perRace = EditorPrefs.GetFloat(SecondsPerRaceKey, 0f);
        EditorGUILayout.LabelField($"{total} corrida(s)" +
            (perRace > 0f ? $" — perto de {ScoreBoard.FormatTime(total * perRace)} pela última rodada" : string.Empty));

        using (new EditorGUI.DisabledScope(total == 0 || EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Rodar", GUILayout.Height(30)))
                RaceSimulatorDriver.Start(plan, total);
        }

        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Saia do Play para rodar o simulador.", MessageType.None);

        string last = RaceSimulatorDriver.LastReport;
        if (!string.IsNullOrEmpty(last))
        {
            EditorGUILayout.LabelField("Último relatório: " + last, EditorStyles.miniLabel);
            if (GUILayout.Button("Mostrar na pasta"))
                EditorUtility.RevealInFinder(last);
        }
    }

    void DrawProgress()
    {
        int done = RaceSimulation.Results.Count;
        int total = Mathf.Max(1, RaceSimulatorDriver.Planned);
        float real = RaceSimulation.RealSeconds;
        string left = done > 0 ? $" — falta perto de {ScoreBoard.FormatTime(real / done * (total - done))}" : string.Empty;

        var rect = GUILayoutUtility.GetRect(18, 22, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(rect, (float)done / total, $"{done} de {total}{left}");

        if (GUILayout.Button("Parar e salvar o que já rodou"))
            RaceSimulation.Stop();
    }

    // ── Plano ────────────────────────────────────────────────────────────

    RaceSimulation.Plan BuildPlan(List<PilotPersona> personas, List<ShipDefinition> ships,
                                  List<LevelDefinition> levels)
    {
        var plan = new RaceSimulation.Plan
        {
            repetitions = repetitions,
            firstSeed = firstSeed,
            maxSeconds = maxSeconds,
            maxEndlessSeconds = maxEndlessSeconds,
            frameSeconds = sixtyFps ? 1f / 60f : 1f / 30f,
            hideRendering = hideRendering,
        };

        foreach (var persona in personas.Where(p => !excluded.Contains(p.name)))
            plan.personas.Add(JsonUtility.ToJson(persona));

        plan.ships.AddRange(ships.Where(s => !excluded.Contains(s.name)).Select(s => s.name));
        plan.levels.AddRange(levels.Where(l => !excluded.Contains(l.name)).Select(l => l.name));

        var shipLevels = new List<int>(tierLevels);
        foreach (string part in extraLevelsText.Split(',', ';', ' '))
        {
            if (int.TryParse(part.Trim(), out int level) && level >= 1)
                shipLevels.Add(level);
        }
        plan.shipLevels.AddRange(shipLevels.Distinct().OrderBy(l => l));

        if (easy) plan.difficulties.Add(Difficulty.Facil);
        if (normal) plan.difficulties.Add(Difficulty.Normal);
        if (hard) plan.difficulties.Add(Difficulty.Dificil);

        return plan;
    }

    static int Count(RaceSimulation.Plan plan) =>
        plan.personas.Count * plan.ships.Count * plan.shipLevels.Count * plan.levels.Count *
        plan.difficulties.Count * plan.repetitions;

    static List<PilotPersona> Personas() =>
        AssetDatabase.FindAssets("t:PilotPersona")
            .Select(guid => AssetDatabase.LoadAssetAtPath<PilotPersona>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(p => p != null)
            .OrderBy(p => p.skillRank)
            .ToList();

    static List<ShipDefinition> Ships()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalog>(ShipSetup.CatalogPath);
        return catalog != null ? catalog.ships.Where(s => s != null).ToList() : new List<ShipDefinition>();
    }

    static List<LevelDefinition> Levels()
    {
        var catalog = LevelCatalog.Load();
        var levels = new List<LevelDefinition>();
        if (catalog == null)
            return levels;

        levels.AddRange(catalog.levels.Where(l => l != null).OrderBy(l => l.order));
        if (catalog.endlessLevel != null)
            levels.Add(catalog.endlessLevel);

        return levels;
    }

    /// <summary>Os patamares do molde de evolução — os mesmos níveis do painel de balanceamento.</summary>
    static int[] Tiers()
    {
        var model = ShipEvolutionModel.Default;
        if (model == null)
            return new[] { 1, 15, 40, 60 };

        return new[] { 1, model.upgrade1Level, model.upgrade2Level, model.maxLevel }.Distinct().ToArray();
    }

    void MarkEverything(List<PilotPersona> personas, List<ShipDefinition> ships, List<LevelDefinition> levels)
    {
        excluded.Clear();

        // A sem fim fica de fora: não tem vitória, e o teto de tempo dela
        // alongaria a rodada sem dizer nada sobre as fases.
        var catalog = LevelCatalog.Load();
        if (catalog != null && catalog.endlessLevel != null)
            excluded.Add(catalog.endlessLevel.name);

        tierLevels = Tiers().ToList();
        extraLevelsText = string.Empty;
        easy = normal = hard = true;
    }

    static void Section(string title)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    /// <summary>Título com "Todas" e "Nenhuma" ao lado, para as listas de marcar.</summary>
    void Section(string title, IEnumerable<string> keys)
    {
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (GUILayout.Button("Todas", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                excluded.RemoveAll(keys.Contains);
            if (GUILayout.Button("Nenhuma", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                excluded.AddRange(keys.Where(k => !excluded.Contains(k)));
        }
    }

    void Toggle(string key, string label)
    {
        bool on = !excluded.Contains(key);
        bool now = EditorGUILayout.ToggleLeft(label, on);
        if (now == on)
            return;

        if (now)
            excluded.Remove(key);
        else
            excluded.Add(key);
    }
}

/// <summary>
/// Leva o plano até o Play e o relatório de volta. Fica fora da janela porque
/// a janela pode estar fechada quando o simulador termina — e o relatório não
/// pode depender de ela estar aberta.
/// </summary>
[InitializeOnLoad]
static class RaceSimulatorDriver
{
    const string RunningKey = "cne.simulador.rodando";
    const string PlannedKey = "cne.simulador.planejadas";
    const string LastReportKey = "cne.simulador.ultimo-relatorio";
    const string SecondsPerRaceKey = "cne.simulador.segundos-por-corrida";

    public static bool IsRunning => SessionState.GetBool(RunningKey, false);
    public static int Planned => SessionState.GetInt(PlannedKey, 0);
    public static string LastReport => SessionState.GetString(LastReportKey, string.Empty);

    static RaceSimulatorDriver()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Start(RaceSimulation.Plan plan, int planned)
    {
        // A cena aberta pode ter mudança não salva; o Play não a perde, mas
        // quem volta do simulador tem de achar tudo como deixou.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Game.unity");
        if (scene == null)
        {
            Debug.LogError("[Simulador] Não achei a Assets/Scenes/Game.unity.");
            return;
        }

        SessionState.SetString(RaceSimulation.PlanKey, JsonUtility.ToJson(plan));
        SessionState.SetInt(PlannedKey, planned);
        SessionState.SetBool(RunningKey, true);

        // O Play começa direto na Game.unity, sem passar pelo menu, qualquer que
        // seja a cena aberta. Volta ao normal quando o Play acaba.
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.EnterPlaymode();
    }

    static void Poll()
    {
        if (IsRunning && EditorApplication.isPlaying && RaceSimulation.IsDone)
            EditorApplication.ExitPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (!IsRunning)
            return;

        // Saindo do Play — pelo fim do plano, pelo "Parar" ou pelo botão de Play
        // da Unity. Nos três casos, o que rodou vira relatório.
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            RaceSimulation.Stop();
            WriteReport();
            return;
        }

        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(RunningKey, false);
            SessionState.EraseString(RaceSimulation.PlanKey);
            EditorSceneManager.playModeStartScene = null;
            Time.captureDeltaTime = 0f;
            SaveGame.EndSandbox();
        }
    }

    static void WriteReport()
    {
        var results = RaceSimulation.Results;
        if (RaceSimulation.Current == null || results.Count == 0)
        {
            Debug.LogWarning("[Simulador] Nenhuma corrida terminou — não há relatório.");
            return;
        }

        float real = RaceSimulation.RealSeconds;
        EditorPrefs.SetFloat(SecondsPerRaceKey, real / results.Count);

        string path = SimulationReport.Write(RaceSimulation.Current, results, Planned, real);
        SessionState.SetString(LastReportKey, path);

        Debug.Log($"[Simulador] {results.Count} corrida(s) em {ScoreBoard.FormatTime(real)}. Relatório: {path}");
    }
}
