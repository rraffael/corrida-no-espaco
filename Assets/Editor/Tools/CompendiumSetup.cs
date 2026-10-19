using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta o Compêndio *(pedido do Raffael, 18/10/2026)*: o catálogo em Resources e
/// o painel na Menu.unity, com o botão dele na coluna do menu.
///
/// **O catálogo se preenche sozinho** com toda ficha de obstáculo e de
/// modificador das pastas que ainda não estiver nele, no fim — a ordem que já
/// existe não muda. Coisa nova no jogo entra no Compêndio rodando o Montar.
///
/// Também preenche a **descrição** das fichas de obstáculo que estiverem sem
/// uma: o campo nasceu com o Compêndio, e as fichas antigas abriam vazias.
/// Ficha com descrição não é tocada — o texto é do Raffael.
/// </summary>
static class CompendiumSetup
{
    public const string CatalogPath = LevelSetup.ResourcesFolder + "/" + CompendiumCatalog.ResourcePath + ".asset";

    const string ObstacleFolder = "Assets/Levels/Obstaculos";
    const string ModifierFolder = "Assets/Modificadores";

    const string PanelName = "PainelCompendio";
    const string MenuButton = "BotaoCompendio";
    const string ButtonsRoot = "Botoes";

    static readonly Vector2 BoxSize = new Vector2(880f, 1320f);
    static readonly Vector2 CardsSize = new Vector2(820f, 820f);

    /// <summary>As descrições de partida dos obstáculos, pelo nome do arquivo.</summary>
    static readonly Dictionary<string, string> ObstacleDescriptions = new Dictionary<string, string>
    {
        { "Obstaculo1-Basico", "Destroço à deriva, o obstáculo mais comum da pista. Atire ou desvie." },
        { "Obstaculo2-Largo", "Cargueiro largo o bastante para fechar duas faixas. Aguenta muito tiro, e " +
                              "rende mais velocidade quando cai." },
        { "Obstaculo3-Estilhacos", "Casca cheia de fragmentos. Destruí-lo de frente cobra caro: os " +
                                   "estilhaços descem na sua faixa." },
        { "Obstaculo4-Rastro", "Deixa um vácuo por onde passa. Passar colado nele sem destruí-lo puxa a " +
                               "nave para o lado." },
    };

    [MenuItem(ProjectTools.CompendiumItem, false, 118)]
    internal static void Setup()
    {
        var catalog = GetOrCreateCatalog();
        FillMissingDescriptions();

        var scene = ShipSelectSetup.OpenMenuScene();
        if (!scene.IsValid())
            return;

        var menu = Object.FindAnyObjectByType<Menu>();
        var canvas = MenuSetup.MenuCanvas();
        var buttons = canvas != null ? canvas.transform.Find(ButtonsRoot) as RectTransform : null;
        if (menu == null || buttons == null)
        {
            Debug.LogError("[Compêndio] Não achei o Menu ou a coluna de botões. Rode antes o 'Montar menu'.");
            return;
        }

        var panel = BuildPanel(canvas, menu);
        UiBuilder.SetReference(menu, "compendiumPanel", panel);
        panel.SetActive(false);

        BuildButton(buttons, menu);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ProjectTools.MarkRun(ProjectTools.CompendiumId);

        Debug.Log($"[Compêndio] Catálogo com {catalog.obstacles.Length} obstáculo(s) e " +
                  $"{catalog.modifiers.Length} modificador(es), e o painel montado. As naves vêm do " +
                  "catálogo de naves.", panel);
    }

    [MenuItem(ProjectTools.CompendiumUndoItem, false, 119)]
    static void Teardown()
    {
        var scene = ShipSelectSetup.OpenMenuScene();
        if (!scene.IsValid())
            return;

        foreach (var name in new[] { PanelName, MenuButton })
        {
            var target = ShipSelectSetup.FindAnywhere(name);
            if (target != null)
                Undo.DestroyObjectImmediate(target);
        }

        MenuSetup.RelayoutButtons();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ProjectTools.Forget(ProjectTools.CompendiumId);
    }

    static CompendiumCatalog GetOrCreateCatalog()
    {
        System.IO.Directory.CreateDirectory(LevelSetup.ResourcesFolder);
        AssetDatabase.Refresh();

        var catalog = AssetDatabase.LoadAssetAtPath<CompendiumCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CompendiumCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.obstacles = Merge(catalog.obstacles, ObstacleFolder);
        catalog.modifiers = Merge(catalog.modifiers, ModifierFolder);
        catalog.placeholder = PlaceholderArt.Circle();

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    /// <summary>
    /// O que já está listado, na mesma ordem, mais o que a pasta tem e a lista
    /// não — pelo nome do arquivo, que é a ordem em que elas foram criadas.
    /// </summary>
    internal static T[] Merge<T>(T[] current, string folder) where T : ScriptableObject
    {
        var listed = new List<T>();
        foreach (var item in current ?? new T[0])
        {
            if (item != null && !listed.Contains(item))
                listed.Add(item);
        }

        var found = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
        {
            var item = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && !listed.Contains(item) && !found.Contains(item))
                found.Add(item);
        }

        found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        listed.AddRange(found);
        return listed.ToArray();
    }

