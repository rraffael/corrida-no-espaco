using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta as conquistas: o catálogo em Resources e o painel "Conquistas" na
/// Menu.unity, com o botão dele na coluna do menu.
///
/// **O catálogo se preenche sozinho** com toda ficha de conquista da pasta
/// <c>Assets/Conquistas/</c> que ainda não estiver nele. Conquista nova é criar a
/// ficha na pasta (menu de criação → Corrida no Espaço → Conquista) e rodar o
/// Montar. Para tirar uma, apaga-se a ficha.
///
/// O painel ganha uma linha modelo, desligada, que o <see cref="AchievementsMenu"/>
/// clona lendo o catálogo — a mesma arquitetura das abas de fase e de naves.
/// </summary>
static class AchievementsSetup
{
    public const string Folder = "Assets/Conquistas";
    public const string CatalogPath = LevelSetup.ResourcesFolder + "/" + AchievementCatalog.ResourcePath + ".asset";

    const string PanelName = "PainelConquistas";
    const string MenuButton = "BotaoConquistas";
    const string ToastName = "AvisoConquista";
    const string ButtonsRoot = "Botoes";

    static readonly Vector2 BoxSize = new Vector2(880f, 1320f);
    static readonly Vector2 RowsSize = new Vector2(820f, 780f);
    const float RowHeight = 170f;

    [MenuItem(ProjectTools.AchievementsItem, false, 116)]
    internal static void Setup()
    {
        var catalog = GetOrCreateCatalog();

        var scene = ShipSelectSetup.OpenMenuScene();
        if (!scene.IsValid())
            return;

        var menu = Object.FindAnyObjectByType<Menu>();
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var buttons = canvas != null ? canvas.transform.Find(ButtonsRoot) as RectTransform : null;
        if (menu == null || buttons == null)
        {
            Debug.LogError("[Conquistas] Não achei o Menu ou a coluna de botões. Rode antes o 'Montar menu'.");
            return;
        }

        var panel = BuildPanel(canvas, menu);
        UiBuilder.SetReference(menu, "achievementsPanel", panel);
        panel.SetActive(false);

        BuildButton(buttons, menu);
        BuildToast(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ProjectTools.MarkRun(ProjectTools.AchievementsId);

        Debug.Log($"[Conquistas] Catálogo com {catalog.achievements.Length} conquista(s) e painel montado. " +
                  $"Conquista nova: crie a ficha em {Folder} e rode o Montar.", panel);
    }

    [MenuItem(ProjectTools.AchievementsUndoItem, false, 117)]
    static void Teardown()
    {
        var scene = ShipSelectSetup.OpenMenuScene();
        if (!scene.IsValid())
            return;

        foreach (var name in new[] { PanelName, MenuButton, ToastName })
        {
            var target = ShipSelectSetup.FindAnywhere(name);
            if (target != null)
                Undo.DestroyObjectImmediate(target);
        }

        MenuSetup.RelayoutButtons();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ProjectTools.Forget(ProjectTools.AchievementsId);
    }

    /// <summary>
    /// O catálogo, com toda ficha da pasta que ainda não estiver nele, no fim —
    /// a ordem que já existe não muda.
    /// </summary>
    static AchievementCatalog GetOrCreateCatalog()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(LevelSetup.ResourcesFolder);
        AssetDatabase.Refresh();

        var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AchievementCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var listed = new List<Achievement>();
        foreach (var achievement in catalog.achievements)
        {
            if (achievement != null)
                listed.Add(achievement);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Achievement", new[] { Folder }))
        {
            var achievement = AssetDatabase.LoadAssetAtPath<Achievement>(AssetDatabase.GUIDToAssetPath(guid));
            if (achievement != null && !listed.Contains(achievement))
                listed.Add(achievement);
        }

        catalog.achievements = listed.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        return catalog;
    }

    /// <summary>O botão "Conquistas", logo depois de "Naves": os dois são sobre o jogador.</summary>
    static void BuildButton(RectTransform buttons, Menu menu)
    {
        var existing = ShipSelectSetup.FindAnywhere(MenuButton);
        if (existing == null)
        {
            var created = UiBuilder.Button(MenuButton, buttons, "Conquistas", Vector2.zero, MenuSetup.ButtonSize,
                                           UiBuilder.NeutralButton);
            Undo.RegisterCreatedObjectUndo(created.gameObject, "Montar conquistas");
            UnityEventTools.AddPersistentListener(created.onClick, menu.OnAchievementsButton);

            var label = created.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = 46f;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
            }

            existing = created.gameObject;
        }

        var ships = ShipSelectSetup.FindAnywhere("BotaoNaves");
        existing.transform.SetSiblingIndex(ships != null ? ships.transform.GetSiblingIndex() + 1 : 2);
        MenuSetup.RelayoutButtons();
    }

