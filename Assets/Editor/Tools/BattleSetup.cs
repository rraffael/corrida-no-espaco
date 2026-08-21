using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o combate na Game.unity: a ficha da nave com vida e tiro automático, os
/// obstáculos que descem pelas faixas, o HUD de vida e os **três** painéis de fim
/// de corrida.
///
/// São três porque os desfechos são diferentes: fase de progressão vencida (sem
/// placar, só o que destravou), nave destruída numa fase de progressão, e o fim
/// da fase sem fim — o único com campo de nome e tabela, porque é a única fase
/// que pontua.
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
    const string EndlessPanelObject = "PainelSemFim";

    [MenuItem(ProjectTools.BattleItem, false, 104)]
    internal static void Setup()
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
        var stats = GetOrAdd<ShipStats>(ship);

        // A ficha é um asset, e sem ela a nave não tem número nenhum. Criar aqui
        // se faltar, em vez de só reclamar: quem roda o Montar não deveria ter de
        // saber que existe uma ordem entre os passos.
        var definition = ShipSetup.GetOrCreateStarter();
        UiBuilder.SetReference(stats, "definition", definition);

        // Onde os poderes da corrida ficam pendurados. Entra vazio: quem dá poder
        // à nave é o item da pista, que ainda não existe.
        GetOrAdd<ShipPowerUps>(ship);

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
        var endless = BuildEndlessPanel(canvas.transform, director);

        var speedHud = canvas.transform.Find(SpeedHudObject);
        var speedHudComponent = speedHud != null ? speedHud.GetComponent<SpeedHud>() : null;
        if (speedHudComponent != null)
            UiBuilder.SetReference(speedHudComponent, "director", director);

        UiBuilder.SetReference(director, "victoryPanel", victory.panel);
        UiBuilder.SetReference(director, "victoryTimeLabel", victory.time);
        UiBuilder.SetReference(director, "victoryUnlockLabel", victory.unlock);
        UiBuilder.SetReference(director, "defeatPanel", defeat);

        UiBuilder.SetReference(director, "endlessPanel", endless.panel);
        UiBuilder.SetReference(director, "endlessDistanceLabel", endless.distance);
        UiBuilder.SetReference(director, "nameField", endless.nameField);
        UiBuilder.SetReference(director, "placementLabel", endless.placement);
        UiBuilder.SetReference(director, "recordsBoard", endless.board);

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
        endless.panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.BattleId);

        Selection.activeGameObject = ship;
        EditorGUIUtility.PingObject(ship);

        Debug.Log(
            "[Combate] Nave com ficha (vida 100, dano 25, 3 tiros/s), obstáculos, HUD de vida e " +
            "de dobra, e os três painéis de fim montados em " + ScenePath + ". " +
            "Fase de progressão: vencer é segurar a dobra, e não marca placar. " +
            "Fase sem fim: a corrida acaba quando a nave cai, e vale a distância percorrida.",
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
            foreach (var name in new[] { HealthHudObject, WarpHudObject, VictoryPanelObject,
                                         DefeatPanelObject, EndlessPanelObject })
            {
                var target = canvas.transform.Find(name);
                if (target == null)
                    continue;

                Undo.DestroyObjectImmediate(target.gameObject);
                removed++;
            }
        }

        RemoveComponent<ShipWeapon>(FindInScene(scene, ShipObject), ref removed);
        RemoveComponent<ShipPowerUps>(FindInScene(scene, ShipObject), ref removed);
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
        public TextMeshProUGUI unlock;
    }

    /// <summary>
    /// Fase de progressão vencida. **Sem campo de nome e sem tabela**: estas
    /// fases não pontuam, elas ensinam o jogo e apresentam obstáculo novo. O que
    /// o jogador leva daqui é a fase seguinte, e é isso que o painel anuncia.
    /// </summary>
    static VictoryPanel BuildVictoryPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(VictoryPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(VictoryPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(900f, 780f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Fase concluída!", 64f,
                        new Vector2(0f, 280f), new Vector2(820f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Rotulo", box.transform, "Seu tempo", 34f,
                        new Vector2(0f, 190f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var time = UiBuilder.Label("Tempo", box.transform, "0:00.00", 96f,
                                   new Vector2(0f, 100f), new Vector2(820f, 130f), UiBuilder.LabelColor);

        var unlock = UiBuilder.Label("Destravou", box.transform, string.Empty, 36f,
                                     new Vector2(0f, -30f), new Vector2(820f, 110f),
                                     new Color(0.7f, 1f, 0.8f, 1f));

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar ao menu",
                                    new Vector2(0f, -250f), new Vector2(440f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, director.BackToMenu);

        return new VictoryPanel
        {
            panel = panel,
            time = time,
            unlock = unlock,
        };
    }

    struct EndlessPanel
    {
        public GameObject panel;
        public TextMeshProUGUI distance;
        public TextMeshProUGUI placement;
        public TMP_InputField nameField;
        public RecordsBoard board;
    }

    /// <summary>
    /// Fim da fase sem fim. Não é derrota: a nave cair é o fim previsto de lá, e
    /// o que interessa é até onde ela chegou. É o **único** painel com campo de
    /// nome e tabela, porque é a única fase que pontua.
    /// </summary>
    static EndlessPanel BuildEndlessPanel(Transform canvas, RaceDirector director)
    {
        var existing = canvas.Find(EndlessPanelObject);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(EndlessPanelObject, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Montar combate");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(900f, 1250f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Corrida encerrada", 60f,
                        new Vector2(0f, 520f), new Vector2(820f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Rotulo", box.transform, "Distância percorrida", 34f,
                        new Vector2(0f, 420f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var distance = UiBuilder.Label("Distancia", box.transform, "0 km", 88f,
                                       new Vector2(0f, 330f), new Vector2(820f, 130f),
                                       UiBuilder.LabelColor);

        var nameField = UiBuilder.InputField("CampoNome", box.transform, "Seu nome",
                                             new Vector2(0f, 190f), new Vector2(640f, 110f));

        var save = UiBuilder.Button("BotaoSalvar", box.transform, "Salvar distância",
                                    new Vector2(0f, 55f), new Vector2(440f, 110f),
                                    UiBuilder.PrimaryButton);
        UnityEventTools.AddPersistentListener(save.onClick, director.SaveScore);

        var placement = UiBuilder.Label("Colocacao", box.transform, "Salve sua distância na tabela.", 32f,
                                        new Vector2(0f, -35f), new Vector2(820f, 60f),
                                        UiBuilder.DimLabelColor);

        UiBuilder.Label("TituloTabela", box.transform, "Maiores distâncias", 36f,
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

        return new EndlessPanel
        {
            panel = panel,
            distance = distance,
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
                        "A corrida acabou antes da dobra.\nA fase só é concluída entrando em dobra.", 34f,
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
