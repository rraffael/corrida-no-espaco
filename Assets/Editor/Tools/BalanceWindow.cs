using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// O **painel de balanceamento** *(pedido do Raffael, 26/09/2026)*: todas as
/// naves do catálogo nos níveis de patamar, com os atributos de cada um, e o
/// **tempo estimado até completar a dobra** em cada dificuldade.
///
/// Existe para a sessão de balancear ser olhar uma tabela, e não jogar 60
/// níveis nem fazer conta. Lê as fichas de verdade e se refaz sozinho quando
/// um número muda.
///
/// **É estimativa, não simulação.** Soma três trechos: do zero ao cruzeiro na
/// aceleração com o bônus de baixa velocidade, do cruzeiro até a dobra no
/// ganho passivo mais os abates por segundo escolhidos aqui, e a carga da
/// dobra. Não conta batida, raspão nem modificador pego — dá a ordem de
/// grandeza e a comparação entre naves e níveis; o veredito é o aparelho.
/// </summary>
class BalanceWindow : EditorWindow
{
    [MenuItem(ProjectTools.BalanceItem, false, 152)]
    static void Open() => GetWindow<BalanceWindow>("Balanceamento");

    // Os dois números do RaceSpeed da Game.unity, conferidos em 26/09/2026.
    // Ficam editáveis aqui porque moram na cena, e ler a cena daqui obrigaria a
    // abri-la — o que o Editor aberto do Raffael não pode sofrer por uma tabela.
    float passiveGainFactor = 0.04f;
    float lowSpeedBonus = 2f;

    float killsPerSecond = 0.5f;
    int levelIndex;
    Vector2 scroll;

    void OnGUI()
    {
        var ships = Ships();
        var levels = Levels(out var levelNames);
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelSetup.CatalogPath);

        if (ships.Count == 0 || levels.Count == 0 || catalog == null)
        {
            EditorGUILayout.HelpBox("Falta o catálogo de naves ou o de fases. Rode o Montar.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Estimativa até a dobra", EditorStyles.boldLabel);

        levelIndex = Mathf.Clamp(EditorGUILayout.Popup("Fase", levelIndex, levelNames), 0, levels.Count - 1);
        killsPerSecond = EditorGUILayout.Slider(
            new GUIContent("Abates por segundo", "Quantos obstáculos de peso 1 a nave destrói por segundo " +
                                                 "acima do cruzeiro. 0 = só desviando."),
            killsPerSecond, 0f, 2f);

        using (new EditorGUILayout.HorizontalScope())
        {
            passiveGainFactor = EditorGUILayout.FloatField(
                new GUIContent("Ganho passivo", "passiveGainFactor do RaceSpeed na Game.unity"), passiveGainFactor);
            lowSpeedBonus = EditorGUILayout.FloatField(
                new GUIContent("Bônus baixa veloc.", "lowSpeedAccelerationBonus do RaceSpeed na Game.unity"),
                lowSpeedBonus);
        }

        EditorGUILayout.HelpBox("Estimativa, não simulação: sem batida, raspão nem modificador. Serve para " +
                                "comparar naves e níveis — o veredito é o aparelho.", MessageType.None);

        var level = levels[levelIndex];
        var obstacles = Obstacles();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var ship in ships)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(ship.displayName, EditorStyles.boldLabel);
            Header(catalog);

            var model = ShipEvolution.Model(ship);
            int[] shipLevels = { 1, model.upgrade1Level, model.upgrade2Level, model.maxLevel };
            foreach (int shipLevel in shipLevels)
                Row(ship, shipLevel, level, catalog);

