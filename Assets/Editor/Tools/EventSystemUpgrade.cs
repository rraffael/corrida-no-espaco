using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca o StandaloneInputModule (Input Manager antigo) pelo InputSystemUIInputModule.
///
/// Com o projeto em "active input handling = Input System package", o módulo
/// antigo lança InvalidOperationException a cada frame ao tentar ler
/// UnityEngine.Input, e a UI para de responder ao toque. Este é o conserto.
/// </summary>
static class EventSystemUpgrade
{
    const string ScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("Tools/Corrida no Espaço/Migrar EventSystem para o Input System", false, 120)]
    static void Upgrade()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        var legacyModules = FindLegacyModules(scene);
        if (legacyModules.Count == 0)
        {
            Debug.Log($"[EventSystem] Nenhum StandaloneInputModule em {ScenePath}. Nada a fazer.");
            return;
        }

        foreach (var legacy in legacyModules)
        {
            var host = legacy.gameObject;

            // Remover antes de adicionar: os dois módulos no mesmo objeto
            // brigariam pelo EventSystem, e o primeiro ativo é que vale.
            Undo.DestroyObjectImmediate(legacy);

            if (host.GetComponent<InputSystemUIInputModule>() == null)
                Undo.AddComponent<InputSystemUIInputModule>(host);

            Debug.Log($"[EventSystem] '{host.name}' migrado para InputSystemUIInputModule.", host);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "[EventSystem] Pronto. O módulo novo recebe as ações padrão sozinho ao rodar — " +
            "o campo Actions Asset ficar vazio no Inspector é o esperado.");
    }

    static Scene OpenMenuScene()
    {
        var current = SceneManager.GetActiveScene();
        if (current.path == ScenePath)
            return current;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static List<StandaloneInputModule> FindLegacyModules(Scene scene)
    {
        var found = new List<StandaloneInputModule>();

        foreach (var root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<StandaloneInputModule>(true));

        return found;
    }
}