    static void FillMissingDescriptions()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ObstacleStats", new[] { ObstacleFolder }))
        {
            var stats = AssetDatabase.LoadAssetAtPath<ObstacleStats>(AssetDatabase.GUIDToAssetPath(guid));
            if (stats == null || !string.IsNullOrEmpty(stats.description) ||
                !ObstacleDescriptions.TryGetValue(stats.name, out var text))
                continue;

            stats.description = text;
            EditorUtility.SetDirty(stats);
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>O botão "Compêndio", depois de "Conquistas" e antes de "Sair".</summary>
    static void BuildButton(RectTransform buttons, Menu menu)
    {
        var existing = ShipSelectSetup.FindAnywhere(MenuButton);
        if (existing == null)
        {
            var created = UiBuilder.Button(MenuButton, buttons, "Compêndio", Vector2.zero, MenuSetup.ButtonSize,
                                           UiBuilder.NeutralButton);
            Undo.RegisterCreatedObjectUndo(created.gameObject, "Montar compêndio");
            UnityEventTools.AddPersistentListener(created.onClick, menu.OnCompendiumButton);

            var label = created.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.fontSize = 46f;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
            }

            existing = created.gameObject;
        }

        var achievements = ShipSelectSetup.FindAnywhere("BotaoConquistas");
        int index = achievements != null
            ? achievements.transform.GetSiblingIndex() + 1
            : Mathf.Max(0, buttons.childCount - 2);
        existing.transform.SetSiblingIndex(index);
        MenuSetup.RelayoutButtons();
    }

    static GameObject BuildPanel(Canvas canvas, Menu menu)
    {
        var existing = ShipSelectSetup.FindAnywhere(PanelName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var panel = UiBuilder.Panel(PanelName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Montar compêndio");
        panel.transform.SetAsLastSibling();

        var box = UiBuilder.Box("Caixa", panel.transform, BoxSize, Vector2.zero);

        UiBuilder.Label("Titulo", box.transform, "Compêndio", 60f,
                        new Vector2(0f, 560f), new Vector2(820f, 100f), UiBuilder.LabelColor);

        var component = panel.AddComponent<CompendiumMenu>();

        // As abas, na ordem de Compendium.Section.
        string[] tabNames = { "Naves", "Modificadores\nde Fase", "Obstáculos\ne Inimigos" };
        UnityEngine.Events.UnityAction[] tabActions =
        {
            component.OnShipsTab, component.OnModifiersTab, component.OnObstaclesTab,
        };
        var tabs = new Object[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            var tab = UiBuilder.Button("Aba" + i, box.transform, tabNames[i],
                                       new Vector2((i - 1) * 276f, 455f), new Vector2(262f, 100f),
                                       UiBuilder.NeutralButton, 28f);
            var label = tab.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = 28f;
                label.fontSizeMin = 18f;
            }

            UnityEventTools.AddPersistentListener(tab.onClick, tabActions[i]);
            tabs[i] = tab;
        }

        var cardsRoot = ShipSelectSetup.BuildScroll(box.transform, new Vector2(0f, -45f), CardsSize, out var cardsObject);
        var scroll = cardsRoot.GetComponentInParent<ScrollRect>();

        var layout = cardsObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        // A altura é do cartão, que a tira dos textos: cada coisa tem o tamanho
        // do que há para dizer sobre ela.
        layout.childControlHeight = true;

        var fitter = cardsObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var cardTemplate = BuildCardTemplate(cardsObject.transform);

        var empty = UiBuilder.Label("Vazio", box.transform, "Nada aqui ainda.", 30f,
                                    new Vector2(0f, -45f), new Vector2(820f, 80f), UiBuilder.DimLabelColor);

        var back = UiBuilder.Button("BotaoVoltar", box.transform, "Voltar",
                                    new Vector2(0f, -570f), new Vector2(420f, 110f), UiBuilder.NeutralButton);
        UnityEventTools.AddPersistentListener(back.onClick, menu.OnCloseCompendiumButton);

        UiBuilder.SetReference(component, "scroll", scroll);
        UiBuilder.SetReference(component, "cardsRoot", cardsRoot);
        UiBuilder.SetReference(component, "cardTemplate", cardTemplate);
        UiBuilder.SetReference(component, "emptyLabel", empty);
        UiBuilder.SetReferenceArray(component, "tabs", tabs);

        return panel;
    }

    /// <summary>
    /// O cartão que vira todos os outros: cabeçalho com imagem, nome e o que a
    /// coisa é; embaixo as quatro seções de texto. Fica desligado na cena.
    /// </summary>
    static CompendiumCard BuildCardTemplate(Transform parent)
    {
        var go = UiBuilder.NewUI("ModeloDeCartao", parent);

        var background = go.AddComponent<Image>();
        background.sprite = UiBuilder.BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = UiBuilder.NeutralButton;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 24);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var header = UiBuilder.NewUI("Cabecalho", go.transform);
        var headerSize = header.AddComponent<LayoutElement>();
        headerSize.minHeight = 120f;
        headerSize.preferredHeight = 120f;

        var imageObject = UiBuilder.NewUI("Imagem", header.transform);
        UiBuilder.Place(imageObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        Vector2.zero, new Vector2(110f, 110f));
        var image = imageObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        var title = HeaderLine(header, "Nome", 40f, UiBuilder.LabelColor, 0.45f, 1f);
        var subtitle = HeaderLine(header, "Tipo", 24f, UiBuilder.DimLabelColor, 0f, 0.45f);

        var difficulty = DifficultyRow(go, out var stars);
        var description = Section(go, "Descricao", 26f, UiBuilder.LabelColor);
        var attributes = Section(go, "Atributos", 23f, UiBuilder.DimLabelColor);
        var power = Section(go, "Poder", 23f, UiBuilder.DimLabelColor);
        var extra = Section(go, "Extra", 23f, UiBuilder.DimLabelColor);

        var card = go.AddComponent<CompendiumCard>();
        UiBuilder.SetReference(card, "image", image);
        UiBuilder.SetReference(card, "titleLabel", title);
        UiBuilder.SetReference(card, "subtitleLabel", subtitle);
        UiBuilder.SetReference(card, "descriptionLabel", description);
        UiBuilder.SetReference(card, "attributesLabel", attributes);
        UiBuilder.SetReference(card, "powerLabel", power);
        UiBuilder.SetReference(card, "extraLabel", extra);
        UiBuilder.SetReference(card, "difficultyRow", difficulty);
        UiBuilder.SetReferenceArray(card, "stars", stars);

        go.SetActive(false);
        return card;
    }

