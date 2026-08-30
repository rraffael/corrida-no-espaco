using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ajusta a Menu.unity: botões maiores, o terceiro botão (Recordes) entre
/// Jogar e Sair, o painel da tabela e a transição que mostra o pódio antes da
/// partida começar.
///
/// Os botões que já existiam são reaproveitados — a fiação deles com
/// <c>Menu.OnPlayButton</c> e <c>Menu.OnQuitButton</c> continua de pé.
/// </summary>
static class MenuSetup
{
    const string ScenePath = "Assets/Scenes/Menu.unity";

    const string ButtonsRoot = "Botoes";
    const string PlayButton = "BotaoJogar";
    const string RecordsButton = "BotaoRecordes";
    const string QuitButton = "BotaoSair";
    const string RecordsPanel = "PainelRecordes";
    const string TransitionPanel = "PainelTransicao";

    // Os botões eram 300x65. "Um pouco maiores" com folga para o dedo, e com
    // espaçamento que cabe a coluna inteira sem encostar no título.
    internal static readonly Vector2 ButtonSize = new Vector2(440f, 110f);
    const float ButtonSpacing = 140f;

    [MenuItem(ProjectTools.MenuSceneItem, false, 110)]
    internal static void Setup()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        var menu = Object.FindAnyObjectByType<Menu>();
        if (menu == null)
        {
            Debug.LogError("[Menu] Nenhum componente Menu na cena. Não dá para ligar os botões.");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Menu] Nenhum Canvas na cena.");
            return;
        }

        var buttons = GetOrCreateButtonsRoot(canvas);
        var play = AdoptButton(PlayButton, buttons);
        var quit = AdoptButton(QuitButton, buttons);
        var records = GetOrCreateRecordsButton(buttons, menu);

        if (play == null || quit == null)
        {
            Debug.LogError("[Menu] Não achei o BotaoJogar ou o BotaoSair na cena.");
            return;
        }

        // Ordem visual: Jogar em cima, Sair embaixo, Recordes logo acima do Sair.
        // O que estiver no meio — hoje a aba de Naves — fica onde a montagem dele
        // o pôs, e é por isso que aqui não se numera ninguém no meio.
        play.transform.SetAsFirstSibling();
        quit.transform.SetAsLastSibling();
        records.transform.SetSiblingIndex(buttons.childCount - 2);

        RelayoutButtons();

        var recordsPanel = BuildRecordsPanel(canvas, menu);
        var transitionPanel = BuildTransitionPanel(canvas, menu);

        UiBuilder.SetReference(menu, "recordsPanel", recordsPanel);
        UiBuilder.SetReference(menu, "transitionPanel", transitionPanel);
        UiBuilder.SetReference(menu, "buttonsRoot", buttons.gameObject);

        recordsPanel.SetActive(false);
        transitionPanel.SetActive(false);
        buttons.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.MenuSceneId);

        Selection.activeGameObject = buttons.gameObject;
        EditorGUIUtility.PingObject(buttons.gameObject);

        Debug.Log("[Menu] Botões maiores (440x110), botão Recordes no meio, painel da tabela e " +
                  "transição com o pódio montados em " + ScenePath + ".", buttons);
    }

    [MenuItem(ProjectTools.MenuSceneUndoItem, false, 111)]
    static void Teardown()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        int removed = 0;
        foreach (var name in new[] { RecordsPanel, TransitionPanel })
        {
            var target = FindAnywhere(name);
            if (target == null)
                continue;

            Undo.DestroyObjectImmediate(target);
            removed++;
        }

        var records = FindAnywhere(RecordsButton);
        if (records != null)
        {
            Undo.DestroyObjectImmediate(records);
            removed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.MenuSceneId);

        Debug.Log($"[Menu] {removed} objeto(s) removido(s). Os botões Jogar e Sair continuam, " +
                  "no tamanho novo — desfazer o tamanho é Ctrl+Z ou remontar.");
    }

    /// <summary>
    /// Junta os botões num objeto só, para o menu sumir inteiro quando um painel
    /// abre. O contêiner cobre o Canvas, então as posições dos botões não mudam.
    /// </summary>
    static RectTransform GetOrCreateButtonsRoot(Canvas canvas)
    {
        var existing = canvas.transform.Find(ButtonsRoot) as RectTransform;
        if (existing != null)
            return existing;

        var go = UiBuilder.NewUI(ButtonsRoot, canvas.transform);
        Undo.RegisterCreatedObjectUndo(go, "Montar menu");
        return UiBuilder.Stretch(go);
    }

    /// <summary>Move um botão que já existe para o contêiner, no tamanho e na altura novos.</summary>
    static Button AdoptButton(string name, RectTransform parent)
    {
        var target = FindAnywhere(name);
        if (target == null)
            return null;

        var button = target.GetComponent<Button>();
        if (button == null)
            return null;

        // O contêiner tem o mesmo retângulo do Canvas, então reparentar não move
        // o botão de lugar — só muda quem o liga e desliga.
        Undo.SetTransformParent(target.transform, parent, "Montar menu");
        // A altura fica para o RelayoutButtons, que recentra a coluna inteira no
        // fim: aqui só o tamanho, porque quem sabe quantos botões existem é ele.
        UiBuilder.PlaceCentered(target, Vector2.zero, ButtonSize);

        GrowLabel(target);
        EditorUtility.SetDirty(target);

        return button;
    }

    static Button GetOrCreateRecordsButton(RectTransform parent, Menu menu)
    {
        var existing = FindAnywhere(RecordsButton);
        if (existing != null)
        {
            Undo.SetTransformParent(existing.transform, parent, "Montar menu");
            UiBuilder.PlaceCentered(existing, Vector2.zero, ButtonSize);
            GrowLabel(existing);
            return existing.GetComponent<Button>();
        }

        var button = UiBuilder.Button(RecordsButton, parent, "Recordes",
                                      Vector2.zero, ButtonSize, UiBuilder.NeutralButton);
        Undo.RegisterCreatedObjectUndo(button.gameObject, "Montar menu");
        UnityEventTools.AddPersistentListener(button.onClick, menu.OnRecordsButton);

        return button;
    }

    /// <summary>
    /// Recentra a coluna de botões, dando a cada um a altura que a posição dele
    /// na hierarquia pede.
    ///
    /// **A altura sai da ordem, e não de um número decorado por botão** — foi o
    /// que quebrou quando entrou a aba de Naves em 30/08/2026: cada montagem
    /// sabia a altura dos botões dela e nenhuma sabia quantos botões existiam ao
    /// todo, então o quarto nasceu por cima do terceiro. Agora quem monta um
    /// botão novo só precisa pô-lo na ordem certa e chamar isto.
    ///
    /// É <c>internal</c> porque o <see cref="ShipSelectSetup"/> chama: o dono da
    /// coluna é esta ferramenta, e quem acrescenta um botão pede a ela para
    /// reempilhar.
    /// </summary>
    internal static void RelayoutButtons()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var buttons = canvas != null ? canvas.transform.Find(ButtonsRoot) : null;
        if (buttons == null)
            return;

        // A coluna fica centrada seja qual for o número de botões: com quatro e
        // 140 de passo, o primeiro sobe 210 e o último desce 210.
        float top = (buttons.childCount - 1) * ButtonSpacing * 0.5f;

        for (int i = 0; i < buttons.childCount; i++)
        {
            var child = buttons.GetChild(i).gameObject;
            UiBuilder.PlaceCentered(child, new Vector2(0f, top - i * ButtonSpacing), ButtonSize);
            EditorUtility.SetDirty(child);
        }
    }

    /// <summary>O texto tem de crescer junto com o botão, senão fica perdido no meio.</summary>
    static void GrowLabel(GameObject button)
    {
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;

        Undo.RecordObject(label, "Montar menu");
        label.fontSize = 46f;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(label);
    }

    static GameObject BuildRecordsPanel(Canvas canvas, Menu menu)
    {
        var existing = canvas.transform.Find(RecordsPanel);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(RecordsPanel, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar menu");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, new Vector2(880f, 1100f), Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Recordes", 64f,
                        new Vector2(0f, 460f), new Vector2(800f, 110f), UiBuilder.LabelColor);

        UiBuilder.Label("Ajuda", box.transform, "Distância na fase sem fim — maior é melhor", 30f,
                        new Vector2(0f, 380f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var listObject = UiBuilder.NewUI("Lista", box.transform);
        UiBuilder.PlaceCentered(listObject, new Vector2(0f, -20f), new Vector2(800f, 700f));
        var list = UiBuilder.Label(listObject, string.Empty, 38f, UiBuilder.LabelColor);
        list.alignment = TextAlignmentOptions.Top;

        var board = listObject.AddComponent<RecordsBoard>();
        UiBuilder.SetReference(board, "target", list);

        var boardSerialized = new SerializedObject(board);
        boardSerialized.FindProperty("emptyMessage").stringValue =
            "Ainda sem recordes.\nCorra na fase sem fim para inaugurar a tabela.";
        boardSerialized.ApplyModifiedPropertiesWithoutUndo();

        var close = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                     new Vector2(0f, -450f), new Vector2(420f, 110f),
                                     UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(close.onClick, menu.OnCloseRecordsButton);

        return panel;
    }

    static GameObject BuildTransitionPanel(Canvas canvas, Menu menu)
    {
        var existing = canvas.transform.Find(TransitionPanel);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        var panel = UiBuilder.Panel(TransitionPanel, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar menu");
        panel.transform.SetAsLastSibling();

        // O botão de pular fica atrás do conteúdo, cobrindo a tela: qualquer
        // toque começa a partida sem esperar os segundos.
        var skip = UiBuilder.InvisibleFullScreenButton("TocarParaComecar", panel.transform);
        UnityEventTools.AddPersistentListener(skip.onClick, menu.OnSkipTransitionButton);

        UiBuilder.Label("Titulo", panel.transform, "Maiores distâncias", 58f,
                        new Vector2(0f, 420f), new Vector2(900f, 100f), UiBuilder.LabelColor);

        var podiumObject = UiBuilder.NewUI("Podio", panel.transform);
        UiBuilder.PlaceCentered(podiumObject, new Vector2(0f, 150f), new Vector2(860f, 320f));
        var podium = UiBuilder.Label(podiumObject, string.Empty, 44f, UiBuilder.LabelColor);
        podium.alignment = TextAlignmentOptions.Top;

        var board = podiumObject.AddComponent<RecordsBoard>();
        UiBuilder.SetReference(board, "target", podium);

        var serialized = new SerializedObject(board);
        serialized.FindProperty("maxRows").intValue = 3;
        serialized.FindProperty("emptyMessage").stringValue =
            "Nenhuma distância registrada ainda.\nA primeira pode ser a sua.";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UiBuilder.Label("Aviso", panel.transform, "Toque para começar", 36f,
                        new Vector2(0f, -420f), new Vector2(800f, 80f), UiBuilder.DimLabelColor);

        return panel;
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
