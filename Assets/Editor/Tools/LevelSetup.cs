using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cria as fichas de obstáculo, as fichas de fase e o catálogo. Depois disto,
/// mexer no jogo é editar arquivo de dados no Inspector: fase nova, obstáculo
/// novo e ajuste de dificuldade não passam mais por código.
///
/// Roda sem apagar o que já existe: se você tiver ajustado números à mão, rodar
/// de novo só preenche o que estiver faltando.
/// </summary>
static class LevelSetup
{
    const string LevelFolder = "Assets/Levels";
    const string ObstacleFolder = LevelFolder + "/Obstaculos";
    const string ResourcesFolder = "Assets/Resources";
    const string CatalogPath = ResourcesFolder + "/LevelCatalog.asset";

    [MenuItem(ProjectTools.LevelsItem, false, 106)]
    internal static void Setup()
    {
        Directory.CreateDirectory(ObstacleFolder);
        Directory.CreateDirectory(ResourcesFolder);
        AssetDatabase.Refresh();

        var pixel = PlaceholderArt.Pixel();

        var basico = GetOrCreateObstacle("Obstaculo1-Basico", stats =>
        {
            stats.displayName = "Detrito";
            stats.color = new Color(1f, 0.55f, 0.4f, 1f);
            stats.laneSpan = 1;
            stats.height = 0.9f;
            stats.maxHealth = 50f;
            stats.contactDamage = 20f;
            // Peso relativo, e não velocidade: quem converte peso em velocidade é
            // a ficha da nave (ShipStats.KillSpeedGain).
            stats.killWeight = 1.5f;
            stats.speedPenaltyOnCrash = 2f;
        }, pixel);

        var largo = GetOrCreateObstacle("Obstaculo2-Largo", stats =>
        {
            stats.displayName = "Barcaça";
            stats.color = new Color(0.7f, 0.6f, 1f, 1f);
            // Duas faixas: num corredor de 3, tranca dois terços da pista.
            stats.laneSpan = 2;
            stats.height = 1.1f;
            stats.maxHealth = 140f;
            stats.contactDamage = 30f;
            stats.killWeight = 2.5f;
            stats.speedPenaltyOnCrash = 3f;
            stats.crashDistance = 0.8f;
            stats.hitDistance = 0.6f;
        }, pixel);

        var estilhacos = GetOrCreateObstacle("Obstaculo3-Estilhacos", stats =>
        {
            stats.displayName = "Casulo";
            stats.color = new Color(1f, 0.85f, 0.35f, 1f);
            stats.laneSpan = 1;
            stats.height = 0.9f;
            stats.maxHealth = 70f;
            stats.contactDamage = 20f;
            stats.killWeight = 2f;
            stats.speedPenaltyOnCrash = 2f;
            // O que dá sentido a este tipo: destruir de frente cobra caro, e
            // obriga a sair da faixa em vez de segurar o gatilho parado.
            stats.shrapnelOnDeath = true;
            stats.shrapnelDamage = 15f;
            stats.shrapnelSpeed = 6f;
            stats.shrapnelCount = 1;
        }, pixel);

        var fase1 = GetOrCreateLevel("Fase1", level =>
        {
            level.displayName = "Fase 1 — Cinturão";
            level.order = 1;
            level.warpSpeed = 15f;
            level.warpChargeSeconds = 5f;
            level.startInterval = 1.4f;
            level.endInterval = 1.1f;
            level.rampSeconds = 60f;
            level.obstacles = new[]
            {
                Schedule(basico, 0f, 0f, 1f),
            };
        });

        var fase2 = GetOrCreateLevel("Fase2", level =>
        {
            level.displayName = "Fase 2 — Comboio";
            level.order = 2;
            level.warpSpeed = 15f;
            level.warpChargeSeconds = 6f;
            level.startInterval = 1.3f;
            level.endInterval = 0.95f;
            level.rampSeconds = 60f;
            level.obstacles = new[]
            {
                Schedule(basico, 0f, 0f, 2f),
                Schedule(largo, 5f, 10f, 1f),
            };
        });

        var fase3 = GetOrCreateLevel("Fase3", level =>
        {
            level.displayName = "Fase 3 — Ninho";
            level.order = 3;
            level.warpSpeed = 16f;
            level.warpChargeSeconds = 7f;
            level.startInterval = 1.2f;
            level.endInterval = 0.85f;
            level.rampSeconds = 60f;
            level.obstacles = new[]
            {
                Schedule(basico, 0f, 0f, 2f),
                Schedule(largo, 5f, 10f, 1f),
                Schedule(estilhacos, 12f, 20f, 1.5f),
            };
        });

        var infinita = GetOrCreateLevel("FaseInfinita", level =>
        {
            level.displayName = "Sem fim";
            level.order = 99;
            level.endless = true;
            level.warpSpeed = 999f;
            level.warpChargeSeconds = 0f;
            level.startInterval = 1.4f;
            level.endInterval = 0.55f;
            // Rampa longa: a fase do leaderboard tem de premiar quem aguenta.
            level.rampSeconds = 180f;
            level.obstacles = new[]
            {
                Schedule(basico, 0f, 0f, 2f),
                Schedule(largo, 10f, 20f, 1f),
                Schedule(estilhacos, 25f, 40f, 1.5f),
            };
        });

        var catalog = GetOrCreateCatalog();
        catalog.levels = new[] { fase1, fase2, fase3 };
        catalog.endlessLevel = infinita;

        if (catalog.difficulties == null || catalog.difficulties.Length == 0)
        {
            catalog.difficulties = new[]
            {
                new DifficultySettings
                {
                    difficulty = Difficulty.Facil,
                    displayName = "Fácil",
                },
                new DifficultySettings
                {
                    difficulty = Difficulty.Normal,
                    displayName = "Normal",
                    warpSpeedBonus = 1.5f,
                    intervalFactor = 0.85f,
                    obstacleHealthFactor = 1.3f,
                    obstacleDamageFactor = 1.2f,
                },
                new DifficultySettings
                {
                    difficulty = Difficulty.Dificil,
                    displayName = "Difícil",
                    warpSpeedBonus = 3f,
                    intervalFactor = 0.7f,
                    obstacleHealthFactor = 1.7f,
                    obstacleDamageFactor = 1.5f,
                },
            };
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ProjectTools.MarkRun(ProjectTools.LevelsId);

        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);

        Debug.Log(
            "[Fases] Catálogo em " + CatalogPath + " com 3 fases, a fase sem fim e as três " +
            "dificuldades. As fichas de obstáculo estão em " + ObstacleFolder + " — " +
            "daqui para a frente, obstáculo novo e fase nova são arquivo de dados, não código.",
            catalog);
    }

