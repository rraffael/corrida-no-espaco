using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// O botão único de teste, irmão do <see cref="Montar"/>. O Raffael roda **só este**; quem
/// decide o que ele joga é o Claude, reescrevendo a lista <see cref="Rounds"/> a cada pedido.
///
/// Existe porque preencher a janela do simulador à mão, rodada por rodada, é onde um plano
/// sai errado sem ninguém ver: um nível desmarcado invalida a comparação só depois de horas.
/// </summary>
[InitializeOnLoad]
static class Testar
{
    /// <summary>Campo nulo quer dizer "todos": assim, coisa nova no jogo já entra no retrato.</summary>
    sealed class Round
    {
        public string Title;

        /// <summary>Nulo: todas, menos as de teste (<see cref="PilotPersona.staysPut"/>).</summary>
        public string[] Personas;

        public string[] Ships;
        public int[] ShipLevels;

        /// <summary>Nulo: todas as fases e a sem fim.</summary>
        public string[] Levels;

        public Difficulty[] Difficulties;
        public int Repetitions = 5;
        public int FirstSeed = 1;
        public int Frames = 30;
        public bool VariableFrames;
        public bool PowerOnDanger;
    }

    /// <summary>
    /// O que o teste faz hoje. **Esta lista é reescrita a cada pedido.** Lista vazia é resposta
    /// válida: não há nada para testar.
    ///
    /// Estado em 02/11/2026: a base nova e o bloco da Parada, que o retrato não mede. A 15 quadros,
    /// porque a prova de 19h46 deu a mesma Impressão a 30, a 15 e em quadro variável.
    /// </summary>
    static readonly Round[] Rounds =
    {
        new Round { Title = "Retrato completo", Frames = 15 },
        new Round
        {
            Title = "Bloco da Parada",
            Personas = new[] { "Persona-Teste-Parada" },
            Levels = new[] { "FaseInfinita" },
            Frames = 15,
        },
    };

    /// <summary>Verdadeiro: as rodadas têm de dar a mesma Impressão do CSV, e o veredito cobra isso.</summary>
    static readonly bool MustMatch = false;

    // ── A fila ───────────────────────────────────────────────────────────

    // Na sessão do Editor: entrar no Play recarrega os scripts e apagaria um campo estático.
    const string CurrentKey = "cne.testar.rodada-atual";
    const string SummariesKey = "cne.testar.resumos";
    const char SummarySeparator = '\n';

    static Testar() => RaceSimulatorDriver.RoundFinished += OnRoundFinished;

    /// <summary>Para a janela do simulador mostrar em que rodada a fila está. Vazio: sem fila.</summary>
    public static string Status
    {
        get
        {
            int current = SessionState.GetInt(CurrentKey, -1);
            return current >= 0 && current < Rounds.Length
                ? $"Testar: rodada {current + 1} de {Rounds.Length}, {Rounds[current].Title}"
                : string.Empty;
        }
    }

    [MenuItem(ProjectTools.TestItem, false, 91)]
    static void Run()
    {
        if (EditorApplication.isPlaying || RaceSimulatorDriver.IsRunning)
        {
            Debug.LogWarning("[Testar] Saia do Play antes: já há algo rodando.");
            return;
        }

        if (Rounds.Length == 0)
        {
            Debug.Log("[Testar] Nada a testar desta vez.");
            return;
        }

        // O plano inteiro antes da primeira corrida: um nome errado na terceira rodada
        // só apareceria depois de a primeira e a segunda terem gastado o tempo delas.
        var problems = new List<string>();
        var plans = Rounds.Select(round => BuildPlan(round, problems)).ToList();
        if (problems.Count > 0)
        {
            Debug.LogError("[Testar] O plano não fecha, e nada rodou:\n" + string.Join("\n", problems.Distinct()));
            return;
        }

        if (!GameChecks.PassesForBuild(out string checks))
        {
            Debug.LogError("[Testar] As verificações falharam, e nada rodou.\n" + checks);
            return;
        }

        if (!EditorUtility.DisplayDialog("Testar", Describe(plans), "Rodar", "Cancelar"))
            return;

        SessionState.EraseString(SummariesKey);
        EditorWindow.GetWindow<RaceSimulatorWindow>("Simulador de corridas");
        StartRound(0);
    }

