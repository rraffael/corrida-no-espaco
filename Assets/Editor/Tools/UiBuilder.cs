using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Peças de UI para os scripts de montagem. Existe para painel de pausa, de
/// recordes e de fim de corrida saírem com a mesma cara sem ninguém copiar
/// código de um para o outro.
/// </summary>
static class UiBuilder
{
    public static readonly Color Backdrop = new Color(0.02f, 0.02f, 0.06f, 0.82f);
    public static readonly Color BoxColor = new Color(0.09f, 0.11f, 0.20f, 0.96f);
    public static readonly Color PrimaryButton = new Color(0.16f, 0.42f, 0.75f, 0.95f);
    public static readonly Color NeutralButton = new Color(0.20f, 0.24f, 0.36f, 0.95f);
    public static readonly Color DangerButton = new Color(0.35f, 0.16f, 0.20f, 0.95f);
    public static readonly Color LabelColor = new Color(0.92f, 0.96f, 1f, 1f);
    public static readonly Color DimLabelColor = new Color(0.68f, 0.76f, 0.9f, 1f);

    /// <summary>Caixa arredondada que vem com o Unity — evita importar arte só para ter canto redondo.</summary>
    public static Sprite BuiltinSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    public static Sprite BuiltinArrow() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");

    public static Sprite BuiltinCheckmark() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

    public static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    /// <summary>Centraliza no pai, com posição relativa ao centro.</summary>
    public static RectTransform PlaceCentered(GameObject go, Vector2 position, Vector2 size) =>
        Place(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

    public static RectTransform Stretch(GameObject go, float padding = 0f)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        return rect;
    }

    /// <summary>Painel que cobre a tela inteira. O fundo escuro também segura o toque.</summary>
    public static GameObject Panel(string name, Transform parent)
    {
        var panel = NewUI(name, parent);
        Stretch(panel);

        var image = panel.AddComponent<Image>();
        image.color = Backdrop;

        return panel;
    }

    public static GameObject Box(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var box = NewUI(name, parent);
        PlaceCentered(box, position, size);

        var image = box.AddComponent<Image>();
        image.sprite = BuiltinSprite();
        image.type = Image.Type.Sliced;
        image.color = BoxColor;

        return box;
    }

    public static TextMeshProUGUI Label(GameObject go, string text, float fontSize, Color color)
    {
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, string text, float fontSize,
                                        Vector2 position, Vector2 size, Color color)
    {
        var go = NewUI(name, parent);
        PlaceCentered(go, position, size);
        return Label(go, text, fontSize, color);
    }

    public static Button Button(GameObject go, Color background)
    {
        var image = go.AddComponent<Image>();
        image.sprite = BuiltinSprite();
        image.type = Image.Type.Sliced;
        image.color = background;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        // A tinta de estado multiplica a cor da imagem, então o padrão do Unity
        // (escurecer ao apertar) já dá o retorno certo.
        return button;
    }

    public static Button Button(string name, Transform parent, string text, Vector2 position,
                                Vector2 size, Color background, float fontSize = 44f)
    {
        var go = NewUI(name, parent);
        PlaceCentered(go, position, size);

        var button = Button(go, background);

        var labelObject = NewUI("Texto", go.transform);
        Stretch(labelObject);
        Label(labelObject, text, fontSize, LabelColor);

        return button;
    }

    /// <summary>
    /// Campo de texto do TextMesh Pro. Ele exige uma hierarquia certinha —
    /// viewport com máscara, texto e placeholder — e é isso que esta função monta.
    /// </summary>
    public static TMP_InputField InputField(string name, Transform parent, string placeholderText,
                                            Vector2 position, Vector2 size, float fontSize = 40f)
    {
        var go = NewUI(name, parent);
        PlaceCentered(go, position, size);

        var background = go.AddComponent<Image>();
        background.sprite = BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = new Color(0.04f, 0.05f, 0.1f, 0.95f);

        var viewport = NewUI("Text Area", go.transform);
        Stretch(viewport, padding: 14f);
        viewport.AddComponent<RectMask2D>();

        var textObject = NewUI("Text", viewport.transform);
        Stretch(textObject);
        var text = Label(textObject, string.Empty, fontSize, LabelColor);
        text.richText = false;

        var placeholderObject = NewUI("Placeholder", viewport.transform);
        Stretch(placeholderObject);
        var placeholder = Label(placeholderObject, placeholderText, fontSize, DimLabelColor);
        placeholder.fontStyle = FontStyles.Italic;

        var input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.textViewport = (RectTransform)viewport.transform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.onFocusSelectAll = true;
        input.text = string.Empty;

        return input;
    }

