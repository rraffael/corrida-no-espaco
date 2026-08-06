using System;
using UnityEngine;

public enum Difficulty
{
    Facil = 0,
    Normal = 1,
    Dificil = 2,
}

/// <summary>
/// O que a dificuldade muda. Ela **não** troca os obstáculos — isso é papel da
/// fase — e sim aperta os números dos mesmos obstáculos.
/// </summary>
[Serializable]
public class DifficultySettings
{
    public Difficulty difficulty = Difficulty.Facil;

    public string displayName = "Fácil";

    [Tooltip("Soma na velocidade de dobra pedida pela fase.")]
    public float warpSpeedBonus;

    [Tooltip("Multiplica o intervalo entre obstáculos. Menor que 1 = mais obstáculos.")]
    [Min(0.1f)] public float intervalFactor = 1f;

    [Min(0.1f)] public float obstacleHealthFactor = 1f;

    [Min(0.1f)] public float obstacleDamageFactor = 1f;
}

/// <summary>
/// Lista de todas as fases e dificuldades do jogo. Fica em
/// <c>Assets/Resources/</c> para o menu e a cena de jogo acharem sem
/// referência de cena.
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Corrida no Espaço/Catálogo de fases")]
public class LevelCatalog : ScriptableObject
{
    public const string ResourcePath = "LevelCatalog";

    [Tooltip("Fases numeradas, na ordem em que destravam.")]
    public LevelDefinition[] levels = Array.Empty<LevelDefinition>();

    [Tooltip("A fase sem fim, que alimenta o leaderboard. Destrava depois das outras.")]
    public LevelDefinition endlessLevel;

    public DifficultySettings[] difficulties = Array.Empty<DifficultySettings>();

    static LevelCatalog cached;

    /// <summary>Carrega de Resources, uma vez por sessão.</summary>
    public static LevelCatalog Load()
    {
        if (cached == null)
            cached = Resources.Load<LevelCatalog>(ResourcePath);

        if (cached == null)
            Debug.LogError($"[Fases] Não achei o catálogo em Resources/{ResourcePath}.");

        return cached;
    }

    public DifficultySettings SettingsFor(Difficulty difficulty)
    {
        foreach (var entry in difficulties)
        {
            if (entry.difficulty == difficulty)
                return entry;
        }

        // Sem ficha para esta dificuldade, os fatores neutros mantêm a fase
        // jogável em vez de derrubar a corrida.
        return new DifficultySettings { difficulty = difficulty };
    }

    public LevelDefinition LevelAt(int order)
    {
        foreach (var level in levels)
        {
            if (level != null && level.order == order)
                return level;
        }

        return null;
    }
}