    /// <summary>
    /// "Dificuldade" e três estrelas, logo abaixo do cabeçalho *(pedido do
    /// Raffael, 19/10/2026)*. Só obstáculo acende a linha; o cartão a esconde
    /// nas outras abas.
    /// </summary>
    static GameObject DifficultyRow(GameObject card, out Object[] stars)
    {
        var row = UiBuilder.NewUI("Dificuldade", card.transform);
        var rowSize = row.AddComponent<LayoutElement>();
        rowSize.minHeight = 44f;
        rowSize.preferredHeight = 44f;

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var labelObject = UiBuilder.NewUI("Rotulo", row.transform);
        var labelSize = labelObject.AddComponent<LayoutElement>();
        labelSize.preferredWidth = 170f;
        labelSize.preferredHeight = 44f;
        var label = UiBuilder.Label(labelObject, "<b><color=#FFD65A>Dificuldade</color></b>", 23f,
                                    UiBuilder.DimLabelColor);
        label.alignment = TextAlignmentOptions.MidlineLeft;

        var sprite = PlaceholderArt.Star();
        stars = new Object[3];
        for (int i = 0; i < stars.Length; i++)
        {
            var starObject = UiBuilder.NewUI("Estrela" + (i + 1), row.transform);
            var starSize = starObject.AddComponent<LayoutElement>();
            starSize.preferredWidth = 40f;
            starSize.preferredHeight = 40f;

            var star = starObject.AddComponent<Image>();
            star.sprite = sprite;
            star.preserveAspect = true;
            star.raycastTarget = false;
            stars[i] = star;
        }

        return row;
    }

    /// <summary>Uma linha do cabeçalho, à direita da imagem, numa faixa da altura dele.</summary>
    static TMP_Text HeaderLine(GameObject header, string name, float size, Color color, float bottom, float top)
    {
        var go = UiBuilder.NewUI(name, header.transform);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = new Vector2(130f, 0f);
        rect.offsetMax = Vector2.zero;

        var label = UiBuilder.Label(go, string.Empty, size, color);
        label.alignment = bottom > 0f ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    /// <summary>Um bloco de texto do cartão. A altura dele é a do texto — quem mede é o grupo de layout.</summary>
    static TMP_Text Section(GameObject card, string name, float size, Color color)
    {
        var go = UiBuilder.NewUI(name, card.transform);
        var label = UiBuilder.Label(go, string.Empty, size, color);
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
}
