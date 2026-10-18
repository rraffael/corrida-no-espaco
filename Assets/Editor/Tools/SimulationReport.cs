using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// Escreve o que o simulador de corridas jogou: um <c>.md</c> para ler e um
/// <c>.csv</c> com cada corrida, em <c>docs/simulacoes/</c>.
///
/// **Em docs, e não em pasta ignorada**, de propósito: o valor de um relatório
/// é ser comparado com o próximo — a nave antes e depois do ajuste. Commitar o
/// que valer a pena guardar fica a critério do Raffael.
/// </summary>
static class SimulationReport
{
    public const string Folder = "docs/simulacoes";

    static readonly CultureInfo Br = new CultureInfo("pt-BR");

    /// <summary>Grava os dois arquivos e devolve o caminho do <c>.md</c>.</summary>
    public static string Write(RaceSimulation.Plan plan, IReadOnlyList<RaceSimulation.Result> results,
                               int planned, float realSeconds)
    {
        Directory.CreateDirectory(Folder);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH'h'mm");
        string markdown = Path.Combine(Folder, stamp + ".md");
        string csv = Path.Combine(Folder, stamp + ".csv");

        File.WriteAllText(csv, Csv(results), new UTF8Encoding(true));
        File.WriteAllText(markdown, Markdown(plan, results, planned, realSeconds, Path.GetFileName(csv)),
                          new UTF8Encoding(false));

        return markdown;
    }

    // ── Markdown ─────────────────────────────────────────────────────────

    static string Markdown(RaceSimulation.Plan plan, IReadOnlyList<RaceSimulation.Result> results,
                           int planned, float realSeconds, string csvName)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Simulação de {DateTime.Now:dd/MM/yyyy HH:mm}");
        text.AppendLine();
        text.AppendLine($"- **Corridas:** {results.Count} de {planned}" +
                        (results.Count < planned ? " — **interrompida**, o que está aqui é o que rodou" : string.Empty));
        text.AppendLine($"- **Repetições por combinação:** {plan.repetitions} (sementes {plan.firstSeed} a " +
                        $"{plan.firstSeed + plan.repetitions - 1})");
        text.AppendLine($"- **Quadro:** {Mathf.RoundToInt(1f / plan.frameSeconds)} por segundo · **teto:** " +
                        $"{plan.maxSeconds:0} s nas fases, {plan.maxEndlessSeconds:0} s na sem fim");
        text.AppendLine($"- **Levou:** {ScoreBoard.FormatTime(realSeconds)} de relógio");
        text.AppendLine($"- **Cada corrida:** `{csvName}`");
        text.AppendLine();
        text.AppendLine("> **Como ler.** Uma persona sozinha diz pouco; o que mede é a mesma persona em duas " +
                        "circunstâncias — antes e depois de mexer. Com 5 repetições, cada corrida vale 20 pontos " +
                        "na taxa de vitória: diferença de uma corrida pode ser sorte. As sementes são as mesmas " +
                        "a cada rodada, então rodar o mesmo plano depois de uma mudança compara pistas parecidas.");
        text.AppendLine();

        var races = results.Where(r => !r.endless).ToList();
        var endless = results.Where(r => r.endless).ToList();

        if (races.Count > 0)
        {
            text.AppendLine("## Vitórias por persona e fase");
            text.AppendLine();
            Matrix(text, races, r => $"{r.levelName} · {DifficultyName(r.difficulty)}",
                   r => (r.levelOrder, (int)r.difficulty), WinCell);

            text.AppendLine("## Vitórias por persona e nave");
            text.AppendLine();
            Matrix(text, races, r => ShipLabel(r), r => (0, 0), WinCell);

            // Com várias naves e níveis, a tabela de cima vira uma fileira de
            // colunas; deitada, uma nave por linha, a discrepância salta aos olhos.
            if (races.Select(r => (r.shipName, r.shipLevel)).Distinct().Count() > 1)
            {
                text.AppendLine("## Vitórias por nave e nível — todas as fases somadas");
                text.AppendLine();
                ShipRows(text, races);
            }
        }

