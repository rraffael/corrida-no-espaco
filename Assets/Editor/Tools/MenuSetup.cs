using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ajusta a Menu.unity: o título no alto, a coluna de botões que se arruma
/// sozinha (18/10/2026) e a transição que mostra o pódio da sem
/// fim antes de ela começar. O botão e o painel de Recordes saíram em
/// 17/10/2026 — os placares moram na seleção de fase.
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
    const string TitleName = "Titulo";

    // Os botões eram 300x65. "Um pouco maiores" com folga para o dedo.
    internal static readonly Vector2 ButtonSize = new Vector2(440f, 110f);
    const float ButtonMinHeight = 80f;
    const float ButtonGap = 30f;

    // Faixas da tela, em fração da altura (de baixo para cima). Não se cruzam:
    // é isso que garante que o título nunca mais fica atrás de um botão, seja
    // qual for a tela ou o número de botões.
    static readonly Vector2 TitleBand = new Vector2(0.70f, 0.92f);
    static readonly Vector2 ButtonsBand = new Vector2(0.08f, 0.66f);
    const float TitleSideMargin = 0.06f;

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

        if (play == null || quit == null)
        {
            Debug.LogError("[Menu] Não achei o BotaoJogar ou o BotaoSair na cena.");
            return;
        }

        RemoveRecords();

        // Ordem visual: Jogar em cima, Sair embaixo. O que estiver no meio — as
        // abas de Naves e Conquistas — fica onde a montagem delas o pôs, e é por
        // isso que aqui não se numera ninguém no meio.
        play.transform.SetAsFirstSibling();
        quit.transform.SetAsLastSibling();

        RelayoutButtons();

        var transitionPanel = BuildTransitionPanel(canvas, menu);

        UiBuilder.SetReference(menu, "transitionPanel", transitionPanel);
        UiBuilder.SetReference(menu, "buttonsRoot", buttons.gameObject);

        transitionPanel.SetActive(false);
        buttons.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.MenuSceneId);

        Selection.activeGameObject = buttons.gameObject;
        EditorGUIUtility.PingObject(buttons.gameObject);

        Debug.Log("[Menu] Título no alto, coluna de botões que se arruma sozinha e transição com o pódio da sem fim montados em " +
                  ScenePath + ". O botão e o painel de Recordes saíram: os placares moram na " +
                  "seleção de fase.", buttons);
    }

    [MenuItem(ProjectTools.MenuSceneUndoItem, false, 111)]
    static void Teardown()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        int removed = RemoveRecords();

        var transition = FindAnywhere(TransitionPanel);
        if (transition != null)
        {
            Undo.DestroyObjectImmediate(transition);
            removed++;
        }

        RelayoutButtons();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.MenuSceneId);

        Debug.Log($"[Menu] {removed} objeto(s) removido(s). Os botões Jogar e Sair continuam, " +
                  "no tamanho novo — desfazer o tamanho é Ctrl+Z ou remontar.");
    }

    /// <summary>
    /// Junta os botões num objeto só, para o menu sumir inteiro quando um painel
    /// abre. O retângulo dele é a faixa dos botões — ver <see cref="RelayoutButtons"/>.
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

        // A posição fica para o RelayoutButtons, que liga o grupo de layout da
        // coluna: aqui só o tamanho, porque quem sabe quantos botões existem é ele.
        Undo.SetTransformParent(target.transform, parent, "Montar menu");
        UiBuilder.PlaceCentered(target, Vector2.zero, ButtonSize);

        GrowLabel(target);
        EditorUtility.SetDirty(target);

        return button;
    }

    /// <summary>
    /// Tira o botão e o painel de Recordes, que existiram até 17/10/2026. A
    /// tabela da sem fim aparece na transição antes dela, e cada fase tem o
    /// placar dela na seleção — um terceiro lugar para ver recordes só mostrava
    /// a tabela errada no lugar errado.
    /// </summary>
    static int RemoveRecords()
    {
        int removed = 0;
        foreach (var name in new[] { RecordsButton, RecordsPanel })
        {
            var target = FindAnywhere(name);
            if (target == null)
                continue;

            Undo.DestroyObjectImmediate(target);
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// Arruma a tela do menu inteira: o título preso no alto e a coluna de
    /// botões no espaço que sobra embaixo dele.
    ///
    /// **Quem empilha os botões é um <see cref="VerticalLayoutGroup"/>, não esta
    /// conta** *(18/10/2026)*. Até ali a coluna era centrada na tela pelo número
    /// de botões, e o título também morava no centro — com quatro botões, ele
    /// ficava atrás deles. Agora as duas coisas vivem em faixas da tela que não
    /// se cruzam, medidas em proporção, e o grupo de layout recentra a coluna
    /// sozinho a cada botão que entra ou sai — inclusive um botão desligado em
    /// jogo, que ele pula. Se botões demais apertarem a faixa, eles encolhem até
    /// <see cref="ButtonMinHeight"/> antes de transbordar.
    ///
    /// Continua <c>internal</c> e com o mesmo nome porque o
    /// <see cref="ShipSelectSetup"/> e o <see cref="AchievementsSetup"/> chamam:
    /// quem acrescenta um botão só precisa pô-lo na ordem certa e chamar isto.
    /// </summary>
    internal static void RelayoutButtons()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        LayoutTitle(canvas.transform.Find(TitleName));

        var buttons = canvas.transform.Find(ButtonsRoot) as RectTransform;
        if (buttons == null)
            return;

        Undo.RecordObject(buttons, "Montar menu");
        buttons.anchorMin = new Vector2(0f, ButtonsBand.x);
        buttons.anchorMax = new Vector2(1f, ButtonsBand.y);
        buttons.pivot = new Vector2(0.5f, 0.5f);
        buttons.offsetMin = Vector2.zero;
        buttons.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(buttons);

        var column = buttons.GetComponent<VerticalLayoutGroup>();
        if (column == null)
            column = Undo.AddComponent<VerticalLayoutGroup>(buttons.gameObject);

        Undo.RecordObject(column, "Montar menu");
        column.childAlignment = TextAnchor.MiddleCenter;
        column.spacing = ButtonGap;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = false;
        column.childForceExpandHeight = false;
        column.padding = new RectOffset(0, 0, 0, 0);
        EditorUtility.SetDirty(column);

        for (int i = 0; i < buttons.childCount; i++)
        {
            var child = buttons.GetChild(i).gameObject;

            var element = child.GetComponent<LayoutElement>();
            if (element == null)
                element = Undo.AddComponent<LayoutElement>(child);

            // Preferido é o tamanho de sempre; o mínimo é até onde a altura
            // cede quando a faixa aperta. Na largura não cede: 440 cabe em
            // qualquer celular em pé.
            Undo.RecordObject(element, "Montar menu");
            element.preferredWidth = ButtonSize.x;
            element.preferredHeight = ButtonSize.y;
            element.minWidth = ButtonSize.x;
            element.minHeight = ButtonMinHeight;
            EditorUtility.SetDirty(element);
        }
    }

    /// <summary>
    /// O título ocupa a faixa de cima, de borda a borda com margem, e o tamanho
    /// da letra se ajusta à faixa — numa tela estreita ele quebra em duas linhas
    /// em vez de vazar.
    /// </summary>
    static void LayoutTitle(Transform title)
    {
        if (title == null)
            return;

        var rect = (RectTransform)title;
        Undo.RecordObject(rect, "Montar menu");
        rect.anchorMin = new Vector2(TitleSideMargin, TitleBand.x);
        rect.anchorMax = new Vector2(1f - TitleSideMargin, TitleBand.y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rect);

        var label = title.GetComponent<TMP_Text>();
        if (label == null)
            return;

        Undo.RecordObject(label, "Montar menu");
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMin = 56f;
        label.fontSizeMax = 120f;
        EditorUtility.SetDirty(label);
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
