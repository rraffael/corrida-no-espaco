using UnityEditor;
using UnityEngine;

/// <summary>
/// Põe a escada do tempo de dobra nos assets que já existem: 2 s de carga nas
/// três fichas de fase e o bônus por dificuldade (0 · +0,75 · +1,5) no catálogo.
///
/// Existe porque o <see cref="LevelSetup"/> não sobrescreve asset criado antes —
/// ele só preenche o que falta, para não apagar número ajustado à mão. Os
/// valores daqui já são os padrões de lá, então fase nova nasce certa e este
/// conserto sai do projeto depois de rodar.
///
/// Conserto pontual de 01/09/2026 — ver ROADMAP, "A escada do tempo de dobra".
/// </summary>
static class WarpChargeLadderFix
{
    const float ChargeSeconds = 2f;

    [MenuItem(ProjectTools.WarpChargeLadderItem, false, 300)]
    internal static void Fix()
    {
        int levelsChanged = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition"))
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));

            // A fase sem fim não tem dobra, e carga 0 é o que diz isso. Mexer
            // nela daria à fase do leaderboard um fim que ela não deve ter.
            if (level == null || level.endless || level.warpChargeSeconds <= 0f)
                continue;

            if (Mathf.Approximately(level.warpChargeSeconds, ChargeSeconds))
                continue;

            Debug.Log($"[Dobra] {level.name}: carga {level.warpChargeSeconds:0.##}s → {ChargeSeconds:0.##}s.", level);
            level.warpChargeSeconds = ChargeSeconds;
            EditorUtility.SetDirty(level);
            levelsChanged++;
        }

        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelSetup.CatalogPath);
        if (catalog == null)
        {
            Debug.LogError($"[Dobra] Não achei o catálogo em {LevelSetup.CatalogPath}. " +
                           "Rode antes o 'Criar fases e fichas de obstáculo'.");
            return;
        }

        int difficultiesChanged = 0;
        difficultiesChanged += SetBonus(catalog, Difficulty.Facil, 0f);
        difficultiesChanged += SetBonus(catalog, Difficulty.Normal, 0.8f);
        difficultiesChanged += SetBonus(catalog, Difficulty.Dificil, 1.5f);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ProjectTools.MarkRun(ProjectTools.WarpChargeLadderId);

        Debug.Log(
            $"[Dobra] {levelsChanged} ficha(s) de fase em {ChargeSeconds:0.##}s e " +
            $"{difficultiesChanged} dificuldade(s) com o bônus novo. A dobra agora pede " +
            "2s no Fácil, 2,8s no Normal e 3,5s no Difícil.",
            catalog);
    }

    /// <summary>
    /// Só escreve a dificuldade que já existe: se ela sumiu do catálogo, quem
    /// recria é o LevelSetup, e inventar uma ficha aqui esconderia o buraco.
    /// </summary>
    static int SetBonus(LevelCatalog catalog, Difficulty difficulty, float bonus)
    {
        foreach (var entry in catalog.difficulties)
        {
            if (entry == null || entry.difficulty != difficulty)
                continue;

            if (Mathf.Approximately(entry.warpChargeBonus, bonus))
                return 0;

            entry.warpChargeBonus = bonus;
            return 1;
        }

        Debug.LogWarning($"[Dobra] O catálogo não tem a dificuldade {difficulty}; passei por cima.", catalog);
        return 0;
    }
}
