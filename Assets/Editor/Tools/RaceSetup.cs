using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a Parte 2 do jogo na Game.unity: o campo de estrelas que rola, o
/// <see cref="RaceSpeed"/> que manda na velocidade e o painel de velocidade no
/// rodapé. A arte é marcador de lugar gerada aqui mesmo — trocar o PNG por arte
/// de verdade depois não encosta em código.
/// </summary>
static class RaceSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string PlaceholderFolder = "Assets/Art/Placeholder";
    const string StarfieldPath = PlaceholderFolder + "/starfield.png";

    const string RaceObject = "Race";
    const string BackgroundObject = "Background";
    const string CanvasObject = "UI";
    const string HudObject = "HudVelocidade";

    /// <summary>Ladrilhos do fundo. Dois bastam: um na tela, um esperando em cima.</summary>
    const int TileCount = 2;

    /// <summary>
    /// O ladrilho é mais largo que o enquadramento de propósito. A câmera vê
    /// `orthographicSize * 2 * aspect` de largura, e o aspect varia de 0.46
    /// (celular alto) a 0.75 (tablet) — 0.8 cobre os dois sem faixa preta.
    /// </summary>
    const float TileWidthFactor = 0.8f;

    /// <summary>Um tico mais alto que a tela, para a emenda entre ladrilhos nunca aparecer.</summary>
    const float TileHeightFactor = 1.05f;

    [MenuItem(ProjectTools.RaceItem, false, 100)]
    internal static void Setup()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Corrida] Nenhuma câmera com a tag MainCamera na cena. " +
                           "Sem ela não dá para dimensionar o fundo.");
            return;
        }

        float viewHeight = cam.orthographicSize * 2f;
        float tileHeight = viewHeight * TileHeightFactor;
        float tileWidth = viewHeight * TileWidthFactor;

        var starfield = LoadOrCreateStarfield(tileWidth, tileHeight);

        var raceObject = GetOrCreate(scene, RaceObject);
        raceObject.transform.position = Vector3.zero;
        GetOrAdd<RaceSpeed>(raceObject);

        BuildBackground(scene, cam, starfield, tileWidth, tileHeight);
        BuildHud(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.RaceId);

        Selection.activeGameObject = raceObject;
        EditorGUIUtility.PingObject(raceObject);

        Debug.Log(
            "[Corrida] Fundo, velocidade e HUD montados em " + ScenePath + ". " +
            "A fase começa com a nave parada e acelerando até a velocidade de cruzeiro " +
            "(8 u/s hoje, no Inspector do objeto 'Race').",
            raceObject);
    }

    [MenuItem(ProjectTools.RaceUndoItem, false, 101)]
    static void Teardown()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        int removed = 0;
        foreach (var name in new[] { BackgroundObject, RaceObject })
        {
            var target = FindInScene(scene, name);
            if (target == null)
                continue;

            Undo.DestroyObjectImmediate(target);
            removed++;
        }

        var canvas = FindInScene(scene, CanvasObject);
        var hud = canvas != null ? canvas.transform.Find(HudObject) : null;
        if (hud != null)
        {
            Undo.DestroyObjectImmediate(hud.gameObject);
            removed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.RaceId);

        Debug.Log($"[Corrida] {removed} objeto(s) removido(s) de {ScenePath}. " +
                  "A arte marcador de lugar continua em " + PlaceholderFolder + ".");
    }

    /// <summary>
    /// Os ladrilhos ficam como filhos de verdade, e não criados em runtime, para
    /// o fundo aparecer na Scene view sem entrar em Play.
    /// </summary>
    static void BuildBackground(Scene scene, Camera cam, Sprite starfield, float tileWidth, float tileHeight)
    {
        var existing = FindInScene(scene, BackgroundObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var background = new GameObject(BackgroundObject);
        Undo.RegisterCreatedObjectUndo(background, "Montar corrida");

        // Ancorado no centro do enquadramento: o embrulho dos ladrilhos é
        // calculado em espaço local, então o fundo acompanha se a câmera mudar.
        var camPosition = cam.transform.position;
        background.transform.position = new Vector3(camPosition.x, camPosition.y, 0f);

        for (int i = 0; i < TileCount; i++)
        {
            var tile = new GameObject($"Ladrilho {i + 1}");
            tile.transform.SetParent(background.transform, false);
            tile.transform.localPosition = new Vector3(0f, i * tileHeight, 0f);

            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = starfield;
            // Atrás de tudo: nave é 10, divisas são 0.
            renderer.sortingOrder = -100;

            // O sprite já nasce com o tamanho certo (ver o PPU em LoadOrCreateStarfield),
            // mas se a arte for trocada por uma de outra proporção, isto reenquadra.
            var size = starfield.bounds.size;
            if (size.x > 0f && size.y > 0f)
                tile.transform.localScale = new Vector3(tileWidth / size.x, tileHeight / size.y, 1f);
        }

        Undo.AddComponent<ScrollingBackground>(background);
    }

    static void BuildHud(Scene scene)
    {
        var canvas = FindInScene(scene, CanvasObject);
        if (canvas == null || canvas.GetComponent<Canvas>() == null)
        {
            Debug.LogWarning(
                "[Corrida] Não achei o Canvas 'UI' na cena, então o painel de velocidade não foi " +
                "montado. O fundo e a velocidade funcionam sem ele.");
            return;
        }

        var existing = canvas.transform.Find(HudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = new GameObject(HudObject, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(hud, "Montar corrida");
        hud.transform.SetParent(canvas.transform, false);

        // Primeiro filho: assim o painel de pausa, que é o último, cobre o HUD
        // quando abre, em vez de o número ficar por cima do menu.
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(600f, 120f);
        rect.anchoredPosition = new Vector2(0f, 48f);

        var label = hud.AddComponent<TextMeshProUGUI>();
        label.text = "0";
        label.fontSize = 72f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.75f, 0.9f, 1f, 0.85f);
        label.raycastTarget = false;

        hud.AddComponent<SpeedHud>();
    }

    /// <summary>
    /// Campo de estrelas marcador de lugar. Semente fixa: rodar de novo dá
    /// exatamente a mesma imagem, e não uma arte diferente a cada montagem.
    /// </summary>
    static Sprite LoadOrCreateStarfield(float tileWidth, float tileHeight)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(StarfieldPath);
        if (existing != null)
            return existing;

        const int width = 320;
        int height = Mathf.RoundToInt(width * (tileHeight / tileWidth));

        var pixels = new Color32[width * height];
        var space = new Color32(10, 10, 24, 255);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = space;

        var random = new System.Random(20260805);

        // Três tamanhos de estrela dão profundidade sem custar nada: as pequenas
        // fazem o volume, as grandes dão pontos de referência para o olho
        // perceber o movimento.
        DrawStars(pixels, width, height, random, count: 260, radius: 0, brightness: 0.55f);
        DrawStars(pixels, width, height, random, count: 70, radius: 1, brightness: 0.8f);
        DrawStars(pixels, width, height, random, count: 18, radius: 2, brightness: 1f);

        return WriteSprite(StarfieldPath, width, height, pixels, pixelsPerUnit: width / tileWidth);
    }

    static void DrawStars(Color32[] pixels, int width, int height, System.Random random,
                          int count, int radius, float brightness)
    {
        for (int i = 0; i < count; i++)
        {
            int cx = random.Next(radius, width - radius);
            int cy = random.Next(radius, height - radius);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    // Cantos do quadrado fora: estrela redonda, não quadradinho.
                    if (x * x + y * y > radius * radius)
                        continue;

                    float falloff = radius == 0 ? 1f : 1f - Mathf.Sqrt(x * x + y * y) / (radius + 1f);
                    byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * brightness * falloff), 0, 255);

                    int index = (cy + y) * width + (cx + x);
                    var current = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Mathf.Max(current.r, value),
                        (byte)Mathf.Max(current.g, value),
                        (byte)Mathf.Max(current.b, (byte)Mathf.Min(255, value + 20)),
                        255);
                }
            }
        }
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
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static GameObject GetOrCreate(Scene scene, string name)
    {
        var existing = FindInScene(scene, name);
        if (existing != null)
            return existing;

        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Montar corrida");
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
