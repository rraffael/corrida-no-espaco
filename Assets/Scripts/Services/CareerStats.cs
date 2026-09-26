using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Os números de carreira do jogador, como vão para o salvamento.</summary>
[Serializable]
public class CareerData
{
    /// <summary>Corridas que chegaram ao fim — vencidas, perdidas ou a sem fim encerrada.</summary>
    public int races;
    public int wins;
    public int defeats;
    public int endlessRuns;

    /// <summary>Obstáculos destruídos a tiro.</summary>
    public int kills;

    /// <summary>Batidas de frente — não conta raspão de estilhaço.</summary>
    public int crashes;

    /// <summary>Distância somada de todas as corridas, em unidades de mundo (a tela multiplica por 10).</summary>
    public float distance;

    /// <summary>Maior distância numa corrida da fase sem fim.</summary>
    public float bestEndlessDistance;

    /// <summary>Segundos de corrida, sem contar pausa.</summary>
    public float raceSeconds;

    /// <summary>Vezes que o jogador evoluiu uma nave, pagando.</summary>
    public int shipLevelUps;

    /// <summary>Os mesmos números, separados pela nave com que a corrida foi jogada.</summary>
    public List<ShipCareerEntry> ships = new List<ShipCareerEntry>();
}

/// <summary>A carreira de uma nave só. Pelo nome do asset, como tudo que é por nave no salvamento.</summary>
[Serializable]
public class ShipCareerEntry
{
    public string ship;
    public int races;
    public int wins;
    public int kills;
    public float distance;
    public int levelUps;
}

/// <summary>
/// As **estatísticas de carreira** *(26/09/2026)*: o que o jogador já fez, somado
/// de todas as partidas. É a base que as conquistas e a moeda da Parte 8 vão ler
/// — "destrua 500 obstáculos", "complete 10 dobras", "ganhe moeda por distância".
///
/// **Abate e batida acumulam em memória durante a corrida** e vão para o disco
/// uma vez, no fim: gravar a cada tiro seria escrever o arquivo dezenas de vezes
/// por minuto, num celular, por um número que só importa quando a corrida acaba.
///
/// Corrida abandonada pelo menu de pausa **não conta** — nem ela, nem os abates
/// dela. É o que "corrida" quer dizer para uma conquista: chegar ao fim.
/// </summary>
public static class CareerStats
{
    public enum Outcome { Win, Defeat, Endless }

    static int pendingKills;
    static int pendingCrashes;

    public static CareerData Data => SaveGame.Data.career;

    /// <summary>A corrida começou: o que ficou pendurado de uma abandonada não conta.</summary>
    public static void RaceStarted()
    {
        pendingKills = 0;
        pendingCrashes = 0;
    }

    public static void AddKill() => pendingKills++;

    public static void AddCrash() => pendingCrashes++;

    /// <summary>A corrida chegou ao fim. Soma tudo e grava uma vez.</summary>
    public static void RaceEnded(Outcome outcome, float distance, float seconds)
    {
        var data = Data;

        data.races++;
        data.kills += pendingKills;
        data.crashes += pendingCrashes;
        data.distance += Mathf.Max(0f, distance);
        data.raceSeconds += Mathf.Max(0f, seconds);

        switch (outcome)
        {
            case Outcome.Win:
                data.wins++;
                break;
            case Outcome.Defeat:
                data.defeats++;
                break;
            default:
                data.endlessRuns++;
                data.bestEndlessDistance = Mathf.Max(data.bestEndlessDistance, distance);
                break;
        }

        var ship = ShipSelection.Ship;
        if (ship != null)
        {
            var entry = For(ship, create: true);
            entry.races++;
            entry.kills += pendingKills;
            entry.distance += Mathf.Max(0f, distance);
            if (outcome == Outcome.Win)
                entry.wins++;
        }

        pendingKills = 0;
        pendingCrashes = 0;

        RaceFinished?.Invoke(outcome);
        SaveGame.Save();
    }

    /// <summary>
    /// O jogador evoluiu uma nave, pagando. Conta, confere as conquistas na hora
    /// — a de "evolua pela primeira vez" tem de aparecer ali, e não só depois da
    /// próxima corrida — e grava.
    /// </summary>
    public static void ShipEvolved(ShipDefinition ship)
    {
        Data.shipLevelUps++;

        var entry = For(ship, create: true);
        if (entry != null)
            entry.levelUps++;

        Updated?.Invoke();
        SaveGame.Save();
    }

    /// <summary>
    /// Um número de carreira mudou fora da corrida. As conquistas conferem aqui;
    /// antes de gravar, para o que elas mudarem ir no mesmo arquivo.
    /// </summary>
    public static event Action Updated;

    /// <summary>A carreira de uma nave. Nula quando ela nunca terminou corrida e não se pediu para criar.</summary>
    public static ShipCareerEntry For(ShipDefinition ship, bool create = false)
    {
        if (ship == null)
            return null;

        var ships = Data.ships;
        for (int i = 0; i < ships.Count; i++)
        {
            if (ships[i] != null && ships[i].ship == ship.name)
                return ships[i];
        }

        if (!create)
            return null;

        var entry = new ShipCareerEntry { ship = ship.name };
        ships.Add(entry);
        return entry;
    }

    /// <summary>
    /// Avisa quem precisa reagir ao fim de uma corrida já somada — as conquistas
    /// conferem o progresso aqui. Antes de gravar, para o que elas mudarem ir no
    /// mesmo arquivo.
    /// </summary>
    public static event Action<Outcome> RaceFinished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        pendingKills = 0;
        pendingCrashes = 0;
        RaceFinished = null;
        Updated = null;
    }
}
