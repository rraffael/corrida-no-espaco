using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Um quadradinho da fileira do <see cref="ModifierHud"/>: o anel que esvazia, o
/// fundo por trás dele e o ícone no meio.
///
/// **O anel é um <see cref="Image"/> preenchido em radial**, e não uma barra: o
/// círculo esvaziando é a forma que o olho reconhece como relógio sem precisar
/// de legenda, e ocupa o mesmo espaço em qualquer duração — um poder de 3
/// segundos e um de 30 desenham o mesmo círculo.
///
/// Ele desenha **o que falta**, não o que passou: começa cheio e vai sumindo, que
/// é a leitura de "está acabando".
/// </summary>
public class ModifierHudSlot : MonoBehaviour
{
    [Tooltip("O anel que esvazia. Precisa ser Image do tipo Filled, em Radial 360.")]
    [SerializeField] Image ring;

    [Tooltip("O disco por trás do anel, que dá contraste contra o fundo estrelado.")]
    [SerializeField] Image backdrop;

    [Tooltip("O ícone do modificador, no meio.")]
    [SerializeField] Image icon;

    /// <summary>A ficha desenhada agora. Serve para não repintar tudo a cada quadro.</summary>
    LevelModifier shown;

    /// <summary>Põe um modificador neste quadradinho e liga.</summary>
    public void Show(ActiveLevelModifier active)
    {
        if (active == null || active.Definition == null)
        {
            Hide();
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        var definition = active.Definition;

        // A cor e o ícone só mudam quando a ficha muda. O anel é o que muda todo
        // quadro, e é só ele que se escreve aqui embaixo.
        if (shown != definition)
        {
            shown = definition;
            var color = definition.Color;

            if (ring != null)
                ring.color = color;

            if (backdrop != null)
                backdrop.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.85f);

            if (icon != null)
            {
                icon.sprite = definition.sprite;
                // Modificador sem arte ainda não é erro: a maioria vai passar um
                // tempo sem ícone, e a cor da categoria já diz o que é.
                icon.enabled = definition.sprite != null;
                icon.color = color;
            }
        }

        if (ring != null)
            ring.fillAmount = active.Fraction;
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
            return;

        gameObject.SetActive(false);

        // Esquecer o que estava aqui: o próximo modificador a ocupar este
        // quadradinho pode ser outro, e a cor tem de ser repintada.
        shown = null;
    }
}
