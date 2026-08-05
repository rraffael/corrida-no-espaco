using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tabela de recordes. A pontuação é o **tempo até entrar em dobra**, então
/// menor é melhor — a lista fica em ordem crescente.
///
/// Guardado em PlayerPrefs: é do aparelho, sobrevive a fechar o jogo, e não
/// exige servidor nem login. Quando (e se) o Play Games entrar na Fase 7, este
/// é o lugar por onde um leaderboard online passaria a conversar.
/// </summary>
public static class ScoreBoard
{
    const string TableKey = "corrida.recordes";
    const string LastNameKey = "corrida.ultimo-nome";
    const int MaxEntries = 10;

    [Serializable]
    public class Entry
    {
        public string name;
        public float seconds;
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

    /// <summary>Recordes do melhor para o pior. Nunca devolve nulo.</summary>
    public static IReadOnlyList<Entry> All() => Load().entries;

    /// <summary>
    /// Guarda um tempo. Devolve a posição na tabela (0 é o primeiro lugar), ou
    /// -1 se não foi bom o bastante para entrar.
    /// </summary>
    public static int Submit(string name, float seconds)
    {
        if (seconds <= 0f)
            return -1;

        var table = Load();
        var entry = new Entry
        {
            name = string.IsNullOrWhiteSpace(name) ? "Piloto" : name.Trim(),
            seconds = seconds,
        };

        table.entries.Add(entry);
        table.entries.Sort((a, b) => a.seconds.CompareTo(b.seconds));

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

    /// <summary>Tempo em m:ss.cc — o formato de cronômetro que se lê de relance.</summary>
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