    /// <summary>
    /// Seletor suspenso do TextMesh Pro. Ele não funciona sem uma hierarquia
    /// exata — modelo desligado, viewport com máscara, conteúdo e um item que
    /// serve de molde —, e montar isso na mão pelo Inspector é receita de campo
    /// esquecido. O componente é adicionado por último, com os filhos já de pé.
    ///
    /// As opções vêm em runtime de quem usa: aqui só nasce a casca.
    /// </summary>
    public static TMP_Dropdown Dropdown(string name, Transform parent, Vector2 position, Vector2 size,
                                        float fontSize = 38f, float itemHeight = 72f, float listHeight = 224f)
    {
        var go = NewUI(name, parent);
        PlaceCentered(go, position, size);

        var background = go.AddComponent<Image>();
        background.sprite = BuiltinSprite();
        background.type = Image.Type.Sliced;
        background.color = NeutralButton;

        var captionObject = NewUI("Label", go.transform);
        var captionRect = Stretch(captionObject);
        captionRect.offsetMin = new Vector2(26f, 8f);
        captionRect.offsetMax = new Vector2(-74f, -8f);
        var caption = Label(captionObject, string.Empty, fontSize, LabelColor);
        caption.alignment = TextAlignmentOptions.Left;

        var arrowObject = NewUI("Seta", go.transform);
        Place(arrowObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
              new Vector2(-24f, 0f), new Vector2(32f, 32f));
        var arrow = arrowObject.AddComponent<Image>();
        arrow.sprite = BuiltinArrow();
        arrow.color = DimLabelColor;
        arrow.raycastTarget = false;

        var template = NewUI("Template", go.transform);
        var templateRect = (RectTransform)template.transform;
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, listHeight);

        var templateImage = template.AddComponent<Image>();
        templateImage.sprite = BuiltinSprite();
        templateImage.type = Image.Type.Sliced;
        templateImage.color = BoxColor;

        var viewport = NewUI("Viewport", template.transform);
        var viewportRect = Stretch(viewport);
        viewportRect.pivot = new Vector2(0f, 1f);
        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.sprite = BuiltinSprite();
        viewportImage.type = Image.Type.Sliced;
        // A máscara precisa de um Graphic no mesmo objeto, mas ele não pode
        // aparecer: quem desenha o fundo da lista é o Template.
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = NewUI("Content", viewport.transform);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, itemHeight);

        var item = NewUI("Item", content.transform);
        var itemRect = (RectTransform)item.transform;
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0f, itemHeight);

        var itemBackgroundObject = NewUI("Item Background", item.transform);
        Stretch(itemBackgroundObject);
        var itemBackground = itemBackgroundObject.AddComponent<Image>();
        itemBackground.color = PrimaryButton;

        var checkmarkObject = NewUI("Item Checkmark", item.transform);
        Place(checkmarkObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
              new Vector2(30f, 0f), new Vector2(30f, 30f));
        var checkmark = checkmarkObject.AddComponent<Image>();
        checkmark.sprite = BuiltinCheckmark();
        checkmark.color = LabelColor;

        var itemLabelObject = NewUI("Item Label", item.transform);
        var itemLabelRect = Stretch(itemLabelObject);
        itemLabelRect.offsetMin = new Vector2(66f, 4f);
        itemLabelRect.offsetMax = new Vector2(-24f, -4f);
        var itemLabel = Label(itemLabelObject, "Opção", fontSize, LabelColor);
        itemLabel.alignment = TextAlignmentOptions.Left;

        var toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        toggle.graphic = checkmark;
        toggle.isOn = true;

        var scroll = template.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 0f;

        var dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = background;
        dropdown.template = templateRect;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;

        // O modelo só ganha vida quando o jogador abre a lista.
        template.SetActive(false);

        return dropdown;
    }

    /// <summary>Botão sem arte, do tamanho do pai: serve para "toque em qualquer lugar".</summary>
    public static Button InvisibleFullScreenButton(string name, Transform parent)
    {
        var go = NewUI(name, parent);
        Stretch(go);

        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        // Transparente mas ainda clicável: é isto que faz o toque valer na tela toda.
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        return button;
    }

    public static void SetReference(Object owner, string propertyName, Object value)
    {
        var serialized = new SerializedObject(owner);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[Montagem] {owner.GetType().Name} não tem o campo '{propertyName}'.");
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetReferenceArray(Object owner, string propertyName, params Object[] values)
    {
        var serialized = new SerializedObject(owner);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[Montagem] {owner.GetType().Name} não tem o campo '{propertyName}'.");
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