            ShotsTable(ship, shipLevels, obstacles, catalog);
        }

        EditorGUILayout.EndScrollView();
    }

    static readonly float[] Widths = { 44, 52, 58, 50, 66, 74, 86, 70 };

    void Header(LevelCatalog catalog)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            string[] titles = { "Nível", "Vida", "Defesa", "Dano", "Cadência", "Cruzeiro", "Aceleração", "Abate" };
            for (int i = 0; i < titles.Length; i++)
                GUILayout.Label(titles[i], EditorStyles.miniBoldLabel, GUILayout.Width(Widths[i]));

            foreach (var settings in catalog.difficulties)
                GUILayout.Label($"Dobra {settings.displayName}", EditorStyles.miniBoldLabel, GUILayout.Width(96));
        }
    }

    void Row(ShipDefinition ship, int shipLevel, LevelDefinition level, LevelCatalog catalog)
    {
        float V(ShipStat stat) => ShipEvolution.Value(ship, stat, shipLevel);

        using (new EditorGUILayout.HorizontalScope())
        {
            string[] cells =
            {
                shipLevel.ToString(),
                $"{V(ShipStat.MaxHealth):0.#}",
                $"{V(ShipStat.DefensePercent):0.#}%",
                $"{V(ShipStat.Damage):0.##}",
                $"{V(ShipStat.AttackSpeed):0.##}/s",
                $"{V(ShipStat.CruiseSpeed) * RaceSpeed.DisplayScale:0.#}",
                $"{V(ShipStat.Acceleration) * RaceSpeed.DisplayScale:0.#}",
                $"{V(ShipStat.KillSpeedGain) * RaceSpeed.DisplayScale:0.#}",
            };

            for (int i = 0; i < cells.Length; i++)
                GUILayout.Label(cells[i], GUILayout.Width(Widths[i]));

            foreach (var settings in catalog.difficulties)
                GUILayout.Label(WarpTime(ship, shipLevel, level, settings), GUILayout.Width(96));
        }
    }

    /// <summary>
    /// **Quantos tiros cada obstáculo do jogo leva**, por nível da nave e por
    /// dificuldade *(pedido do Raffael, 26/09/2026)*. A dificuldade multiplica a
    /// vida do obstáculo (<c>obstacleHealthFactor</c>), então o mesmo dano pode
    /// ser 2 tiros no Fácil e 3 no Difícil — é o que cada célula mostra, na
    /// ordem das dificuldades do catálogo.
    ///
    /// Com o dano da evolução e sem poder nenhum: o Reforço da Predadora, por
    /// exemplo, soma por cima disto.
    /// </summary>
    static void ShotsTable(ShipDefinition ship, int[] shipLevels, List<ObstacleStats> obstacles,
                           LevelCatalog catalog)
    {
        if (obstacles.Count == 0)
            return;

        var names = new List<string>();
        foreach (var settings in catalog.difficulties)
            names.Add(settings.displayName);

        EditorGUILayout.LabelField($"Tiros para destruir ({string.Join(" · ", names)})",
                                   EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Nível", EditorStyles.miniBoldLabel, GUILayout.Width(Widths[0]));
            foreach (var obstacle in obstacles)
            {
                GUILayout.Label($"{obstacle.displayName} ({obstacle.maxHealth:0})", EditorStyles.miniBoldLabel,
                                GUILayout.Width(120));
            }
        }

        foreach (int shipLevel in shipLevels)
        {
            float damage = ShipEvolution.Value(ship, ShipStat.Damage, shipLevel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(shipLevel.ToString(), GUILayout.Width(Widths[0]));

                foreach (var obstacle in obstacles)
                {
                    var shots = new List<string>();
                    foreach (var settings in catalog.difficulties)
                        shots.Add(ShotsToKill(obstacle.maxHealth * settings.obstacleHealthFactor, damage).ToString());

                    GUILayout.Label(string.Join(" · ", shots), GUILayout.Width(120));
                }
            }
        }
    }

    /// <summary>
    /// Tiros até a vida chegar a zero. A folga tira o erro de ponto flutuante:
    /// 50 de vida e 25 de dano são 2 tiros, e não 3 por um resto de 0,0000001.
    /// </summary>
    static int ShotsToKill(float health, float damage) =>
        damage <= 0f ? 0 : Mathf.Max(1, Mathf.CeilToInt(health / damage - 0.0001f));

    /// <summary>
    /// Todos os tipos de obstáculo do jogo, e não só os da fase escolhida: a
    /// pergunta é quanto cada nave aguenta contra cada um, esteja ele onde estiver.
    /// Na ordem do nome do arquivo, que é a ordem em que foram criados.
    /// </summary>
    static List<ObstacleStats> Obstacles()
    {
        var obstacles = new List<ObstacleStats>();
        foreach (string guid in AssetDatabase.FindAssets("t:ObstacleStats"))
        {
            var stats = AssetDatabase.LoadAssetAtPath<ObstacleStats>(AssetDatabase.GUIDToAssetPath(guid));
            if (stats != null)
                obstacles.Add(stats);
        }

        obstacles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return obstacles;
    }

    /// <summary>
    /// Segundos da largada até a dobra completa: subir ao cruzeiro, sair do
    /// cruzeiro até o limiar, e segurar a carga.
    /// </summary>
    string WarpTime(ShipDefinition ship, int shipLevel, LevelDefinition level, DifficultySettings settings)
    {
        if (level.endless || level.warpChargeSeconds <= 0f)
            return "sem dobra";

        float cruise = ShipEvolution.Value(ship, ShipStat.CruiseSpeed, shipLevel);
        float acceleration = ShipEvolution.Value(ship, ShipStat.Acceleration, shipLevel);
        float killGain = ShipEvolution.Value(ship, ShipStat.KillSpeedGain, shipLevel);
        float warp = level.warpSpeed + settings.warpSpeedBonus;
        float charge = level.warpChargeSeconds + settings.warpChargeBonus;

        if (cruise >= warp)
            return "cruzeiro ≥ dobra!";

        // Abaixo do cruzeiro o bônus de baixa velocidade cai do cheio a zero
        // conforme a nave sobe; a média dele é metade.
        float toCruise = cruise / (acceleration * (1f + lowSpeedBonus * 0.5f));

        float rate = acceleration * passiveGainFactor + killsPerSecond * killGain;
        if (rate <= 0f)
            return "não chega";

        float toWarp = (warp - cruise) / rate;
        return $"{toCruise + toWarp + charge:0} s";
    }

    static List<ShipDefinition> Ships()
    {
        var ships = new List<ShipDefinition>();
        var catalog = AssetDatabase.LoadAssetAtPath<ShipCatalog>(ShipSetup.CatalogPath);
        if (catalog == null)
            return ships;

        foreach (var ship in catalog.ships)
        {
            if (ship != null)
                ships.Add(ship);
        }

        return ships;
    }

    static List<LevelDefinition> Levels(out string[] names)
    {
        var levels = new List<LevelDefinition>();
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelSetup.CatalogPath);
        if (catalog != null)
        {
            foreach (var level in catalog.levels)
            {
                if (level != null)
                    levels.Add(level);
            }
        }

        names = new string[levels.Count];
        for (int i = 0; i < levels.Count; i++)
            names[i] = levels[i].displayName;

        return levels;
    }

    // Número mudou numa ficha: a tabela se refaz sem precisar clicar nela.
    void OnInspectorUpdate() => Repaint();
}