    static GameObject BuildPanel(Canvas canvas, Menu menu)
    {
        var existing = ShipSelectSetup.FindAnywhere(PanelName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = UiBuilder.Panel(PanelName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar conquistas");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, BoxSize, Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Conquistas", 60f,
                        new Vector2(0f, 560f), new Vector2(820f, 100f), UiBuilder.LabelColor);

        var career = UiBuilder.Label("Carreira", box.transform, string.Empty, 24f,
                                     new Vector2(0f, 460f), new Vector2(820f, 90f), UiBuilder.DimLabelColor);

        var rowsRoot = ShipSelectSetup.BuildScroll(box.transform, new Vector2(0f, -40f), RowsSize, out var rowsObject);

        var layout = rowsObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var fitter = rowsObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var rowTemplate = BuildRowTemplate(rowsObject.transform);

        // Sem conquista nenhuma o painel diz isso, em vez de abrir vazio.
        var empty = UiBuilder.Label("Vazio", box.transform, "Nenhuma conquista ainda.", 30f,
                                    new Vector2(0f, -40f), new Vector2(820f, 80f), UiBuilder.DimLabelColor);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                    new Vector2(0f, -570f), new Vector2(420f, 110f), UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, menu.OnCloseAchievementsButton);

        var component = panel.AddComponent<AchievementsMenu>();
        UiBuilder.SetReference(component, "rowsRoot", rowsRoot);
        UiBuilder.SetReference(component, "rowTemplate", rowTemplate);
        UiBuilder.SetReference(component, "careerLabel", career);
        UiBuilder.SetReference(component, "emptyLabel", empty);

        return panel;
    }

    /// <summary>
    /// O cartão de "Conquista completa", no alto da tela. **Com Canvas próprio
    /// e ordem de desenho alta:** ele tem de aparecer por cima de qualquer
    /// painel aberto — é na aba de naves que se evolui —, e depender da ordem na
    /// hierarquia quebraria no dia em que outra montagem pusesse um painel por
    /// último.
    /// </summary>
    static void BuildToast(Canvas canvas)
    {
        var existing = ShipSelectSetup.FindAnywhere(ToastName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var toast = UiBuilder.NewUI(ToastName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(toast, "Montar conquistas");
        UiBuilder.Place(toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -60f), new Vector2(820f, 150f));

        var layer = toast.AddComponent<Canvas>();
        layer.overrideSorting = true;
        layer.sortingOrder = 100;

        var background = toast.AddComponent<Image>();
        background.sprite = UiBuilder.BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = new Color(0.14f, 0.32f, 0.22f, 0.97f);
        background.raycastTarget = false;

        var labelObject = UiBuilder.NewUI("Texto", toast.transform);
        UiBuilder.Stretch(labelObject, 16f);
        var label = UiBuilder.Label(labelObject, string.Empty, 32f, UiBuilder.LabelColor);

        toast.AddComponent<CanvasGroup>();
        var component = toast.AddComponent<AchievementToast>();
        UiBuilder.SetReference(component, "label", label);
    }

    static AchievementRow BuildRowTemplate(Transform parent)
    {
        var go = UiBuilder.NewUI("ModeloDeConquista", parent);
        ((RectTransform)go.transform).sizeDelta = new Vector2(0f, RowHeight);

        var background = go.AddComponent<Image>();
        background.sprite = UiBuilder.BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = UiBuilder.NeutralButton;

        var nameLabel = ShipSelectSetup.Line(go, "Nome", 34f, UiBuilder.LabelColor, -14f, 48f);
        var detailLabel = ShipSelectSetup.Line(go, "Detalhe", 22f, UiBuilder.DimLabelColor, -64f, 64f);

        var claim = UiBuilder.Button("BotaoResgatar", go.transform, "Resgatar",
                                     Vector2.zero, Vector2.zero, UiBuilder.PrimaryButton, 24f);
        UiBuilder.Place(claim.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-20f, 14f), new Vector2(300f, 58f));

        var row = go.AddComponent<AchievementRow>();
        UiBuilder.SetReference(row, "nameLabel", nameLabel);
        UiBuilder.SetReference(row, "detailLabel", detailLabel);
        UiBuilder.SetReference(row, "claimButton", claim);
        UiBuilder.SetReference(row, "claimLabel", claim.GetComponentInChildren<TMP_Text>());
        UiBuilder.SetReference(row, "background", background);

        go.SetActive(false);
        return row;
    }
}
