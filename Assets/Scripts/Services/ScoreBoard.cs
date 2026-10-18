using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Os placares do jogo: **um por fase e dificuldade**, mais o da fase sem fim.
///
/// Até 17/10/2026 só a sem fim tinha placar — as fases de progressão "ensinavam
/// o jogo e o prêmio era a próxima" (decisão de 06/08/2026). O Raffael reabriu
/// isso com o retorno do teste interno: cada fase ganha o seu placar, visto
/// pelo troféu na tela de seleção e ao concluir a fase.
///
/// **Duas regras de ordem:**
/// - **Sem fim:** distância percorrida, maior é melhor.
/// - **Fase:** tempo até a dobra, menor é melhor; empate no tempo, ganha quem
///   destruiu mais obstáculos.
///
/// Empate completo não passa ninguém: quem chegou antes fica na frente. Por isso
/// a entrada nova é **inserida** no lugar, e não ordenada junto — o
/// <c>List.Sort</c> não garante a ordem de empatados.
///
/// **Hoje é local**: cada placar guarda as corridas deste aparelho, até
/// <see cref="MaxEntries"/>. Quando o Play Games entrar (Fase 7), é aqui que o
/// placar online passa a conversar — e aí "posição" passa a ser entre jogadores,
/// com milhares de linhas, que é o caso que o <see cref="LeaderboardLayout"/> já
/// sabe desenhar.
/// </summary>
public static class ScoreBoard
{
    /// <summary>Quantas linhas um placar local guarda. É o mesmo topo que a tela mostra inteiro.</summary>
    public const int MaxEntries = LeaderboardLayout.TopRows;

    /// <summary>O placar da fase sem fim. Mora no campo <c>records</c> do salvamento, que é o de sempre.</summary>
    public const string EndlessBoardId = "sem-fim";

    /// <summary>
    /// A escala da velocidade, e é **a mesma de propósito**: é ela que faz a
    /// conta fechar na cabeça do jogador. Correndo a 160 un/s por um segundo, o
    /// placar sobe 160 un — velocidade e distância são a mesma unidade, uma por
    /// segundo e a outra acumulada. Se as duas escalas pudessem divergir, o
    /// painel passaria a mentir sobre a relação entre elas.
    /// </summary>
    public const float DisplayScale = RaceSpeed.DisplayScale;

    /// <summary>A distância é a velocidade acumulada, então herda a unidade dela sem o "por segundo".</summary>
    public const string DisplayUnit = "un";

    const string DefaultName = "Piloto";

    /// <summary>
    /// Uma corrida no placar. Os campos de nave, tempo e abates entraram em
    /// 17/10/2026: linha antiga da sem fim abre com eles zerados, e a tela
    /// mostra um traço no lugar da nave.
    /// </summary>
    [Serializable]
    public class Entry
    {
        public string name;
        public float distance;
        public float time;
        public int kills;

        /// <summary>Nome do asset da nave, e não o de tela — o mesmo motivo do <see cref="ShipSelection"/>.</summary>
        public string ship;
        public int shipLevel;
    }

    /// <summary>Onde a corrida caiu. <see cref="Rank"/> 0 é "não entrou no placar".</summary>
    public readonly struct Placement
    {
        public readonly int Rank;

        /// <summary>
        /// De onde a subida começa na tela. Local, é o fim da tabela: a corrida
        /// é uma linha nova, que entra por baixo. Online será a posição anterior
        /// do jogador.
        /// </summary>
        public readonly int FromRank;

        public Placement(int rank, int fromRank)
        {
            Rank = rank;
            FromRank = fromRank;
        }
    }

    /// <summary>Nome usado da última vez, para o campo já vir preenchido.</summary>
    public static string LastName
    {
        get => SaveGame.Data.lastName;
        set => SaveGame.Data.lastName = value ?? string.Empty;
    }

    /// <summary>
    /// Quem é "você" no placar. Até o apelido da Play Store chegar, é o último
    /// nome digitado na sem fim.
    /// </summary>
    public static string PilotName => string.IsNullOrWhiteSpace(LastName) ? DefaultName : LastName.Trim();

    public static string BoardId(LevelDefinition level, Difficulty difficulty)
    {
        if (level == null || level.endless)
            return EndlessBoardId;

        // Pelo nome do asset: o nome de tela da fase é texto que pode mudar, e o
        // placar não pode se perder por causa disso.
        return $"{level.name}.{difficulty}";
    }

    public static bool IsDistanceBoard(string boardId) => boardId == EndlessBoardId;

