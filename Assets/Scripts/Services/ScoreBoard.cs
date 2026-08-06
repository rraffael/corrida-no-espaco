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
/// Guardado em PlayerPrefs: é do aparelho, sobrevive a fechar o jogo, e não
/// exige servidor nem login. Quando (e se) o Play Games entrar na Fase 7, este
/// é o lugar por onde um leaderboard online passaria a conversar.
/// </summary>
public static class ScoreBoard
{
    /// <summary>
    /// Chave nova de propósito. A tabela antiga guardava segundos em ordem
    /// crescente; reaproveitar a chave misturaria tempo com distância na mesma
    /// lista, e o jogador veria um recorde de "0,8 km" que na verdade era um
    /// tempo de 8 segundos. Quem tinha recorde antigo começa a nova tabela vazia.
    /// </summary>
    const string TableKey = "corrida.recordes-distancia";

    const string LastNameKey = "corrida.ultimo-nome";
    const int MaxEntries = 10;

    /// <summary>
    /// Quantos "km" cada unidade de mundo vale no painel. As unidades de mundo
    /// são poucas para dar sensação de distância percorrida; a conta do jogo não
    /// muda. É o mesmo espírito do <c>displayScale</c> do <see cref="SpeedHud"/>.
    /// </summary>
    public const float DisplayScale = 10f;

    [Serializable]
    public class Entry
    {
        public string name;
        public float distance;
    }

    [Serializable]
    class Table
    {
        public List<Entry> entries = new List<Entry>();
    }

    static Table cache;

    /// <summary>Nome usado da última vez, para o campo já vir preenchido.</summary>
    public static string LastName
    {
        get => PlayerPrefs.GetString(LastNameKey, string.Empty);
        set => PlayerPrefs.SetString(LastNameKey, value ?? string.Empty);
    }

    /// <summary>Recordes do melhor para o pior — do mais longe para o mais perto.</summary>
    public static IReadOnlyList<Entry> All() => Load().entries;

    /// <summary>
    /// Guarda uma distância. Devolve a posição na tabela (0 é o primeiro lugar),
    /// ou -1 se não foi longe o bastante para entrar.
    /// </summary>
    public static int Submit(string name, float distance)
    {
        if (distance <= 0f)
            return -1;

        var table = Load();
        var entry = new Entry
        {
            name = string.IsNullOrWhiteSpace(name) ? "Piloto" : name.Trim(),
            distance = distance,
        };

        table.entries.Add(entry);

        // Decrescente: aqui, maior é melhor.
        table.entries.Sort((a, b) => b.distance.CompareTo(a.distance));

        if (table.entries.Count > MaxEntries)
            table.entries.RemoveRange(MaxEntries, table.entries.Count - MaxEntries);

        Save(table);
        LastName = entry.name;
        PlayerPrefs.Save();

        return table.entries.IndexOf(entry);
    }

    public static void Clear()
    {
        cache = new Table();
        PlayerPrefs.DeleteKey(TableKey);
        PlayerPrefs.Save();
    }

    /// <summary>Distância como o jogador lê: "4.800 km".</summary>
    public static string FormatDistance(float distance)
    {
        int shown = Mathf.RoundToInt(Mathf.Max(0f, distance) * DisplayScale);
        return shown.ToString("n0") + " km";
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

    static Table Load()
    {
        if (cache != null)
            return cache;

        cache = new Table();

        string json = PlayerPrefs.GetString(TableKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return cache;

        try
        {
            var parsed = JsonUtility.FromJson<Table>(json);
            if (parsed?.entries != null)
                cache = parsed;
        }
        catch (Exception e)
        {
            // Tabela corrompida não pode impedir o jogo de abrir: no pior caso
            // o jogador perde os recordes, não a partida.
            Debug.LogWarning($"[Recordes] Não deu para ler a tabela ({e.Message}). Começando vazia.");
        }

        return cache;
    }

    static void Save(Table table)
    {
        cache = table;
        PlayerPrefs.SetString(TableKey, JsonUtility.ToJson(table));
    }
}
