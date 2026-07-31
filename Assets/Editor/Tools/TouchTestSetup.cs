using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta e desmonta o objeto de validação de toque na Game.unity pelo menu,
/// em vez de criar GameObject e arrastar componente na mão.
/// É ferramenta de teste: desmontar antes de gerar build de release.
/// </summary>
static class TouchTestSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string ObjectName = "TouchTest";

    [MenuItem("Tools/Corrida no Espaço/Montar teste de toque", false, 100)]
    static void Setup()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var target = FindInScene(scene, ObjectName);
        if (target == null)
        {
            target = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(target, "Montar teste de toque");
        }

        // TouchTester exige TouchInput ([RequireComponent]). Adicionar os dois na
        // ordem explícita deixa claro o que a cena passa a ter.
        if (target.GetComponent<TouchInput>() == null)
            Undo.AddComponent<TouchInput>(target);

        if (target.GetComponent<TouchTester>() == null)
            Undo.AddComponent<TouchTester>(target);

        SaveAndPing(scene, target);

        if (Camera.main == null)
        {
            Debug.LogWarning(
                "[TouchTest] Nenhuma câmera com a tag MainCamera na cena. " +
                "A leitura de tela e os taps funcionam mesmo assim, mas a posição " +
                "no mundo e o marcador embaixo do dedo não.");
        }

        Debug.Log(
            $"[TouchTest] '{ObjectName}' pronto em {ScenePath}. " +
            "Agora é File > Build And Run e conferir dedos ativos, posição, delta e taps.",
            target);
    }

    [MenuItem("Tools/Corrida no Espaço/Desmontar teste de toque", false, 101)]
    static void Teardown()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var target = FindInScene(scene, ObjectName);
        if (target == null)
        {
            Debug.Log($"[TouchTest] Não havia nenhum '{ObjectName}' em {ScenePath}.");
            return;
        }

        Undo.DestroyObjectImmediate(target);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[TouchTest] '{ObjectName}' removido de {ScenePath}.");
    }

    /// <summary>
    /// Abre a Game.unity, oferecendo salvar antes o que estiver aberto e sujo.
    /// Devolve uma cena inválida quando o usuário cancela.
    /// </summary>
    static Scene OpenGameScene()
    {
        var current = SceneManager.GetActiveScene();
        if (current.path == ScenePath)
            return current;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return default;

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    static void SaveAndPing(Scene scene, GameObject target)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
    }
}
