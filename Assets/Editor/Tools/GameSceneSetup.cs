using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a Game.unity da primeira etapa do jogo: corredor de 3 faixas, nave que
/// troca de faixa no arraste e a camada de toque. A arte é marcador de lugar
/// gerada aqui mesmo — trocar por arte de verdade depois, sem mexer no código.
/// </summary>
static class GameSceneSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string PlaceholderFolder = "Assets/Art/Placeholder";

    const string InputObject = "Input";
    const string TrackObject = "Track";
    const string ShipObject = "Ship";
    const string DividersObject = "LaneDividers";

    [MenuItem("Tools/Corrida no Espaço/Montar cena do jogo (3 faixas)", false, 20)]
    static void Setup()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        WarnIfTouchTestPresent(scene);

        var shipSprite = LoadOrCreateShipSprite();
        var pixelSprite = LoadOrCreatePixelSprite();

        ConfigureCamera(scene);

        var trackObject = GetOrCreate(scene, TrackObject);
        trackObject.transform.position = Vector3.zero;
        var track = GetOrAdd<LaneTrack>(trackObject);

        var inputObject = GetOrCreate(scene, InputObject);
        GetOrAdd<TouchInput>(inputObject);

        BuildDividers(trackObject, track, pixelSprite);

        var shipObject = GetOrCreate(scene, ShipObject);
        var renderer = GetOrAdd<SpriteRenderer>(shipObject);
        renderer.sprite = shipSprite;
        renderer.sortingOrder = 10;

        // A nave fica na parte de baixo da tela: é de lá que o jogador enxerga o
        // que vem vindo. Com a câmera ortográfica em size 5, y = -3 sobra espaço.
        shipObject.transform.position = new Vector3(track.LaneCenterX(track.CenterLane), -3f, 0f);
        GetOrAdd<ShipLaneController>(shipObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = shipObject;
        EditorGUIUtility.PingObject(shipObject);

        Debug.Log(
            "[Cena do jogo] Corredor de 3 faixas montado em " + ScenePath + ". " +
            "Dá para testar no Editor arrastando com o mouse (o TouchInput cai para o mouse), " +
            "ou File > Build And Run para conferir no aparelho.",
            shipObject);
    }

    [MenuItem("Tools/Corrida no Espaço/Desmontar cena do jogo", false, 21)]
    static void Teardown()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        int removed = 0;
        foreach (var name in new[] { ShipObject, TrackObject, InputObject })
        {
            var target = FindInScene(scene, name);
            if (target == null)
                continue;

            Undo.DestroyObjectImmediate(target);
            removed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Cena do jogo] {removed} objeto(s) removido(s) de {ScenePath}. " +
                  "A arte marcador de lugar continua em " + PlaceholderFolder + ".");
    }

    static void ConfigureCamera(Scene scene)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Cena do jogo] Nenhuma câmera com a tag MainCamera. " +
                             "O enquadramento não foi ajustado.");
            return;
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.09f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        EditorUtility.SetDirty(cam);
    }

    /// <summary>
    /// Linhas finas separando as faixas, só para dar referência visual enquanto
    /// não há cenário. Recriadas do zero a cada montagem: assim mudar a largura
    /// da faixa no LaneTrack e remontar já reposiciona tudo.
    /// </summary>
    static void BuildDividers(GameObject parent, LaneTrack track, Sprite pixel)
    {
        var existing = parent.transform.Find(DividersObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var holder = new GameObject(DividersObject);
        Undo.RegisterCreatedObjectUndo(holder, "Montar cena do jogo");
        holder.transform.SetParent(parent.transform, false);

        // Uma linha em cada divisa entre faixas, mais as duas bordas externas.
        float half = track.LaneWidth * 0.5f;
        for (int i = 0; i <= track.LaneCount; i++)
        {
            float x = track.LaneCenterX(0) - half + i * track.LaneWidth;
            bool edge = i == 0 || i == track.LaneCount;

            var line = new GameObject(edge ? $"Borda {i}" : $"Divisa {i}");
            line.transform.SetParent(holder.transform, false);
            line.transform.localPosition = new Vector3(x - parent.transform.position.x, 0f, 0f);
            line.transform.localScale = new Vector3(edge ? 0.06f : 0.03f, 24f, 1f);

            var renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = edge
                ? new Color(0.45f, 0.85f, 1f, 0.55f)
                : new Color(0.45f, 0.85f, 1f, 0.22f);
            renderer.sortingOrder = 0;
        }
    }

    static Sprite LoadOrCreateShipSprite()
    {
        const string path = PlaceholderFolder + "/ship.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        const int width = 64;
        const int height = 96;
        var pixels = new Color32[width * height];
        var body = new Color32(220, 230, 255, 255);
        var trim = new Color32(90, 170, 255, 255);

        for (int y = 0; y < height; y++)
        {
            // Afina de baixo para cima: silhueta de foguete sem precisar de arte.
            float t = y / (float)(height - 1);
            float halfWidth = Mathf.Lerp(26f, 3f, t * t);

            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x - (width - 1) * 0.5f);
                if (dx > halfWidth)
                    continue;

                // Faixa central mais escura só para dar noção de orientação.
                pixels[y * width + x] = dx < halfWidth * 0.35f && t > 0.15f ? trim : body;
            }
        }

        return WriteSprite(path, width, height, pixels, pixelsPerUnit: 80f);
    }

    /// <summary>Sprite branco de 1x1 unidade de mundo. Escalar no transform dá qualquer retângulo.</summary>
    static Sprite LoadOrCreatePixelSprite()
    {
        const string path = PlaceholderFolder + "/pixel.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        var pixels = new Color32[4];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);

        return WriteSprite(path, 2, 2, pixels, pixelsPerUnit: 2f);
    }

    static Sprite WriteSprite(string assetPath, int width, int height, Color32[] pixels, float pixelsPerUnit)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void WarnIfTouchTestPresent(Scene scene)
    {
        if (FindInScene(scene, "TouchTest") == null)
            return;

        Debug.LogWarning(
            "[Cena do jogo] O objeto 'TouchTest' ainda está na cena. Ele também carrega um " +
            "TouchInput, e o segundo se autodestrói por ser singleton. Rode " +
            "Tools > Corrida no Espaço > Desmontar teste de toque para tirar o overlay de debug.");
    }

    static GameObject GetOrCreate(Scene scene, string name)
    {
        var existing = FindInScene(scene, name);
        if (existing != null)
            return existing;

        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Montar cena do jogo");
        return created;
    }

    static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    /// <summary>Abre a Game.unity, oferecendo salvar antes o que estiver aberto e sujo.</summary>
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
}
