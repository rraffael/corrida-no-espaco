using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Monta na Menu.unity a aba "Naves": cada nave do catálogo com os atributos, o
/// poder ativo e o passivo, e a escolha de com qual jogar.
///
/// A cena não ganha uma linha por nave — ganha **uma linha modelo, desligada**,
/// que o <see cref="ShipSelectMenu"/> clona em runtime lendo o catálogo. Nave
/// nova no catálogo aparece aqui sozinha, sem ninguém rodar esta ferramenta de
/// novo. É a mesma arquitetura da seleção de fase, e pelo mesmo motivo.
/// </summary>
static class ShipSelectSetup
{
    const string ScenePath = "Assets/Scenes/Menu.unity";
    const string PanelName = "PainelNaves";
    const string ShipsButton = "BotaoNaves";
    const string ButtonsRoot = "Botoes";

    static readonly Vector2 BoxSize = new Vector2(880f, 1320f);
    static readonly Vector2 RowsSize = new Vector2(800f, 900f);
    const float RowHeight = 220f;
    const float RowSpacing = 16f;

    [MenuItem(ProjectTools.ShipSelectItem, false, 114)]
    internal static void Setup()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        var menu = Object.FindAnyObjectByType<Menu>();
        if (menu == null)
        {
            Debug.LogError("[Naves] Nenhum componente Menu na cena. Não dá para ligar o painel.");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Naves] Nenhum Canvas na cena.");
            return;
        }

        var buttons = canvas.transform.Find(ButtonsRoot) as RectTransform;
        if (buttons == null)
        {
            Debug.LogError("[Naves] Não achei o contêiner 'Botoes'. Rode antes o 'Montar menu'.");
            return;
        }

        var panel = BuildPanel(canvas, menu);
        UiBuilder.SetReference(menu, "shipsPanel", panel);
        panel.SetActive(false);

        BuildButton(buttons, menu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.MarkRun(ProjectTools.ShipSelectId);

        Selection.activeGameObject = panel;
        EditorGUIUtility.PingObject(panel);

        Debug.Log("[Naves] Aba montada em " + ScenePath + ". A nave escolhida vale para a próxima " +
                  "corrida e sobrevive a fechar o jogo — ver ShipSelection.", panel);
    }

    [MenuItem(ProjectTools.ShipSelectUndoItem, false, 115)]
    static void Teardown()
    {
        var scene = OpenMenuScene();
        if (!scene.IsValid())
            return;

        int removed = 0;
        foreach (var name in new[] { PanelName, ShipsButton })
        {
            var target = FindAnywhere(name);
            if (target == null)
                continue;

            Undo.DestroyObjectImmediate(target);
            removed++;
        }

        // Os três que restam voltam para as alturas de antes, senão fica um
        // buraco onde estava o de Naves.
        MenuSetup.RelayoutButtons();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ProjectTools.Forget(ProjectTools.ShipSelectId);

        Debug.Log($"[Naves] {removed} objeto(s) removido(s). Sem a aba, o jogo usa a primeira nave " +
                  "do catálogo — ou a que já estiver salva na preferência.");
    }

    /// <summary>
    /// O botão "Naves", entre Jogar e Recordes. Ali e não no fim porque escolher
    /// a nave é parte de preparar a partida, e o que fica ao lado de Jogar é o
    /// que se faz antes de jogar.
    /// </summary>
    static void BuildButton(RectTransform buttons, Menu menu)
    {
        var existing = FindAnywhere(ShipsButton);
        if (existing == null)
        {
            var created = UiBuilder.Button(ShipsButton, buttons, "Naves", Vector2.zero, MenuSetup.ButtonSize,
                                           UiBuilder.NeutralButton);
            Undo.RegisterCreatedObjectUndo(created.gameObject, "Montar aba de naves");
            UnityEventTools.AddPersistentListener(created.onClick, menu.OnShipsButton);

            var label = created.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = 46f;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
            }

            existing = created.gameObject;
        }

        existing.transform.SetSiblingIndex(1);
        MenuSetup.RelayoutButtons();
    }

    static GameObject BuildPanel(Canvas canvas, Menu menu)
    {
        var existing = FindAnywhere(PanelName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = UiBuilder.Panel(PanelName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar aba de naves");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, BoxSize, Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Suas naves", 60f,
                        new Vector2(0f, 560f), new Vector2(820f, 100f), UiBuilder.LabelColor);

        UiBuilder.Label("Ajuda", box.transform, "Toque para escolher com qual jogar", 28f,
                        new Vector2(0f, 490f), new Vector2(820f, 60f), UiBuilder.DimLabelColor);

        var rowsObject = UiBuilder.NewUI("Linhas", box.transform);
        var rowsRoot = UiBuilder.PlaceCentered(rowsObject, new Vector2(0f, -20f), RowsSize);

        var layout = rowsObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        // A altura é da linha, não do layout: ela precisa caber quatro textos, e
        // não deve encolher quando entrar uma quarta nave.
        layout.childControlHeight = false;

        var rowTemplate = BuildRowTemplate(rowsObject.transform);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                    new Vector2(0f, -570f), new Vector2(420f, 110f),
                                    UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, menu.OnCloseShipsButton);

        var select = panel.AddComponent<ShipSelectMenu>();
        UiBuilder.SetReference(select, "rowsRoot", rowsRoot);
        UiBuilder.SetReference(select, "rowTemplate", rowTemplate);

        return panel;
    }

    /// <summary>
    /// A linha que vira todas as outras: nome, atributos, poder ativo e passivo,
    /// e a bolinha da cor da nave à esquerda. Fica desligada na cena.
    /// </summary>
    static ShipSelectRow BuildRowTemplate(Transform parent)
    {
        var go = UiBuilder.NewUI("ModeloDeNave", parent);
        ((RectTransform)go.transform).sizeDelta = new Vector2(0f, RowHeight);

        var background = go.AddComponent<Image>();
        background.sprite = UiBuilder.BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = UiBuilder.PrimaryButton;

        var button = go.AddComponent<Button>();
        button.targetGraphic = background;

        // A bolinha na cor da nave, enquanto não há arte. É o que diferencia uma
        // linha da outra antes de o nome ser lido.
        var markerObject = UiBuilder.NewUI("Cor", go.transform);
        UiBuilder.Place(markerObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(34f, 0f), new Vector2(40f, 40f));
        var marker = markerObject.AddComponent<Image>();
        marker.sprite = PlaceholderArt.Circle();
        marker.raycastTarget = false;
        marker.preserveAspect = true;

        var nameLabel = Line(go, "Nome", 40f, UiBuilder.LabelColor, -16f, 56f);
        var statsLabel = Line(go, "Atributos", 24f, UiBuilder.DimLabelColor, -64f, 40f);
        var activeLabel = Line(go, "Ativo", 24f, UiBuilder.DimLabelColor, -108f, 40f);
        var passiveLabel = Line(go, "Passivo", 24f, UiBuilder.DimLabelColor, -152f, 40f);

        var row = go.AddComponent<ShipSelectRow>();
        UiBuilder.SetReference(row, "button", button);
        UiBuilder.SetReference(row, "background", background);
        UiBuilder.SetReference(row, "marker", marker);
        UiBuilder.SetReference(row, "nameLabel", nameLabel);
        UiBuilder.SetReference(row, "statsLabel", statsLabel);
        UiBuilder.SetReference(row, "activeLabel", activeLabel);
        UiBuilder.SetReference(row, "passiveLabel", passiveLabel);

        go.SetActive(false);

        return row;
    }

    /// <summary>
    /// Uma linha de texto alinhada à esquerda, ancorada no topo da linha. Em
    /// posição a partir do topo e não centrada: as quatro linhas precisam empilhar
    /// na ordem, e o topo é o único ponto que não se mexe quando a linha muda de
    /// altura.
    /// </summary>
    static TMP_Text Line(GameObject row, string name, float size, Color color, float y, float height)
    {
        var go = UiBuilder.NewUI(name, row.transform);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(88f, 0f);
        rect.offsetMax = new Vector2(-24f, 0f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        var label = UiBuilder.Label(go, string.Empty, size, color);
        label.alignment = TextAlignmentOptions.Left;

        return label;
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