    /// <summary>O placar, do melhor para o pior. Placar que ainda não existe volta vazio.</summary>
    public static IReadOnlyList<Entry> Board(string boardId) => Entries(boardId, create: false);

    /// <summary>A tabela da sem fim — a que a transição antes da corrida mostra.</summary>
    public static IReadOnlyList<Entry> All() => Board(EndlessBoardId);

    /// <summary>A melhor linha do piloto neste placar, ou nula se ele ainda não correu aqui.</summary>
    public static Entry BestOf(string boardId, string pilot)
    {
        foreach (var entry in Board(boardId))
        {
            if (entry != null && entry.name == pilot)
                return entry;
        }

        return null;
    }

    /// <summary>
    /// Guarda a corrida, preenchendo nave e nível com o que está escolhido agora.
    /// Corrida que fica abaixo do topo não é guardada.
    /// </summary>
    public static Placement Submit(string boardId, Entry entry)
    {
        if (entry == null)
            return default;

        bool byDistance = IsDistanceBoard(boardId);
        if (byDistance ? entry.distance <= 0f : entry.time <= 0f)
            return default;

        entry.name = string.IsNullOrWhiteSpace(entry.name) ? DefaultName : entry.name.Trim();

        var ship = ShipSelection.Ship;
        if (ship != null && string.IsNullOrEmpty(entry.ship))
        {
            entry.ship = ship.name;
            entry.shipLevel = ShipProgress.Level(ship);
        }

        var entries = Entries(boardId, create: true);
        int index = InsertionIndex(entries, entry, byDistance);
        if (index >= MaxEntries)
            return new Placement(0, entries.Count + 1);

        int from = entries.Count + 1;
        entries.Insert(index, entry);

        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        SaveGame.Save();
        return new Placement(index + 1, from);
    }

    /// <summary>
    /// Onde a entrada nova entra: antes da primeira que ela vence **de fato**.
    /// Empate fica atrás — quem marcou antes segura a posição.
    /// </summary>
    public static int InsertionIndex(IReadOnlyList<Entry> entries, Entry entry, bool byDistance)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (Beats(entry, entries[i], byDistance))
                return i;
        }

        return entries.Count;
    }

    /// <summary><paramref name="a"/> fica na frente de <paramref name="b"/>.</summary>
    public static bool Beats(Entry a, Entry b, bool byDistance)
    {
        if (b == null)
            return true;

        if (byDistance)
            return a.distance > b.distance;

        if (!Mathf.Approximately(a.time, b.time))
            return a.time < b.time;

        return a.kills > b.kills;
    }

    /// <summary>Apaga todos os placares, o da sem fim junto.</summary>
    public static void Clear()
    {
        SaveGame.Data.records.Clear();
        SaveGame.Data.boards.Clear();
        SaveGame.Save();
    }

    static List<Entry> Entries(string boardId, bool create)
    {
        var data = SaveGame.Data;
        if (boardId == EndlessBoardId)
            return data.records;

        foreach (var board in data.boards)
        {
            if (board.id == boardId)
                return board.entries;
        }

        if (!create)
            return new List<Entry>();

        var created = new BoardData { id = boardId };
        data.boards.Add(created);
        return created.entries;
    }

    /// <summary>Quantas letras do nome da nave cabem na coluna do placar.</summary>
    public const int ShipNameLength = 11;

    /// <summary>
    /// Nome de tela da nave da linha, com o nível — "Faísca Nv 12". Nome longo
    /// é cortado com reticências: a coluna da nave tem pouco mais de um terço
    /// da linha, e "Espreitador Espacial (Nv 40)" invadia o tempo ao lado.
    /// </summary>
    public static string DescribeShip(Entry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.ship))
            return "—";

        var catalog = ShipCatalog.Load();
        var ship = catalog != null ? catalog.ByName(entry.ship) : null;
        string name = ship != null ? ship.displayName : entry.ship;

        if (name.Length > ShipNameLength)
            name = name.Substring(0, ShipNameLength - 1).TrimEnd() + "…";

        return entry.shipLevel > 0 ? $"{name} Nv {entry.shipLevel}" : name;
    }

    /// <summary>Distância como o jogador lê: "4.800 un".</summary>
    public static string FormatDistance(float distance)
    {
        int shown = Mathf.RoundToInt(Mathf.Max(0f, distance) * DisplayScale);
        return shown.ToString("n0") + " " + DisplayUnit;
    }

    /// <summary>
    /// Tempo em m:ss.cc — o formato de cronômetro que se lê de relance. É a
    /// pontuação das fases, e o painel de fase concluída mostra em destaque.
    /// </summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return $"{minutes}:{rest:00.00}";
    }
}