        if (endless.Count > 0)
        {
            text.AppendLine("## Sem fim — distância média por persona e nave");
            text.AppendLine();
            Matrix(text, endless, r => ShipLabel(r), r => (0, 0),
                   group => ScoreBoard.FormatDistance(group.Average(r => r.distance)));
        }

        text.AppendLine("## Detalhe");
        text.AppendLine();

        var groups = results
            .GroupBy(r => (r.levelOrder, r.levelName, r.difficulty, r.shipName, r.shipLevel, r.endless))
            .OrderBy(g => g.Key.levelOrder).ThenBy(g => g.Key.difficulty)
            .ThenBy(g => g.Key.shipName).ThenBy(g => g.Key.shipLevel);

        foreach (var group in groups)
        {
            var key = group.Key;
            text.AppendLine($"### {key.levelName} · {DifficultyName(key.difficulty)} · {key.shipName} nível {key.shipLevel}");
            text.AppendLine();

            if (key.endless)
            {
                text.AppendLine("| Persona | Distância | Tempo | Vel. máx. | Batidas | Abates | Poder |");
                text.AppendLine("|---|---|---|---|---|---|---|");
            }
            else
            {
                text.AppendLine("| Persona | Vitórias | Tempo da vitória | Chegou na dobra | Batidas | Abates | Vida no fim | Poder |");
                text.AppendLine("|---|---|---|---|---|---|---|---|");
            }

            foreach (var persona in group.GroupBy(r => (r.personaRank, r.persona)).OrderBy(p => p.Key.personaRank))
            {
                var runs = persona.ToList();
                if (key.endless)
                {
                    text.AppendLine($"| {persona.Key.persona} " +
                                    $"| {Spread(runs.Select(r => r.distance), ScoreBoard.FormatDistance)} " +
                                    $"| {ScoreBoard.FormatTime(runs.Average(r => r.seconds))} " +
                                    $"| {RaceSpeed.Format(runs.Average(r => r.topSpeed))} " +
                                    $"| {Number(runs.Average(r => r.crashes))} " +
                                    $"| {Number(runs.Average(r => r.kills))} " +
                                    $"| {Number(runs.Average(r => r.powerUses))} |");
                    continue;
                }

                var wins = runs.Where(r => r.outcome == RaceSimulation.Outcome.Vitoria).ToList();
                var reached = runs.Where(r => r.warpReachedAt >= 0f).ToList();

                text.AppendLine($"| {persona.Key.persona} " +
                                $"| {WinCell(runs)} " +
                                $"| {(wins.Count > 0 ? Spread(wins.Select(r => r.seconds), ScoreBoard.FormatTime) : "—")} " +
                                $"| {(reached.Count > 0 ? $"{ScoreBoard.FormatTime(reached.Average(r => r.warpReachedAt))} ({reached.Count}/{runs.Count})" : "nunca")} " +
                                $"| {Number(runs.Average(r => r.crashes))} " +
                                $"| {Number(runs.Average(r => r.kills))} " +
                                $"| {runs.Average(r => r.healthLeft) * 100f:0}% " +
                                $"| {Number(runs.Average(r => r.powerUses))} |");
            }

            text.AppendLine();
        }