    static ObstacleSchedule Schedule(ObstacleStats stats, float earliest, float latest, float weight) =>
        new ObstacleSchedule
        {
            stats = stats,
            earliestSeconds = earliest,
            latestSeconds = latest,
            weight = weight,
        };

    static ObstacleStats GetOrCreateObstacle(string fileName, System.Action<ObstacleStats> fill, Sprite sprite)
    {
        string path = $"{ObstacleFolder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ObstacleStats>(path);
        if (existing != null)
        {
            // A arte pode ter sido regerada; o resto é do Raffael e fica como está.
            if (existing.sprite == null)
            {
                existing.sprite = sprite;
                EditorUtility.SetDirty(existing);
            }

            return existing;
        }

        var created = ScriptableObject.CreateInstance<ObstacleStats>();
        fill(created);
        created.sprite = sprite;

        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static LevelDefinition GetOrCreateLevel(string fileName, System.Action<LevelDefinition> fill)
    {
        string path = $"{LevelFolder}/{fileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
        if (existing != null)
            return existing;

        var created = ScriptableObject.CreateInstance<LevelDefinition>();
        fill(created);

        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    static LevelCatalog GetOrCreateCatalog()
    {
        var existing = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (existing != null)
            return existing;

        var created = ScriptableObject.CreateInstance<LevelCatalog>();
        AssetDatabase.CreateAsset(created, CatalogPath);
        return created;
    }
}
