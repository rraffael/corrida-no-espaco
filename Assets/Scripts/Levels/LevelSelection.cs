using UnityEngine;

/// <summary>
/// Qual fase e qual dificuldade a próxima corrida usa. O menu escolhe, a cena de
/// jogo lê. Estático porque precisa atravessar o carregamento de cena, que é
/// justamente onde um componente morreria.
/// </summary>
public static class LevelSelection
{
    static LevelDefinition level;

    public static Difficulty Difficulty { get; private set; } = Difficulty.Facil;

    /// <summary>
    /// A fase escolhida. Abrir a Game.unity direto no Editor, sem passar pelo
    /// menu, cai na primeira fase do catálogo — dá para testar sem navegar.
    /// </summary>
    public static LevelDefinition Level
    {
        get
        {
            if (level != null)
                return level;

            var catalog = LevelCatalog.Load();
            if (catalog == null)
                return null;

            level = catalog.LevelAt(1);
            if (level == null && catalog.levels.Length > 0)
                level = catalog.levels[0];

            return level;
        }
    }

    public static void Choose(LevelDefinition chosen, Difficulty difficulty)
    {
        level = chosen;
        Difficulty = difficulty;
    }

    public static DifficultySettings Settings()
    {
        var catalog = LevelCatalog.Load();
        return catalog != null
            ? catalog.SettingsFor(Difficulty)
            : new DifficultySettings { difficulty = Difficulty };
    }
}
