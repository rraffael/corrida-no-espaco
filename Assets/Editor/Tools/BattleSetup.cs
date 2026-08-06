using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a Parte 3 na Game.unity: a ficha da nave com vida e tiro automático,
/// os obstáculos que descem pelas faixas, o HUD de vida e os painéis de vitória
/// e derrota — com campo de nome e tabela de recordes.
/// </summary>
static class BattleSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";

    const string ShipObject = "Ship";
    const string RaceObject = "Race";
    const string ObstaclesObject = "Obstacles";
    const string CanvasObject = "UI";
    const string PauseButtonObject = "BotaoMenu";
    const string SpeedHudObject = "HudVelocidade";
    const string HealthHudObject = "HudVida";
    const string WarpHudObject = "HudDobra";
    const string VictoryPanelObject = "PainelVitoria";
    const string DefeatPanelObject = "PainelDerrota";

    [MenuItem(ProjectTools.BattleItem, false, 104)]
    static void Setup()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        var ship = FindInScene(scene, ShipObject);
        if (ship == null)
        {
            Debug.LogError("[Combate] Não achei o objeto 'Ship' na cena. Monte a corrida antes.");
            return;
        }

        var race = FindInScene(scene, RaceObject);
        if (race == null)
        {
            Debug.LogError("[Combate] Não achei o objeto 'Race' na cena. " +
                           "Rode antes o 'Montar corrida (fundo + HUD)'.");
            return;
        }

        var pixel = PlaceholderArt.Pixel();

        // ── Nave: vida, ficha e arma ─────────────────────────────────────
        GetOrAdd<Health>(ship);
        GetOrAdd<ShipStats>(ship);
        var weapon = GetOrAdd<ShipWeapon>(ship);
        UiBuilder.SetReference(weapon, "projectileSprite", pixel);

        // ── Obstáculos ───────────────────────────────────────────────────
        var obstacles = GetOrCreate(scene, ObstaclesObject);
        obstacles.transform.position = Vector3.zero;
        var spawner = GetOrAdd<ObstacleSpawner>(obstacles);
        UiBuilder.SetReference(spawner, "sprite", pixel);

        // ── Diretor da corrida ───────────────────────────────────────────
        var director = GetOrAdd<RaceDirector>(race);

        // ── UI ───────────────────────────────────────────────────────────
        var canvas = FindInScene(scene, CanvasObject);
        if (canvas == null)
        {
            Debug.LogError("[Combate] Não achei o Canvas 'UI'. O HUD e os painéis não foram montados.");
            return;
        }

        var healthHud = BuildHealthHud(canvas.transform);
        var warpHud = BuildWarpHud(canvas.transform, director);
        var victory = BuildVictoryPanel(canvas.transform, director);
        var defeat = BuildDefeatPanel(canvas.transform, director);

        var speedHud = canvas.transform.Find(SpeedHudObject);
        var speedHudComponent = speedHud != null ? speedHud.GetComponent<SpeedHud>() : null;
        if (speedHudComponent != null)
            UiBuilder.SetReference(speedHudComponent, "director", director);

        UiBuilder.SetReference(director, "victoryPanel", victory.panel);
        UiBuilder.SetReference(director, "defeatPanel", defeat);
        UiBuilder.SetReference(director, "victoryTimeLabel", victory.time);
        UiBuilder.SetReference(director, "nameField", victory.nameField);
        UiBuilder.SetReference(director, "victoryPlacementLabel", victory.placement);
        UiBuilder.SetReference(director, "victoryBoard", victory.board);

        // O HUD e o botão de pausa somem quando a corrida acaba, para não
        // competirem com o painel de fim.
        var pauseButton = canvas.transform.Find(PauseButtonObject);
        UiBuilder.SetReferenceArray(director, "hideOnEnd",
                                    healthHud,
                                    warpHud,
                                    speedHud != null ? speedHud.gameObject : null,
                                    pauseButton != null ? pauseButton.gameObject : null);

        victory.panel.SetActive(false);
        defeat.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.BattleId);

        Selection.activeGameObject = ship;
        EditorGUIUtility.PingObject(ship);

        Debug.Log(
            "[Combate] Nave com ficha (vida 100, dano 25, 3 tiros/s), obstáculos, HUD de vida e " +
            "de dobra, e painéis de vitória e derrota montados em " + ScenePath + ". " +
            "Vitória é segurar 15 u/s por 5 segundos; cada obstáculo destruído acelera 1,5.",
            ship);
    }

    [MenuItem(ProjectTools.BattleUndoItem, false, 105)]
    static void Teardown()
    {
        var scene = OpenGameScene();
        if (!scene.IsValid())
            return;

        int removed = 0;

        var obstacles = FindInScene(scene, ObstaclesObject);
        if (obstacles != null)
        {
            Undo.DestroyObjectImmediate(obstacles);
            removed++;
        }

        var canvas = FindInScene(scene, CanvasObject);
        if (canvas != null)
        {
            foreach (var name in new[] { HealthHudObject, WarpHudObject, VictoryPanelObject, DefeatPanelObject })
            {
                var target = canvas.transform.Find(name);
                if (target == null)
                    continue;

                Undo.DestroyObjectImmediate(target.gameObject);
                removed++;
            }
        }

        RemoveComponent<ShipWeapon>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<ShipStats>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<Health>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<RaceDirector>(FindInScene(scene, RaceObject), ref removed);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.BattleId);

        Debug.Log($"[Combate] {removed} item(ns) removido(s) de {ScenePath}.");
    }

    static GameObject BuildHealthHud(Transform canvas)
    {
        var existing = canvas.Find(HealthHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(HealthHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(320f, 110f);
        rect.anchoredPosition = new Vector2(48f, 48f);

        var label = UiBuilder.Label(hud, "100", 64f, new Color(0.7f, 1f, 0.8f, 0.9f));
        label.alignment = TextAlignmentOptions.Left;

        hud.AddComponent<HealthHud>();
        return hud;
    }

    /// <summary>Contagem da dobra, logo acima do velocímetro do rodapé.</summary>
    static GameObject BuildWarpHud(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(WarpHudObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var hud = UiBuilder.NewUI(WarpHudObject, canvas);
        Undo.RegisterCreatedObjectUndo(hud, "Montar combate");
        hud.transform.SetAsFirstSibling();

        var rect = (RectTransform)hud.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(700f, 80f);
        // Acima do HudVelocidade, que ocupa de 48 a 168.
        rect.anchoredPosition = new Vector2(0f, 180f);

        UiBuilder.Label(hud, string.Empty, 44f, new Color(0.6f, 1f, 1f, 1f));

        var component = hud.AddComponent<WarpChargeHud>();
        UiBuilder.SetReference(component, "director", director);

        return hud;
    }

    struct VictoryPanel
    {
        public GameObject panel;
        public TextMeshProUGUI time;
        public TextMeshProUGUI placement;
        public TMP_InputField nameField;
        public RecordsBoard board;
    }

    static VictoryPanel BuildVictoryPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(VictoryPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(VictoryPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(900f, 1250f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Dobra espacial!", 64f,
                        new Vector2(0f, 520f), new Vector2(820f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Rotulo", box.transform, "Seu tempo", 34f,
                        new Vector2(0f, 420f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var time = UiBuilder.Label("Tempo", box.transform, "0:00.00", 96f,
                                   new Vector2(0f, 330f), new Vector2(820f, 130f), UiBuilder.LabelColor);

        var nameField = UiBuilder.InputField("CampoNome", box.transform, "Seu nome",
                                             new Vector2(0f, 190f), new Vector2(640f, 110f));

        var save = UiBuilder.Button("BotaoSalvar", box.transform, "Salvar tempo",
                                    new Vector2(0f, 55f), new Vector2(440f, 110f),
                                    UiBuilder.PrimaryButton);
        UnityEventTools.AddPersistentListener(save.onClick, director.SaveScore);

        var placement = UiBuilder.Label("Colocacao", box.transform, "Salve seu tempo na tabela.", 32f,
                                        new Vector2(0f, -35f), new Vector2(820f, 60f),
                                        UiBuilder.DimLabelColor);

        UiBuilder.Label("TituloTabela", box.transform, "Melhores tempos", 36f,
                        new Vector2(0f, -110f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var listObject = UiBuilder.NewUI("Lista", box.transform);
        UiBuilder.PlaceCentered(listObject, new Vector2(0f, -320f), new Vector2(760f, 340f));
        var list = UiBuilder.Label(listObject, string.Empty, 36f, UiBuilder.LabelColor);
        list.alignment = TextAlignmentOptions.Top;

        var board = listObject.AddComponent<RecordsBoard>();
        UiBuilder.SetReference(board, "target", list);

        var boardSerialized = new SerializedObject(board);
        boardSerialized.FindProperty("maxRows").intValue = 5;
        boardSerialized.ApplyModifiedPropertiesWithoutUndo();

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -540f), new Vector2(440f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return new VictoryPanel
        {
            panel = panel,
            time = time,
            placement = placement,
            nameField = nameField,
            board = board,
        };
    }

    static GameObject BuildDefeatPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(DefeatPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(DefeatPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(860f, 620f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Nave destruída", 64f,
                        new Vector2(0f, 180f), new Vector2(780f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Explicacao", box.transform,
                        "A corrida acabou antes da dobra.\nSó quem entra em dobra marca tempo.", 34f,
                        new Vector2(0f, 50f), new Vector2(780f, 120f), UiBuilder.DimLabelColor);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -160f), new Vector2(440f, 110f),
                                    UiBuilder.DangerButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return panel;
    }

    static void RemoveComponent<T>(GameObject target, ref int removed) where T : Component
    {
        if (target == null)
            return;

        var component = target.GetComponent<T>();
        if (component == null)
            return;

        Undo.DestroyObjectImmediate(component);
        removed++;
    }

    static GameObject GetOrCreate(Scene scene, string name)
    {
        var existing = FindInScene(scene, name);
        if (existing != null)
            return existing;

        var created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Montar combate");
        return created;
    }

    static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
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