        return text.ToString();
    }

    /// <summary>Linhas por persona, colunas pelo que <paramref name="column"/> disser.</summary>
    static void Matrix(StringBuilder text, List<RaceSimulation.Result> results,
                       Func<RaceSimulation.Result, string> column,
                       Func<RaceSimulation.Result, (int, int)> columnOrder,
                       Func<List<RaceSimulation.Result>, string> cell)
    {
        var columns = results
            .GroupBy(column)
            .OrderBy(g => columnOrder(g.First())).ThenBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();

        text.AppendLine("| Persona | " + string.Join(" | ", columns) + " |");
        text.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---|", columns.Count)));

        foreach (var persona in results.GroupBy(r => (r.personaRank, r.persona)).OrderBy(p => p.Key.personaRank))
        {
            var cells = columns.Select(c =>
            {
                var group = persona.Where(r => column(r) == c).ToList();
                return group.Count > 0 ? cell(group) : "—";
            });

            text.AppendLine($"| {persona.Key.persona} | " + string.Join(" | ", cells) + " |");
        }

        text.AppendLine();
    }

    /// <summary>Linhas por nave e nível, colunas por persona, e o total da linha no fim.</summary>
    static void ShipRows(StringBuilder text, List<RaceSimulation.Result> results)
    {
        var personas = results
            .GroupBy(r => (r.personaRank, r.persona))
            .OrderBy(p => p.Key.personaRank)
            .Select(p => p.Key)
            .ToList();

        text.AppendLine("| Nave | " + string.Join(" | ", personas.Select(p => p.persona)) + " | Todas |");
        text.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---|", personas.Count + 1)));

        foreach (var ship in results.GroupBy(r => (r.shipName, r.shipLevel))
                                    .OrderBy(g => g.Key.shipName).ThenBy(g => g.Key.shipLevel))
        {
            var cells = personas.Select(p =>
            {
                var group = ship.Where(r => r.personaRank == p.personaRank && r.persona == p.persona).ToList();
                return group.Count > 0 ? WinCell(group) : "—";
            });

            text.AppendLine($"| {ship.Key.shipName} nv {ship.Key.shipLevel} | " + string.Join(" | ", cells) +
                            $" | {WinCell(ship.ToList())} |");
        }

        text.AppendLine();
    }

    static string WinCell(List<RaceSimulation.Result> runs)
    {
        int wins = runs.Count(r => r.outcome == RaceSimulation.Outcome.Vitoria);
        int timedOut = runs.Count(r => r.outcome == RaceSimulation.Outcome.TempoEsgotado);

        string cell = $"{wins}/{runs.Count} ({100f * wins / runs.Count:0}%)";
        return timedOut > 0 ? cell + $" · {timedOut} esgotou" : cell;
    }

    /// <summary>"média (mín–máx)" — o espalhamento é o que diz quanto foi sorte.</summary>
    static string Spread(IEnumerable<float> values, Func<float, string> format)
    {
        var list = values.ToList();
        if (list.Count == 1)
            return format(list[0]);

        return $"{format(list.Average())} ({format(list.Min())}–{format(list.Max())})";
    }

    static string ShipLabel(RaceSimulation.Result r) => $"{r.shipName} nv {r.shipLevel}";

    static string Number(double value) => value.ToString("0.#", Br);

    static string DifficultyName(Difficulty difficulty)
    {
        var catalog = LevelCatalog.Load();
        return catalog != null ? catalog.SettingsFor(difficulty).displayName : difficulty.ToString();
    }

    // ── CSV ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Ponto e vírgula e vírgula decimal: é o que o Excel e o Google Planilhas
    /// em português abrem sem perguntar nada.
    /// </summary>
    static string Csv(IReadOnlyList<RaceSimulation.Result> results)
    {
        var text = new StringBuilder();
        text.AppendLine("persona;nivel_persona;nave;nivel_nave;fase;dificuldade;semente;desfecho;segundos;" +
                        "distancia;vel_max;dobra_em;abates;batidas;vida_no_fim;poder_usado");

        foreach (var r in results)
        {
            text.AppendLine(string.Join(";",
                r.persona, r.personaRank, r.shipName, r.shipLevel, r.levelName,
                DifficultyName(r.difficulty), r.seed, r.outcome,
                Comma(r.seconds), Comma(r.distance), RaceSpeed.Shown(r.topSpeed),
                r.warpReachedAt >= 0f ? Comma(r.warpReachedAt) : string.Empty,
                r.kills, r.crashes, Comma(r.healthLeft), r.powerUses));
        }

        return text.ToString();
    }

    static string Comma(float value) => value.ToString("0.##", Br);
}
