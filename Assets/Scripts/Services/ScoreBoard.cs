using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tabela de recordes da **fase sem fim**, e só dela. As fases de progressão não
/// têm placar: elas servem para o jogador aprender o jogo e conhecer obstáculo
/// novo, e o prêmio delas é destravar a próxima (decidido pelo Raffael em
/// 06/08/2026).
///
/// A pontuação é a **distância percorrida** — velocidade integrada no tempo, e
/// não tempo puro. Maior é melhor, então a lista fica em ordem decrescente.
/// Isto inverte o sentido que a tabela tinha até 06/08/2026, quando a pontuação
/// era o tempo até a dobra e menor vencia.
///
/// Guardado no <see cref="SaveGame"/>: é do aparelho, sobrevive a fechar o jogo,
/// e não exige servidor nem login. Quando (e se) o Play Games entrar na Fase 7,
/// este é o lugar por onde um leaderboard online passaria a conversar.
/// </summary>
public static class ScoreBoard
{
    const int MaxEntries = 10;

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

    [Serializable]
    public class Entry
    {
        public string name;
        public float distance;
    }

    /// <summary>Nome usado da última vez, para o campo já vir preenchido.</summary>
    public static string LastName
    {
        get => SaveGame.Data.lastName;
        set => SaveGame.Data.lastName = value ?? string.Empty;
    }

    /// <summary>Recordes do melhor para o pior — do mais longe para o mais perto.</summary>
    public static IReadOnlyList<Entry> All() => SaveGame.Data.records;

    /// <summary>
    /// Guarda uma distância. Devolve a posição na tabela (0 é o primeiro lugar),
    /// ou -1 se não foi longe o bastante para entrar.
    /// </summary>
    public static int Submit(string name, float distance)
    {
        if (distance <= 0f)
            return -1;

        var entries = SaveGame.Data.records;
        var entry = new Entry
        {
            name = string.IsNullOrWhiteSpace(name) ? "Piloto" : name.Trim(),
            distance = distance,
        };

        entries.Add(entry);

        // Decrescente: aqui, maior é melhor.
        entries.Sort((a, b) => b.distance.CompareTo(a.distance));

        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        LastName = entry.name;
        SaveGame.Save();

        return entries.IndexOf(entry);
    }

    public static void Clear()
    {
        SaveGame.Data.records.Clear();
        SaveGame.Save();
    }

    /// <summary>Distância como o jogador lê: "4.800 un".</summary>
    public static string FormatDistance(float distance)
    {
        int shown = Mathf.RoundToInt(Mathf.Max(0f, distance) * DisplayScale);
        return shown.ToString("n0") + " " + DisplayUnit;
    }

    /// <summary>
    /// Tempo em m:ss.cc — o formato de cronômetro que se lê de relance. Não é
    /// mais pontuação, mas o painel de fase concluída ainda mostra quanto tempo
    /// a corrida levou.
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
