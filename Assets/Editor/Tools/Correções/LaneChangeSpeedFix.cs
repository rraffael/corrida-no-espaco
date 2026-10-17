using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca de faixa **50% mais rápida**: de 12 para 18 unidades por segundo, na
/// nave da Game.unity *(pedido do Raffael, 17/10/2026)*.
///
/// É conserto pontual porque o número mora na cena, e nenhuma montagem escreve
/// nele — a padrão do <see cref="ShipLaneController"/> já é 18, mas a cena
/// guardou o 12 de antes. Depois de rodar e ser aprovado, sai do projeto.
///
/// Valor fixo, e não "vezes 1,5": rodar duas vezes não pode dar 27.
/// </summary>
static class LaneChangeSpeedFix
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const float Speed = 18f;

    [MenuItem(ProjectTools.LaneChangeSpeedFixItem, false, 300)]
    internal static void Run()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var lanes = Object.FindAnyObjectByType<ShipLaneController>(FindObjectsInactive.Include);
        if (lanes == null)
        {
            Debug.LogError("[Correção] Nenhum ShipLaneController na Game.unity.");
            return;
        }

        var serialized = new SerializedObject(lanes);
        var property = serialized.FindProperty("laneChangeSpeed");
        float before = property.floatValue;
        property.floatValue = Speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.LaneChangeSpeedFixId);

        Debug.Log($"[Correção] Troca de faixa: {before:0.#} → {Speed:0.#} u/s em {ScenePath}.", lanes);
    }

    static Scene OpenGameScene()
    {
        var current = SceneManager.GetActiveScene();
        if (current.path == ScenePath)
            return current;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }
}
