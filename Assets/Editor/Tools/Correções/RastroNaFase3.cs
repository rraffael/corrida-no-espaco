using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Põe o Rastro na Fase 3, ao lado do Casulo *(pedido do Raffael, 18/10/2026)*:
/// mesma janela de chegada (12 a 20 s) e peso 1, um pouco abaixo do Casulo.
///
/// **Conserto pontual, e não montagem:** a ficha da fase é do Raffael, e o
/// <see cref="LevelSetup"/> não reescreve ficha que já existe. Se isto ficasse
/// no Montar para sempre, tirar o Rastro da fase à mão seria desfeito na
/// montagem seguinte. Roda uma vez, não duplica, e sai do projeto depois.
/// </summary>
static class RastroNaFase3
{
    const string LevelPath = "Assets/Levels/Fase3.asset";
    const string RastroPath = "Assets/Levels/Obstaculos/Obstaculo4-Rastro.asset";

    [MenuItem(ProjectTools.RastroFase3Item, false, 200)]
    internal static void Run()
    {
        var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
        var rastro = AssetDatabase.LoadAssetAtPath<ObstacleStats>(RastroPath);
        if (level == null || rastro == null)
        {
            Debug.LogError($"[Correções] Não achei {LevelPath} ou {RastroPath}.");
            return;
        }

        var schedules = new List<ObstacleSchedule>(level.obstacles ?? new ObstacleSchedule[0]);
        if (schedules.Exists(s => s != null && s.stats == rastro))
        {
            Debug.Log("[Correções] O Rastro já está na Fase 3. Nada a fazer.");
            return;
        }

        schedules.Add(new ObstacleSchedule
        {
            stats = rastro,
            earliestSeconds = 12f,
            latestSeconds = 20f,
            weight = 1f,
        });

        level.obstacles = schedules.ToArray();
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();

        Debug.Log("[Correções] Rastro na Fase 3: chega entre 12 e 20 s, como o Casulo, com peso 1.", level);
    }
}
