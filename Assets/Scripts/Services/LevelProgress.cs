using System;
using UnityEngine;

/// <summary>
/// O que já foi vencido. A progressão é uma **corrente única**, decidida pelo
/// Raffael em 06/08/2026: Fácil 1→2→3, depois Normal 1→2→3, depois Difícil
/// 1→2→3. A fase sem fim fica fora da corrente e aberta desde o começo.
///
/// Por ser corrente, e não uma grade de nove quadrados independentes, basta
/// guardar **quantos degraus já caíram** — um int em PlayerPrefs. Sem lista, sem
/// serialização, sem migração quando entrar uma quarta fase: o degrau é
/// calculado a partir do catálogo, então mudar o número de fases não invalida o
/// que o jogador já tem.
/// </summary>
public static class LevelProgress
{
    const string ClearedKey = "corrida.degraus-vencidos";

    /// <summary>Quantas dificuldades a corrente percorre.</summary>
    public const int DifficultyCount = 3;

    /// <summary>Degraus vencidos. Também é o índice do primeiro degrau ainda trancado.</summary>
    public static int StagesCleared
    {
        get => PlayerPrefs.GetInt(ClearedKey, 0);
        private set
        {
            PlayerPrefs.SetInt(ClearedKey, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Tamanho da corrente: todas as fases numeradas vezes as dificuldades.</summary>
    public static int TotalStages(LevelCatalog catalog) =>
        catalog == null || catalog.levels == null ? 0 : catalog.levels.Length * DifficultyCount;

    /// <summary>
    /// Posição do par fase+dificuldade na corrente. <c>-1</c> para a fase sem fim
    /// e para qualquer fase que não esteja no catálogo — nenhuma das duas é
    /// degrau, e nenhuma destrava nada.
    /// </summary>
    public static int StageIndex(LevelCatalog catalog, LevelDefinition level, Difficulty difficulty)
    {
        if (catalog == null || catalog.levels == null || level == null || level.endless)
            return -1;

        int position = Array.IndexOf(catalog.levels, level);
        if (position < 0)
            return -1;

        return (int)difficulty * catalog.levels.Length + position;
    }

    public static bool IsUnlocked(LevelCatalog catalog, LevelDefinition level, Difficulty difficulty)
    {
        if (level == null)
            return false;

        // A sem fim fica **aberta desde o começo** (decidido pelo Raffael em
        // 06/08/2026): ela não é degrau da corrente, é o modo avulso — quem quer
        // só sobreviver não precisa fechar nove fases antes de poder tentar.
        if (level.endless)
            return true;

        int index = StageIndex(catalog, level, difficulty);

        // Fase fora da corrente ficaria trancada para sempre, e ninguém saberia
        // por quê. Liberar é o modo de falhar que ainda deixa o jogo jogável.
        return index < 0 || index <= StagesCleared;
    }

    /// <summary>
    /// Registra a vitória. Devolve <c>true</c> só quando ela destravou um degrau
    /// novo — repetir uma fase já vencida não anda com a corrente.
    /// </summary>
    public static bool MarkCleared(LevelCatalog catalog, LevelDefinition level, Difficulty difficulty)
    {
        int index = StageIndex(catalog, level, difficulty);
        if (index < 0 || StagesCleared > index)
            return false;

        StagesCleared = index + 1;
        return true;
    }

    /// <summary>Nome do degrau na posição indicada — "Fase 2 — Comboio (Normal)".</summary>
    public static string DescribeStage(LevelCatalog catalog, int index)
    {
        int total = TotalStages(catalog);
        if (total == 0 || index < 0 || index >= total)
            return "a fase anterior";

        int count = catalog.levels.Length;
        var level = catalog.levels[index % count];
        var settings = catalog.SettingsFor((Difficulty)(index / count));

        return level != null
            ? $"{level.displayName} ({settings.displayName})"
            : "a fase anterior";
    }

    /// <summary>Zera a progressão. Existe para teste — nada no jogo chama.</summary>
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(ClearedKey);
        PlayerPrefs.Save();
    }
}