    static string Describe(List<RaceSimulation.Plan> plans)
    {
        var text = new StringBuilder();
        int total = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            int races = Count(plans[i]);
            total += races;
            text.AppendLine($"{i + 1}. {Rounds[i].Title}: {races} corridas");
        }

        text.AppendLine();
        text.Append($"Total: {total} corridas.");

        float perRace = RaceSimulatorDriver.LastSecondsPerRace;
        if (perRace > 0f)
            text.Append($" Perto de {ScoreBoard.FormatTime(total * perRace)} pelo ritmo da última rodada, que pode ter sido outra.");

        text.AppendLine();
        text.AppendLine();
        text.Append("Faça o commit antes, e não salve nenhum .cs até terminar.");
        return text.ToString();
    }

    static void StartRound(int index)
    {
        var problems = new List<string>();
        var plan = BuildPlan(Rounds[index], problems);
        if (problems.Count > 0)
        {
            Conclude("Parou: o plano deixou de fechar. " + string.Join(" ", problems), passed: false);
            return;
        }

        SessionState.SetInt(CurrentKey, index);
        Debug.Log($"[Testar] Rodada {index + 1} de {Rounds.Length}: {Rounds[index].Title}");
        RaceSimulatorDriver.Start(plan, Count(plan));

        // Quem cancela a pergunta de salvar a cena desiste aqui, sem entrar no Play.
        if (!RaceSimulatorDriver.IsRunning)
            Conclude("Parou antes de começar a rodada.", passed: false);
    }

    /// <summary>
    /// A próxima rodada é adiada: entrar no Play de novo dentro do aviso de que se acabou de
    /// sair dele deixaria a Unity no meio da troca.
    /// </summary>
    static void OnRoundFinished()
    {
        int current = SessionState.GetInt(CurrentKey, -1);
        if (current < 0)
            return;

        var summary = RaceSimulatorDriver.LastRound;
        if (summary == null)
        {
            Conclude("Parou: a rodada terminou sem nenhuma corrida.", passed: false);
            return;
        }

        string previous = SessionState.GetString(SummariesKey, string.Empty);
        string json = JsonUtility.ToJson(summary);
        SessionState.SetString(SummariesKey, previous.Length > 0 ? previous + SummarySeparator + json : json);

        if (summary.races < summary.planned)
        {
            Conclude($"Parou na rodada {current + 1}: ela foi interrompida, e as seguintes não rodaram.", passed: false);
            return;
        }

        int next = current + 1;
        if (next < Rounds.Length)
        {
            EditorApplication.delayCall += () => StartRound(next);
            return;
        }

        if (MustMatch)
            ConcludeWithComparison();
        else
            Conclude("Todas as rodadas terminaram.", passed: true);
    }

    static List<RaceSimulatorDriver.RoundSummary> Summaries() =>
        SessionState.GetString(SummariesKey, string.Empty)
            .Split(SummarySeparator)
            .Where(line => line.Length > 0)
            .Select(line => JsonUtility.FromJson<RaceSimulatorDriver.RoundSummary>(line))
            .ToList();

    static void ConcludeWithComparison()
    {
        var summaries = Summaries();

        // O carimbo inclui o plano, que muda de propósito entre as rodadas: compara-se o resto.
        bool sameFiles = summaries.Select(s => WithoutPlan(s.stamp)).Distinct().Count() == 1 &&
                         !summaries.Any(s => s.stampChanged);
        bool samePrint = summaries.Select(s => s.print).Distinct().Count() == 1;

        if (!sameFiles)
            Conclude("NÃO PASSOU: arquivos mudaram durante o teste, e a comparação não vale.", passed: false);
        else if (!samePrint)
            Conclude("NÃO PASSOU: as Impressões do CSV são diferentes. Algo ainda depende dos quadros.", passed: false);
        else
            Conclude($"PASSOU: a mesma Impressão do CSV nas {summaries.Count} rodadas ({summaries[0].print}).", passed: true);
    }

    static string WithoutPlan(string stamp)
    {
        int plan = stamp.IndexOf(" · plano", System.StringComparison.Ordinal);
        return plan >= 0 ? stamp.Substring(0, plan) : stamp;
    }

    /// <summary>Grava o veredito ao lado dos relatórios: ele não pode depender de o Console estar limpo.</summary>
    static void Conclude(string verdict, bool passed)
    {
        var summaries = Summaries();
        SessionState.EraseInt(CurrentKey);
        SessionState.EraseString(SummariesKey);

        var text = new StringBuilder();
        text.AppendLine($"# Testar, {System.DateTime.Now:dd/MM/yyyy HH:mm}");
        text.AppendLine();
        text.AppendLine($"**{verdict}**");
        text.AppendLine();
        text.AppendLine("| Rodada | Corridas | Vitórias | Sem resultado por tempo | Usos de poder | Levou | Impressão | Relatório |");
        text.AppendLine("|---|---|---|---|---|---|---|---|");
        for (int i = 0; i < summaries.Count; i++)
        {
            var s = summaries[i];
            string title = i < Rounds.Length ? Rounds[i].Title : (i + 1).ToString();
            text.AppendLine($"| {title} | {s.races} de {s.planned} | {s.wins} | {s.timeouts} | {s.powerUses} | " +
                            $"{ScoreBoard.FormatTime(s.seconds)} | {s.print} | `{Path.GetFileName(s.report)}` |");
        }

        Directory.CreateDirectory(SimulationReport.Folder);
        string path = Path.Combine(SimulationReport.Folder, System.DateTime.Now.ToString("yyyy-MM-dd_HH'h'mm") + "_testar.md");
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));

        string message = $"[Testar] {verdict} Resumo: {path}";
        if (passed)
            Debug.Log(message);
        else
            Debug.LogError(message);

        // Adiado: um diálogo aqui prenderia a saída do Play, e o Editor ficaria parado no meio.
        EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Testar", verdict, "OK");
    }

    // ── O plano ──────────────────────────────────────────────────────────

    static int Count(RaceSimulation.Plan plan) =>
        plan.personas.Count * plan.ships.Count * plan.shipLevels.Count * plan.levels.Count *
        plan.difficulties.Count * plan.repetitions;

    static RaceSimulation.Plan BuildPlan(Round round, List<string> problems)
    {
        var plan = new RaceSimulation.Plan
        {
            repetitions = round.Repetitions,
            firstSeed = round.FirstSeed,
            frameSeconds = 1f / round.Frames,
            variableFrames = round.VariableFrames,
            powerOnDanger = round.PowerOnDanger,
        };

        var personas = RaceSimulatorWindow.Personas();
        var chosenPersonas = round.Personas == null
            ? personas.Where(p => !p.staysPut).ToList()
            : Pick(personas, round.Personas, p => p.name, "Persona", problems);
        plan.personas.AddRange(chosenPersonas.Select(p => JsonUtility.ToJson(p)));

        var ships = RaceSimulatorWindow.Ships();
        var chosenShips = round.Ships == null ? ships : Pick(ships, round.Ships, s => s.name, "Nave", problems);
        plan.ships.AddRange(chosenShips.Select(s => s.name));

        var levels = RaceSimulatorWindow.Levels();
        var chosenLevels = round.Levels == null ? levels : Pick(levels, round.Levels, l => l.name, "Fase", problems);
        plan.levels.AddRange(chosenLevels.Select(l => l.name));

        plan.shipLevels.AddRange(round.ShipLevels ?? RaceSimulatorWindow.Tiers());
        plan.difficulties.AddRange(round.Difficulties ??
                                   new[] { Difficulty.Facil, Difficulty.Normal, Difficulty.Dificil });

        if (Count(plan) == 0)
            problems.Add($"A rodada \"{round.Title}\" não tem corrida nenhuma.");

        return plan;
    }

    static List<T> Pick<T>(List<T> all, string[] names, System.Func<T, string> nameOf, string kind, List<string> problems)
    {
        var picked = new List<T>();
        foreach (string name in names)
        {
            var found = all.Where(item => nameOf(item) == name).ToList();
            if (found.Count == 0)
                problems.Add($"{kind} não encontrada: {name}");
            else
                picked.Add(found[0]);
        }

        return picked;
    }
}
