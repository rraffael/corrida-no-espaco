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

    /// <summary>A rodada em andamento, regravada de tempos em tempos. Sai quando o relatório fica pronto.</summary>
    public const string PartialCsv = Folder + "/_parcial.csv";

    static readonly CultureInfo Br = new CultureInfo("pt-BR");

    /// <summary>
    /// Grava o que já rodou, para uma queda de energia ou um reinício do Windows
    /// não levarem horas de corrida junto.
    /// </summary>
    public static void WritePartial(IReadOnlyList<RaceSimulation.Result> results)
    {
        Directory.CreateDirectory(Folder);

        // Grava ao lado e troca no fim: uma queda de energia no meio da gravação não
        // pode destruir o parcial que já existia.
        string temporary = PartialCsv + ".tmp";
        File.WriteAllText(temporary, Csv(results), new UTF8Encoding(true));

        if (File.Exists(PartialCsv))
            File.Replace(temporary, PartialCsv, null);
        else
            File.Move(temporary, PartialCsv);
    }

    /// <summary>Grava os dois arquivos e devolve o caminho do <c>.md</c>.</summary>
    public static string Write(RaceSimulation.Plan plan, IReadOnlyList<RaceSimulation.Result> results,
                               IReadOnlyList<RaceSimulation.Result> sentinels,
                               int planned, float realSeconds, string stampAtStart, string stampAtEnd)
    {
        Directory.CreateDirectory(Folder);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH'h'mm");
        string markdown = Path.Combine(Folder, stamp + ".md");
        string csv = Path.Combine(Folder, stamp + ".csv");

        string csvText = Csv(results);
        File.WriteAllText(csv, csvText, new UTF8Encoding(true));
        File.WriteAllText(markdown, Markdown(plan, results, sentinels, planned, realSeconds, Path.GetFileName(csv),
                                             stampAtStart, stampAtEnd, SimulationStamp.Of(csvText)),
                          new UTF8Encoding(false));

        if (File.Exists(PartialCsv))
            File.Delete(PartialCsv);

        return markdown;
    }

    // ── Markdown ─────────────────────────────────────────────────────────

    static string Markdown(RaceSimulation.Plan plan, IReadOnlyList<RaceSimulation.Result> results,
                           IReadOnlyList<RaceSimulation.Result> sentinels,
                           int planned, float realSeconds, string csvName,
                           string stampAtStart, string stampAtEnd, string csvPrint)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Simulação de {DateTime.Now:dd/MM/yyyy HH:mm}");
        text.AppendLine();
        text.AppendLine($"- **Corridas:** {results.Count} de {planned}" +
                        (results.Count < planned ? " — **interrompida**, o que está aqui é o que rodou" : string.Empty));
        text.AppendLine($"- **Repetições por combinação:** {plan.repetitions} (sementes {plan.firstSeed} a " +
                        $"{plan.firstSeed + plan.repetitions - 1})");
        string frames = plan.variableFrames ? "variável, de 15 a 120" : Mathf.RoundToInt(1f / plan.frameSeconds).ToString();
        text.AppendLine($"- **Quadro:** {frames} por segundo, com passo de jogo de " +
                        $"{Mathf.RoundToInt(1f / RaceClock.TickSeconds)} por segundo · **teto:** " +
                        $"{plan.maxSeconds:0} s nas fases, {plan.maxEndlessSeconds:0} s na sem fim");
        text.AppendLine($"- **Levou:** {ScoreBoard.FormatTime(realSeconds)} de relógio");
        text.AppendLine($"- **Cada corrida:** `{csvName}`");
        text.AppendLine($"- **Carimbo:** {stampAtStart}");
        if (stampAtStart != stampAtEnd)
            text.AppendLine($"- **ATENÇÃO, arquivos mudaram durante a rodada.** No fim: {stampAtEnd}");
        text.AppendLine($"- **Impressão do CSV:** {csvPrint}. Mesmo plano e mesmo carimbo têm de dar a mesma");
        text.AppendLine($"- **Quadros fora do passo:** {results.Sum(r => r.offStepFrames)}" +
                        (results.Count > 0 ? $" (na 1ª corrida: {results[0].offStepFrames})" : string.Empty));
        text.AppendLine($"- **Colisões por trecho:** {results.Where(r => !r.endless).Sum(r => r.sweepHits)} nas fases · " +
                        $"{results.Where(r => r.endless).Sum(r => r.sweepHits)} na sem fim");
        if (planned >= RaceSimulation.SentinelMinimumJobs)
            text.AppendLine($"- **Sentinela:** {SentinelVerdict(results, sentinels)}");
        if (plan.powerOnDanger)
            text.AppendLine("- **Teto do poder ligado:** toda persona apertou o poder no primeiro perigo, " +
                            "ignorando o hábito dela — aqui se mede o poder, e não quem o usa");
        text.AppendLine();
        text.AppendLine("> **Como ler.** Uma persona sozinha diz pouco; o que mede é a mesma persona em duas " +
                        "circunstâncias — antes e depois de mexer. Com 5 repetições, cada corrida vale 20 pontos " +
                        "na taxa de vitória: diferença de uma corrida pode ser sorte. Semente, fase e dificuldade " +
                        "definem a pista, então rodar o mesmo plano depois de uma mudança compara pistas parecidas. " +
                        "Relatórios anteriores a 02/11/2026 usavam outra sorte: com eles, só valem os totais.");
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
                text.AppendLine("## Vitórias por nave e nível — todas as fases e dificuldades somadas");
                text.AppendLine();
                ShipRows(text, races);

                var difficulties = races.Select(r => r.difficulty).Distinct().OrderBy(d => d).ToList();
                if (difficulties.Count > 1)
                {
                    foreach (var difficulty in difficulties)
                    {
                        text.AppendLine($"### Por nave e nível — só o {DifficultyName(difficulty)}");
                        text.AppendLine();
                        ShipRows(text, races.Where(r => r.difficulty == difficulty).ToList());
                    }
                }
            }

            // Só as naves de poder ativo: na Nave inicial e na Predadora a
            // pergunta nem existe.
            var powered = races.Where(r => r.hasPower).ToList();
            if (powered.Any(r => r.outcome == RaceSimulation.Outcome.Derrota))
            {
                text.AppendLine("## Derrotas com o poder na mão");
                text.AppendLine();
                text.AppendLine("> Das derrotas, quantas terminaram com o poder pronto e sem usar. Alto aqui quer " +
                                "dizer que o poder estava lá e a persona não o apertou.");
                text.AppendLine();
                Matrix(text, powered, r => ShipLabel(r), r => (0, 0), UnusedPowerCell);
            }

            // O trunfo da Raffa é este número: quem chega à dobra termina a fase?
            if (races.Any(r => r.warpReachedAt >= 0f))
            {
                text.AppendLine("## Das que chegaram à dobra, quantas venceram");
                text.AppendLine();
                Matrix(text, races, r => ShipLabel(r), r => (0, 0), ReachedWarpCell);
            }
        }

        if (endless.Count > 0)
        {
            text.AppendLine("## Sem fim — distância média por persona e nave");
            text.AppendLine();
            Matrix(text, endless, r => ShipLabel(r), r => (0, 0),
                   group => ScoreBoard.FormatDistance(group.Average(r => r.distance)));

            // A média das três engana: o Fácil pesa muito mais na distância.
            var endlessDifficulties = endless.Select(r => r.difficulty).Distinct().OrderBy(d => d).ToList();
            if (endlessDifficulties.Count > 1)
            {
                foreach (var difficulty in endlessDifficulties)
                {
                    text.AppendLine($"### Sem fim — só o {DifficultyName(difficulty)}");
                    text.AppendLine();
                    Matrix(text, endless.Where(r => r.difficulty == difficulty).ToList(),
                           r => ShipLabel(r), r => (0, 0),
                           group => ScoreBoard.FormatDistance(group.Average(r => r.distance)));
                }
            }
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

    /// <summary>As corridas repetidas no fim têm de sair iguais às do começo, em todas as colunas.</summary>
    static string SentinelVerdict(IReadOnlyList<RaceSimulation.Result> results,
                                  IReadOnlyList<RaceSimulation.Result> sentinels)
    {
        if (sentinels.Count == 0)
            return "não rodou, porque a rodada parou antes";

        int same = 0;
        for (int i = 0; i < sentinels.Count && i < results.Count; i++)
        {
            if (CsvLine(results[i]) == CsvLine(sentinels[i]))
                same++;
        }

        return same == sentinels.Count
            ? $"{same} de {sentinels.Count} corridas iguais às do começo"
            : $"**ATENÇÃO, só {same} de {sentinels.Count} iguais às do começo: a medição mudou durante a rodada**";
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

    static string UnusedPowerCell(List<RaceSimulation.Result> runs)
    {
        int losses = runs.Count(r => r.outcome == RaceSimulation.Outcome.Derrota);
        if (losses == 0)
            return "sem derrota";

        int unused = runs.Count(r => r.diedWithPowerReady);
        return $"{unused}/{losses} ({100f * unused / losses:0}%)";
    }

    static string ReachedWarpCell(List<RaceSimulation.Result> runs)
    {
        int reached = runs.Count(r => r.warpReachedAt >= 0f);
        if (reached == 0)
            return "não chegou";

        int wins = runs.Count(r => r.warpReachedAt >= 0f && r.outcome == RaceSimulation.Outcome.Vitoria);
        return $"{wins}/{reached} ({100f * wins / reached:0}%)";
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
                        "distancia;vel_max;dobra_em;abates;batidas;vida_no_fim;poder_usado;poder_sobrando;" +
                        "renovacoes;recargas_pegas;segundos_de_efeito;cargas_fim;pico_passiva;" +
                        "quadros_fora;hash_pista;segundos_acima_420;batidas_acima_420;poder_na_carga;" +
                        "dobra_com_efeito;recargas_perdidas;colisoes_por_trecho;" +
                        "itens_vazios;maior_tempo_na_faixa;vezes_7s_na_faixa;tiros;trocas_de_faixa");

        foreach (var r in results)
            text.AppendLine(CsvLine(r));

        return text.ToString();
    }

    static string CsvLine(RaceSimulation.Result r) =>
        string.Join(";",
            r.persona, r.personaRank, r.shipName, r.shipLevel, r.levelName,
            DifficultyName(r.difficulty), r.seed, r.outcome,
            Comma(r.seconds), Comma(r.distance), RaceSpeed.Shown(r.topSpeed),
            r.warpReachedAt >= 0f ? Comma(r.warpReachedAt) : string.Empty,
            r.kills, r.crashes, Comma(r.healthLeft), r.powerUses,
            r.hasPower ? (r.diedWithPowerReady ? "1" : "0") : string.Empty,
            r.powerRenewals, r.powerRecharges, Comma(r.effectSeconds), r.conditionalCharges, r.passivePeak,
            r.offStepFrames, r.trackHash, Comma(r.secondsAtHighSpeed), r.crashesAtHighSpeed,
            r.powerUsesWhileCharging, r.reachedWarpWithEffect ? "1" : "0", r.wastedRecharges, r.sweepHits,
            r.emptyModifierTurns, Comma(r.longestInLane), r.longStaysInLane, r.shots, r.laneChanges);

    static string Comma(float value) => value.ToString("0.##", Br);
}
