using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta na Menu.unity a tela que o "Jogar" passa a abrir: a lista de fases, o
/// seletor de dificuldade e o cadeado do que ainda não foi destravado.
///
/// A cena não ganha uma linha por fase — ganha **uma linha modelo, desligada**,
/// que o <see cref="LevelSelectMenu"/> clona em runtime lendo o catálogo. Fase
/// nova no catálogo aparece no menu sem ninguém rodar esta ferramenta de novo.
/// </summary>
static class LevelSelectSetup
{
    const string ScenePath = "Assets/Scenes/Menu.unity";
    const string PanelName = "PainelSelecaoFase";
    const string LeaderboardPanelName = "PainelPlacar";

    static readonly Vector2 BoxSize = new Vector2(880f, 1320f);
    static readonly Vector2 RowsSize = new Vector2(800f, 620f);
    const float RowHeight = 120f;
    const float RowSpacing = 16f;

    [MenuItem(ProjectTools.LevelSelectItem, false, 112)]
    internal static void Setup()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        var menu = Object.FindAnyObjectByType<Menu>();
        if (menu == null)
        {
            Debug.LogError("[Seleção] Nenhum componente Menu na cena. Não dá para ligar o painel.");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Seleção] Nenhum Canvas na cena.");
            return;
        }

        var panel = BuildPanel(canvas, menu);
        UiBuilder.SetReference(menu, "levelSelectPanel", panel);
        panel.SetActive(false);

        var leaderboard = BuildLeaderboardPanel(canvas, menu);
        UiBuilder.SetReference(menu, "leaderboardPanel", leaderboard);
        leaderboard.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.LevelSelectId);

        Selection.activeGameObject = panel;
        EditorGUIUtility.PingObject(panel);

        Debug.Log("[Seleção] Tela de fases e painel do placar montados em " + ScenePath + ". O " +
                  "\"Jogar\" abre a lista; o troféu de cada fase abre o placar dela, e só a sem fim " +
                  "passa pela transição do pódio.", panel);
    }

    [MenuItem(ProjectTools.LevelSelectUndoItem, false, 113)]
    static void Teardown()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        foreach (var name in new[] { PanelName, LeaderboardPanelName })
        {
            var target = FindAnywhere(name);
            if (target != null)
                Undo.DestroyObjectImmediate(target);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.LevelSelectId);

        Debug.Log("[Seleção] Painel removido. Sem ele, o \"Jogar\" volta a mandar direto para a " +
                  "transição, jogando a fase que o LevelSelection já tiver escolhido.");
    }

    static GameObject BuildPanel(Canvas canvas, Menu menu)
    {
        var existing = FindAnywhere(PanelName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = UiBuilder.Panel(PanelName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar seleção de fase");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, BoxSize, Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Escolha a fase", 60f,
                        new Vector2(0f, 560f), new Vector2(820f, 100f), UiBuilder.LabelColor);

        var difficultyLabel = UiBuilder.Label("RotuloDificuldade", box.transform, "Dificuldade", 38f,
                                              new Vector2(-230f, 450f), new Vector2(300f, 80f),
                                              UiBuilder.DimLabelColor);
        difficultyLabel.alignment = TextAlignmentOptions.Right;

        var dropdown = UiBuilder.Dropdown("Dificuldade", box.transform,
                                          new Vector2(150f, 450f), new Vector2(420f, 96f));

        var rowsObject = UiBuilder.NewUI("Linhas", box.transform);
        var rowsRoot = UiBuilder.PlaceCentered(rowsObject, new Vector2(0f, -40f), RowsSize);

        var layout = rowsObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        // A altura é da linha, não do layout: 120 é o tamanho de dedo que o resto
        // do menu já usa, e não deve encolher quando entrar uma quarta fase.
        layout.childControlHeight = false;

        var rowTemplate = BuildRowTemplate(rowsObject.transform);

        var progress = UiBuilder.Label("Progresso", box.transform, string.Empty, 30f,
                                       new Vector2(0f, -450f), new Vector2(820f, 60f),
                                       UiBuilder.DimLabelColor);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                    new Vector2(0f, -570f), new Vector2(420f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, menu.OnCloseLevelSelectButton);

        var select = panel.AddComponent<LevelSelectMenu>();
        UiBuilder.SetReference(select, "menu", menu);
        UiBuilder.SetReference(select, "difficultyDropdown", dropdown);
        UiBuilder.SetReference(select, "rowsRoot", rowsRoot);
        UiBuilder.SetReference(select, "rowTemplate", rowTemplate);
        UiBuilder.SetReference(select, "progressLabel", progress);

        return panel;
    }

    /// <summary>
    /// A linha que vira todas as outras. Fica desligada na cena: o
    /// <see cref="LevelSelectMenu"/> clona uma por fase e só então liga.
    /// </summary>
    static LevelSelectRow BuildRowTemplate(Transform parent)
    {
        var go = UiBuilder.NewUI("ModeloDeFase", parent);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(0f, RowHeight);

        var background = go.AddComponent<Image>();
        background.sprite = UiBuilder.BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = UiBuilder.PrimaryButton;

        var button = go.AddComponent<Button>();
        button.targetGraphic = background;

        // A linha trancada já pinta a si mesma de escuro; o cinza que o Unity
        // aplica por cima do desabilitado escureceria duas vezes e viraria borrão.
        var colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;

        var nameObject = UiBuilder.NewUI("Nome", go.transform);
        var nameRect = UiBuilder.Stretch(nameObject);
        nameRect.offsetMin = new Vector2(32f, 50f);
        nameRect.offsetMax = new Vector2(-190f, -12f);
        var nameLabel = UiBuilder.Label(nameObject, "Fase", 42f, UiBuilder.LabelColor);
        nameLabel.alignment = TextAlignmentOptions.Left;

        var detailObject = UiBuilder.NewUI("Detalhe", go.transform);
        var detailRect = UiBuilder.Stretch(detailObject);
        detailRect.offsetMin = new Vector2(32f, 12f);
        detailRect.offsetMax = new Vector2(-190f, -68f);
        var detailLabel = UiBuilder.Label(detailObject, string.Empty, 28f, UiBuilder.DimLabelColor);
        detailLabel.alignment = TextAlignmentOptions.Left;

        var padlockObject = UiBuilder.NewUI("Cadeado", go.transform);
        UiBuilder.Place(padlockObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-120f, 0f), new Vector2(56f, 56f));
        var padlock = padlockObject.AddComponent<Image>();
        padlock.sprite = PlaceholderArt.Padlock();
        padlock.color = UiBuilder.DimLabelColor;
        padlock.raycastTarget = false;
        padlock.preserveAspect = true;

        // O troféu na ponta, onde o polegar alcança. É um botão dentro do botão da
        // linha: o toque vai para o de cima, então tocar nele não começa a fase.
        var trophyObject = UiBuilder.NewUI("Trofeu", go.transform);
        UiBuilder.Place(trophyObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-16f, 0f), new Vector2(96f, 96f));
        var trophyBackground = trophyObject.AddComponent<Image>();
        trophyBackground.sprite = UiBuilder.BuiltinSprite();
        trophyBackground.type = Image.Type.Sliced;
        trophyBackground.color = new Color(0f, 0f, 0f, 0.3f);
        var trophy = trophyObject.AddComponent<Button>();
        trophy.targetGraphic = trophyBackground;

        var trophyIconObject = UiBuilder.NewUI("Icone", trophyObject.transform);
        UiBuilder.Stretch(trophyIconObject, 14f);
        var trophyIcon = trophyIconObject.AddComponent<Image>();
        trophyIcon.sprite = PlaceholderArt.Trophy();
        trophyIcon.color = new Color(1f, 0.84f, 0.3f, 1f);
        trophyIcon.raycastTarget = false;
        trophyIcon.preserveAspect = true;

        var row = go.AddComponent<LevelSelectRow>();
        UiBuilder.SetReference(row, "button", button);
        UiBuilder.SetReference(row, "background", background);
        UiBuilder.SetReference(row, "nameLabel", nameLabel);
        UiBuilder.SetReference(row, "detailLabel", detailLabel);
        UiBuilder.SetReference(row, "padlock", padlockObject);
        UiBuilder.SetReference(row, "trophyButton", trophy);

        go.SetActive(false);

        return row;
    }

    /// <summary>
    /// O placar de uma fase, aberto pelo troféu: o topo, o jogador com os
    /// vizinhos, e dali dá para jogar a fase ou voltar à lista.
    /// </summary>
    static LeaderboardPanel BuildLeaderboardPanel(Canvas canvas, Menu menu)
    {
        var existing = FindAnywhere(LeaderboardPanelName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = UiBuilder.Panel(LeaderboardPanelName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar seleção de fase");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, BoxSize, Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Placar", 60f,
                        new Vector2(0f, 560f), new Vector2(820f, 100f), UiBuilder.LabelColor);

        var level = UiBuilder.Label("Fase", box.transform, string.Empty, 36f,
                                    new Vector2(0f, 480f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var view = UiBuilder.Leaderboard(box.transform, new Vector2(0f, -20f), new Vector2(840f, 860f), 30f,
                                         "Ninguém completou esta fase ainda.\nO primeiro tempo pode ser o seu.");

        var play = UiBuilder.Button("BotaoJogar", box.transform, "Jogar",
                                    new Vector2(-210f, -570f), new Vector2(380f, 110f),
                                    UiBuilder.PrimaryButton);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                    new Vector2(210f, -570f), new Vector2(380f, 110f),
                                    UiBuilder.NeutralButton);

        var leaderboard = panel.AddComponent<LeaderboardPanel>();
        UiBuilder.SetReference(leaderboard, "menu", menu);
        UiBuilder.SetReference(leaderboard, "view", view);
        UiBuilder.SetReference(leaderboard, "titleLabel", level);
        UiBuilder.SetReference(leaderboard, "playButton", play);

        UnityEventTools.AddPersistentListener(play.onClick, leaderboard.OnPlayButton);
        UnityEventTools.AddPersistentListener(back.onClick, leaderboard.OnBackButton);

        return leaderboard;
    }

    static GameObject FindAnywhere(string name)
    {
        foreach (var transform in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                                                                         FindObjectsSortMode.None))
        {
            if (transform.name == name)
                return transform.gameObject;
        }

        return null;
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
}
